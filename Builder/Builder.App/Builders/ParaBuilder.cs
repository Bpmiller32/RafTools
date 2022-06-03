using System.Diagnostics;
using System.IO.Compression;
using Common.Data;

public class ParaBuilder
{
    public Settings Settings { get; set; } = new Settings { Name = "Parascript" };

    private readonly ILogger<ParaBuilder> logger;
    private readonly IConfiguration config;
    private readonly ComponentTask tasks;
    private readonly SocketConnection connection;
    private readonly DatabaseContext context;

    private string DataMonth;
    private string DataYear;

    public ParaBuilder(ILogger<ParaBuilder> logger, IConfiguration config, ComponentTask tasks, SocketConnection connection, DatabaseContext context)
    {
        this.logger = logger;
        this.config = config;
        this.tasks = tasks;
        this.connection = connection;
        this.context = context;

    }

    public async Task ExecuteAsync(string DataYearMonth, CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation("Starting Builder");
            tasks.Parascript = ComponentStatus.InProgress;
            connection.SendMessage(parascript: true);

            DataYear = DataYearMonth.Substring(0, 4);
            DataMonth = DataYearMonth.Substring(3, 2);
            Settings.Validate(config, DataYearMonth);

            ExtractDownload();
            Cleanup(fullClean: true);
            await Extract();
            await Archive();
            Cleanup(fullClean: false);
            CheckBuildComplete();

            tasks.Parascript = ComponentStatus.Ready;
        }
        catch (TaskCanceledException e)
        {
            tasks.Parascript = ComponentStatus.Ready;
            connection.SendMessage(parascript: true);
            logger.LogDebug(e.Message);
        }
        catch (System.Exception e)
        {
            tasks.Parascript = ComponentStatus.Error;
            connection.SendMessage(parascript: true);
            logger.LogError(e.Message);
        }
    }


    public void ExtractDownload()
    {
        DirectoryInfo ip = new DirectoryInfo(Settings.AddressDataPath);

        foreach (DirectoryInfo dir in ip.GetDirectories())
        {
            dir.Attributes = dir.Attributes & ~FileAttributes.ReadOnly;
            dir.Delete(true);
        }

        ZipFile.ExtractToDirectory(Path.Combine(Settings.AddressDataPath, @"Files.zip"), Settings.AddressDataPath);

        tasks.ChangeProgress(DirectoryType.Parascript, 1);
    }

    public void Cleanup(bool fullClean)
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

            tasks.ChangeProgress(DirectoryType.Parascript, 2);
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

        tasks.ChangeProgress(DirectoryType.Parascript, 1);
    }

    public async Task Extract()
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

        tasks.ChangeProgress(DirectoryType.Parascript, 18);
    }

    public async Task Archive()
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

        tasks.ChangeProgress(DirectoryType.Parascript, 78);
    }

    public void CheckBuildComplete()
    {
        // Will be null if Crawler never made a record for it, watch out if running standalone
        ParaBundle bundle = context.ParaBundles.Where(x => (int.Parse(DataMonth) == x.DataMonth) && (int.Parse(DataYear) == x.DataYear)).FirstOrDefault();
        bundle.IsBuildComplete = true;

        context.SaveChanges();
    }
}
