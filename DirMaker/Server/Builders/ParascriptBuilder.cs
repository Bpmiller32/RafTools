using System.Diagnostics;
using System.IO.Compression;
using Server.Common;

namespace Server.Builders;

public class ParascriptBuilder : BaseModule
{
    private readonly ILogger<ParascriptBuilder> logger;
    private readonly IConfiguration config;
    private readonly DatabaseContext context;

    private string dataYearMonth;
    private string dataSourcePath;
    private string dataOutputPath;

    public ParascriptBuilder(ILogger<ParascriptBuilder> logger, IConfiguration config, DatabaseContext context)
    {
        this.logger = logger;
        this.config = config;
        this.context = context;

        Settings.DirectoryName = "Parascript";
    }

    public async Task AutoStart(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Starting Builder - Auto mode");

                TimeSpan waitTime = ModuleSettings.CalculateWaitTime(logger, Settings);
                Status = ModuleStatus.Standby;
                await Task.Delay(TimeSpan.FromSeconds(waitTime.TotalSeconds), stoppingToken);

                foreach (ParaBundle bundle in context.ParaBundles.Where(x => x.IsReadyForBuild && !x.IsBuildComplete).ToList())
                {
                    await Start(bundle.DataYearMonth, stoppingToken);
                    // Edge case where end of the month, no bundles to process, this loop executes in < 1 second and potentially calulates next waitTime too early
                    await Task.Delay(TimeSpan.FromSeconds(2));
                }
            }
        }
        catch (TaskCanceledException e)
        {
            logger.LogDebug($"{e.Message}");
        }
        catch (Exception e)
        {
            Status = ModuleStatus.Error;
            logger.LogError($"{e.Message}");
        }
    }

    public async Task Start(string dataYearMonth, CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation("Starting Builder");
            Status = ModuleStatus.InProgress;

            Settings.Validate(config);
            this.dataYearMonth = dataYearMonth;
            dataSourcePath = Path.Combine(Settings.AddressDataPath, dataYearMonth);
            dataOutputPath = Path.Combine(Settings.OutputPath, dataYearMonth);

            Message = "Extracting files from download";
            Progress = 0;
            ExtractDownload(stoppingToken);

            Message = "Cleaning up from previous builds";
            Progress = 1;
            Cleanup(fullClean: true, stoppingToken);

            Message = "Compiling database";
            Progress = 3;
            await Extract(stoppingToken);

            Message = "Packaging database";
            Progress = 21;
            await Archive(stoppingToken);

            Message = "Cleaning up post build";
            Progress = 98;
            Cleanup(fullClean: false, stoppingToken);

            Message = "Updating packaged directories";
            Progress = 99;
            CheckBuildComplete(stoppingToken);

            Message = "";
            Progress = 100;
            logger.LogInformation($"Build Complete: {dataYearMonth}");
            Status = ModuleStatus.Ready;
        }
        catch (TaskCanceledException e)
        {
            Status = ModuleStatus.Ready;
            logger.LogDebug($"{e.Message}");
        }
        catch (Exception e)
        {
            Status = ModuleStatus.Error;
            logger.LogError($"{e.Message}");
        }
    }

    private void ExtractDownload(CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        DirectoryInfo inputPath = new(dataSourcePath);
        foreach (DirectoryInfo dir in inputPath.GetDirectories())
        {
            dir.Attributes &= ~FileAttributes.ReadOnly;
            dir.Delete(true);
        }

        ZipFile.ExtractToDirectory(Path.Combine(dataSourcePath, "Files.zip"), dataSourcePath);
    }

    private void Cleanup(bool fullClean, CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        Utils.KillPsProcs();

        Directory.CreateDirectory(Settings.WorkingPath);
        Directory.CreateDirectory(dataOutputPath);

        if (!fullClean)
        {
            Utils.Cleanup(Settings.WorkingPath, stoppingToken);
            return;
        }

        Utils.Cleanup(Settings.WorkingPath, stoppingToken);
        Utils.Cleanup(dataOutputPath, stoppingToken);
    }

    private async Task Extract(CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        List<Task> buildTasks = new()
        {
            // Zip
            Task.Run(() =>
            {
                ZipFile.ExtractToDirectory(Path.Combine(dataSourcePath, "ads6", $"ads_zip_09_{dataYearMonth.Substring(4, 2)}{dataYearMonth.Substring(2, 2)}.exe"), Path.Combine(Settings.WorkingPath, "zip"));
                File.Create(Path.Combine(Settings.WorkingPath, "zip", "live.txt")).Close();
            }, stoppingToken),
            // Lacs,
            Task.Run(() =>
            {
                ZipFile.ExtractToDirectory(Path.Combine(dataSourcePath, "DPVandLACS", "LACSLink", $"ads_lac_09_{dataYearMonth.Substring(4, 2)}{dataYearMonth.Substring(2, 2)}.exe"), Path.Combine(Settings.WorkingPath, "lacs"));
                File.Create(Path.Combine(Settings.WorkingPath, "lacs", "live.txt")).Close();
            }, stoppingToken),
            // Suite
            Task.Run(() =>
            {
                ZipFile.ExtractToDirectory(Path.Combine(dataSourcePath, "DPVandLACS", "SuiteLink", $"ads_slk_09_{dataYearMonth.Substring(4, 2)}{dataYearMonth.Substring(2, 2)}.exe"), Path.Combine(Settings.WorkingPath, "suite"));
                File.Create(Path.Combine(Settings.WorkingPath, "suite", "live.txt")).Close();
            }, stoppingToken),
            // Dpv
            Task.Run(() =>
            {
                ZipFile.ExtractToDirectory(Path.Combine(dataSourcePath, "DPVandLACS", "DPVfull", $"ads_dpv_09_{dataYearMonth.Substring(4, 2)}{dataYearMonth.Substring(2, 2)}.exe"), Path.Combine(Settings.WorkingPath, "dpv"));
                File.Create(Path.Combine(Settings.WorkingPath, "dpv", "live.txt")).Close();

                Process proc = Utils.RunProc(Path.Combine(Directory.GetCurrentDirectory(), "BuildUtils", "PDBIntegrity.exe"), Path.Combine(Settings.WorkingPath, "dpv", "fileinfo_log.txt"));

                using StreamReader sr = proc.StandardOutput;
                string procOutput = sr.ReadToEnd();

                if (!procOutput.Contains("Database files are consistent"))
                {
                    throw new Exception("Database files are NOT consistent");
                }
            }, stoppingToken)
        };

        await Task.WhenAll(buildTasks);
    }

    private async Task Archive(CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        List<Task> buildTasks = new()
        {
            // Zip
            Task.Run(() =>
            {
                Directory.CreateDirectory(Path.Combine(dataOutputPath, "Zip4"));
                ZipFile.CreateFromDirectory(Path.Combine(Settings.WorkingPath, "zip"), Path.Combine(dataOutputPath, "Zip4", "Zip4.zip"));
            }, stoppingToken),
            // Dpv
            Task.Run(() =>
            {
                Directory.CreateDirectory(Path.Combine(dataOutputPath, "DPV"));
                ZipFile.CreateFromDirectory(Path.Combine(Settings.WorkingPath, "dpv"), Path.Combine(dataOutputPath, "DPV", "DPV.zip"));
            }, stoppingToken),
            // Suite
            Task.Run(() =>
            {
                Directory.CreateDirectory(Path.Combine(dataOutputPath, "Suite"));
                ZipFile.CreateFromDirectory(Path.Combine(Settings.WorkingPath, "suite"), Path.Combine(dataOutputPath, "Suite", "SUITE.zip"));
            }, stoppingToken),
            // Lacs
            Task.Run(() =>
            {
                Directory.CreateDirectory(Path.Combine(dataOutputPath, "LACS"));
                foreach (var file in Directory.GetFiles(Path.Combine(Settings.WorkingPath, "lacs")))
                {
                    File.Copy(file, Path.Combine(Path.Combine(dataOutputPath, "LACS"), Path.GetFileName(file)));
                }
            }, stoppingToken),
        };

        await Task.WhenAll(buildTasks);
    }

    private void CheckBuildComplete(CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        // Will be null if Crawler never made a record for it, watch out if running standalone
        ParaBundle bundle = context.ParaBundles.Where(x => dataYearMonth == x.DataYearMonth).FirstOrDefault();
        bundle.IsBuildComplete = true;
        bundle.CompileDate = Utils.CalculateDbDate();
        bundle.CompileTime = Utils.CalculateDbTime();

        context.SaveChanges();
        SendDbUpdate = true;
    }
}
