using System.Diagnostics;
using System.IO.Compression;
using Common.Data;

public class ParaBuilder
{
    public Settings Settings { get; set; } = new Settings { Name = "Parascript" };
    public ComponentStatus Status { get; set; }
    public int Progress { get; set; }

    private readonly ILogger<ParaBuilder> logger;
    private readonly IConfiguration config;
    private readonly SocketConnection connection;
    private readonly DatabaseContext context;

    private string DataMonth;
    private string DataYear;

    public ParaBuilder(ILogger<ParaBuilder> logger, IConfiguration config, SocketConnection connection, DatabaseContext context)
    {
        this.logger = logger;
        this.config = config;
        this.connection = connection;
        this.context = context;
    }

    public async Task ExecuteAsyncAuto(CancellationToken stoppingToken)
    {
        connection.SendMessage(DirectoryType.Parascript);

        if (!Settings.AutoBuildEnabled)
        {
            logger.LogDebug("AutoBuild disabled");
            return;
        }
        if (Status != ComponentStatus.Ready)
        {
            logger.LogDebug("Build already in progress");
            return;
        }

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Starting Builder - Auto mode");
                TimeSpan waitTime = Settings.CalculateWaitTime(logger, Settings);
                await Task.Delay(TimeSpan.FromSeconds(waitTime.TotalSeconds), stoppingToken);

                List<ParaBundle> bundles = context.ParaBundles.Where(x => x.IsReadyForBuild && !x.IsBuildComplete).ToList();
                foreach (ParaBundle bundle in bundles)
                {
                    await this.ExecuteAsync(bundle.DataYearMonth, stoppingToken);
                }
            }
        }
        catch (TaskCanceledException e)
        {
            logger.LogDebug(e.Message);
        }
        catch (System.Exception e)
        {
            logger.LogError(e.Message);
        }
    }

    public async Task ExecuteAsync(string DataYearMonth, CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation("Starting Builder");
            Status = ComponentStatus.InProgress;
            ChangeProgress(0, reset: true);

            DataYear = DataYearMonth.Substring(0, 4);
            DataMonth = DataYearMonth.Substring(4, 2);
            Settings.Validate(config, DataYearMonth);

            ExtractDownload();
            Cleanup(fullClean: true);
            await Extract();
            await Archive();
            Cleanup(fullClean: false);
            CheckBuildComplete();

            logger.LogInformation("Build Complete: {0}", DataYearMonth);
            Status = ComponentStatus.Ready;
            connection.SendMessage(DirectoryType.Parascript);
        }
        catch (TaskCanceledException e)
        {
            Status = ComponentStatus.Ready;
            connection.SendMessage(DirectoryType.Parascript);
            logger.LogDebug(e.Message);
        }
        catch (System.Exception e)
        {
            Status = ComponentStatus.Error;
            connection.SendMessage(DirectoryType.Parascript);
            logger.LogError(e.Message);
        }
    }

    public void ChangeProgress(int changeAmount, bool reset = false)
    {
        if (reset)
        {
            Progress = 0;
        }
        else
        {
            Progress += changeAmount;
        }

        connection.SendMessage(DirectoryType.Parascript);
    }



    private void ExtractDownload()
    {
        DirectoryInfo ip = new DirectoryInfo(Settings.AddressDataPath);

        foreach (DirectoryInfo dir in ip.GetDirectories())
        {
            dir.Attributes = dir.Attributes & ~FileAttributes.ReadOnly;
            dir.Delete(true);
        }

        ZipFile.ExtractToDirectory(Path.Combine(Settings.AddressDataPath, @"Files.zip"), Settings.AddressDataPath);

        ChangeProgress(1);
    }

    private void Cleanup(bool fullClean)
    {
        Utils.KillPsProcs();

        Directory.CreateDirectory(Settings.WorkingPath);
        Directory.CreateDirectory(Settings.OutputPath);

        DirectoryInfo wp = new DirectoryInfo(Settings.WorkingPath);
        DirectoryInfo op = new DirectoryInfo(Settings.OutputPath);

        if (!fullClean)
        {
            foreach (FileInfo file in wp.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in wp.GetDirectories())
            {
                dir.Attributes = dir.Attributes & ~FileAttributes.ReadOnly;
                dir.Delete(true);
            }

            Directory.Delete(Settings.WorkingPath, true);

            ChangeProgress(2);
            return;
        }

        foreach (var file in wp.GetFiles())
        {
            file.Delete();
        }
        foreach (var dir in wp.GetDirectories())
        {
            dir.Attributes = dir.Attributes & ~FileAttributes.ReadOnly;
            dir.Delete(true);
        }
        foreach (var file in op.GetFiles())
        {
            file.Delete();
        }
        foreach (var dir in op.GetDirectories())
        {
            dir.Delete(true);
        }

        ChangeProgress(1);
    }

    private async Task Extract()
    {
        string shortYear = Settings.DataYearMonth.Substring(2, 2);

        Dictionary<string, Task> buildTasks = new Dictionary<string, Task>();

        buildTasks.Add("zip", Task.Run(() =>
        {
            ZipFile.ExtractToDirectory(Path.Combine(Settings.AddressDataPath, @"ads6", @"ads_zip_09_" + DataMonth + shortYear + ".exe"), Path.Combine(Settings.WorkingPath, @"zip"));
            File.Create(Path.Combine(Settings.WorkingPath, @"zip", @"live.txt")).Close();
        }));
        buildTasks.Add("lacs", Task.Run(() =>
        {
            ZipFile.ExtractToDirectory(Path.Combine(Settings.AddressDataPath, @"DPVandLACS", @"LACSLink", @"ads_lac_09_" + DataMonth + shortYear + ".exe"), Path.Combine(Settings.WorkingPath, @"lacs"));
            File.Create(Path.Combine(Settings.WorkingPath, @"lacs", @"live.txt")).Close();
        }));
        buildTasks.Add("suite", Task.Run(() =>
        {
            ZipFile.ExtractToDirectory(Path.Combine(Settings.AddressDataPath, @"DPVandLACS", @"SuiteLink", @"ads_slk_09_" + DataMonth + shortYear + ".exe"), Path.Combine(Settings.WorkingPath, @"suite"));
            File.Create(Path.Combine(Settings.WorkingPath, @"suite", @"live.txt")).Close();
        }));
        buildTasks.Add("dpv", Task.Run(() =>
        {
            ZipFile.ExtractToDirectory(Path.Combine(Settings.AddressDataPath, @"DPVandLACS", @"DPVfull", @"ads_dpv_09_" + DataMonth + shortYear + ".exe"), Path.Combine(Settings.WorkingPath, @"dpv"));
            File.Create(Path.Combine(Settings.WorkingPath, @"dpv", @"live.txt")).Close();

            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                FileName = Directory.GetCurrentDirectory() + @"\BuildUtils\PDBIntegrity.exe",
                Arguments = Path.Combine(Settings.WorkingPath, @"dpv", @"fileinfo_log.txt"),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };
            Process proc = new Process()
            {
                StartInfo = startInfo
            };

            proc.Start();

            using (StreamReader sr = proc.StandardOutput)
            {
                string procOutput = sr.ReadToEnd();
                if (!procOutput.Contains("Database files are consistent"))
                {
                    throw new Exception("Database files are NOT consistent");
                };
            }
        }));

        await Task.WhenAll(buildTasks.Values);

        ChangeProgress(18);
    }

    private async Task Archive()
    {
        Dictionary<string, Task> buildTasks = new Dictionary<string, Task>();

        buildTasks.Add("zip", Task.Run(() =>
        {
            Directory.CreateDirectory(Path.Combine(Settings.OutputPath, @"Zip4"));
            ZipFile.CreateFromDirectory(Path.Combine(Settings.WorkingPath, @"zip"), Path.Combine(Settings.OutputPath, @"Zip4", @"Zip4.zip"));
        }));
        buildTasks.Add("dpv", Task.Run(() =>
        {
            Directory.CreateDirectory(Path.Combine(Settings.OutputPath, @"DPV"));
            ZipFile.CreateFromDirectory(Path.Combine(Settings.WorkingPath, @"dpv"), Path.Combine(Settings.OutputPath, @"DPV", @"DPV.zip"));
        }));
        buildTasks.Add("suite", Task.Run(() =>
        {
            Directory.CreateDirectory(Path.Combine(Settings.OutputPath, @"Suite"));
            ZipFile.CreateFromDirectory(Path.Combine(Settings.WorkingPath, @"suite"), Path.Combine(Settings.OutputPath, @"Suite", @"SUITE.zip"));
        }));
        buildTasks.Add("lacs", Task.Run(() =>
        {
            Directory.CreateDirectory(Path.Combine(Settings.OutputPath, @"LACS"));
            foreach (var file in Directory.GetFiles(Path.Combine(Settings.WorkingPath, @"lacs")))
            {
                File.Copy(file, Path.Combine(Path.Combine(Settings.OutputPath, @"LACS"), Path.GetFileName(file)));
            }
        }));

        await Task.WhenAll(buildTasks.Values);

        ChangeProgress(78);
    }

    private void CheckBuildComplete()
    {
        // Will be null if Crawler never made a record for it, watch out if running standalone
        ParaBundle bundle = context.ParaBundles.Where(x => (int.Parse(DataMonth) == x.DataMonth) && (int.Parse(DataYear) == x.DataYear)).FirstOrDefault();
        bundle.IsBuildComplete = true;

        DateTime timestamp = DateTime.Now;
        string hour;
        string minute;
        string ampm;
        if (timestamp.Minute < 10)
        {
            minute = timestamp.Minute.ToString().PadLeft(2, '0');
        }
        else
        {
            minute = timestamp.Minute.ToString();
        }
        if (timestamp.Hour > 12)
        {
            hour = (timestamp.Hour - 12).ToString();
            ampm = "pm";
        }
        else
        {
            hour = timestamp.Hour.ToString();
            ampm = "am";
        }
        bundle.CompileDate = timestamp.Month.ToString() + "/" + timestamp.Day + "/" + timestamp.Year.ToString();
        bundle.CompileTime = hour + ":" + minute + ampm;

        context.SaveChanges();
    }
}
