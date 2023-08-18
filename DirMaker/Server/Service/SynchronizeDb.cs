using Microsoft.EntityFrameworkCore;
using Server.Common;

namespace Server.Service;

public class SynchronizeDb : BaseModule
{
    private readonly ILogger<SynchronizeDb> logger;
    private readonly IConfiguration config;
    private readonly DatabaseContext context;


    public SynchronizeDb(ILogger<SynchronizeDb> logger, IConfiguration config, DatabaseContext context)
    {
        this.logger = logger;
        this.config = config;
        this.context = context;
    }

    public void ScanDb()
    {
        // SmartMatch
        Settings.DirectoryName = "SmartMatch";
        Settings.Validate(config);

        foreach (UspsFile file in context.UspsFiles.ToList())
        {
            if (!File.Exists(Path.Combine(Settings.AddressDataPath, file.DataYearMonth, file.Cycle, file.FileName)))
            {
                file.OnDisk = false;
            }
        }

        foreach (UspsBundle bundle in context.UspsBundles.Include("BuildFiles").ToList())
        {
            UspsBundle cycleNEquivalent = context.UspsBundles.Where(x => x.DataYearMonth == bundle.DataYearMonth && x.Cycle == "Cycle-N").Include("BuildFiles").FirstOrDefault();

            if (bundle.Cycle == "Cycle-N" && (!bundle.BuildFiles.All(x => x.OnDisk) || bundle.BuildFiles.Count < 6))
            {
                bundle.IsReadyForBuild = false;
            }
            else if (bundle.Cycle == "Cycle-O" && (!bundle.BuildFiles.All(x => x.OnDisk) || bundle.BuildFiles.Count < 4))
            {
                bundle.IsReadyForBuild = false;
            }
            else if (bundle.Cycle == "Cycle-O" && cycleNEquivalent.BuildFiles.Any(x => x.FileName == "zip4natl.tar") && cycleNEquivalent.BuildFiles.Any(x => x.FileName == "zipmovenatl.tar"))
            {
                continue;
            }
            else
            {
                bundle.IsReadyForBuild = true;
                logger.LogInformation($"{Settings.DirectoryName} Bundle ready to build: {bundle.DataMonth}/{bundle.DataYear} {bundle.Cycle}");
            }

            if (Directory.Exists(Path.Combine(Settings.OutputPath, bundle.DataYearMonth, bundle.Cycle)))
            {
                if (Directory.EnumerateFileSystemEntries(Path.Combine(Settings.OutputPath, bundle.DataYearMonth, bundle.Cycle)).Any())
                {
                    bundle.IsBuildComplete = true;
                }
                else
                {
                    bundle.IsBuildComplete = false;
                }
            }
        }


        // Parascript
        Settings.DirectoryName = "Parascript";
        Settings.Validate(config);

        foreach (ParaFile file in context.ParaFiles.ToList())
        {
            if (!File.Exists(Path.Combine(Settings.AddressDataPath, file.DataYearMonth, "Files.zip")))
            {
                file.OnDisk = false;
            }
        }

        foreach (ParaBundle bundle in context.ParaBundles.Include("BuildFiles").ToList())
        {
            if (!bundle.BuildFiles.All(x => x.OnDisk) || bundle.BuildFiles.Count < 2)
            {

                bundle.IsReadyForBuild = false;
            }
            else
            {
                bundle.IsReadyForBuild = true;
                logger.LogInformation($"{Settings.DirectoryName} Bundle ready to build: {bundle.DataMonth}/{bundle.DataYear}");
            }

            if (Directory.EnumerateFileSystemEntries(Path.Combine(Settings.OutputPath, bundle.DataYearMonth)).Any())
            {
                bundle.IsBuildComplete = true;
            }
            else
            {
                bundle.IsBuildComplete = false;
            }
        }


        // RoyalMail
        Settings.DirectoryName = "RoyalMail";
        Settings.Validate(config);

        foreach (RoyalFile file in context.RoyalFiles.ToList())
        {
            if (!File.Exists(Path.Combine(Settings.AddressDataPath, file.DataYearMonth, "SetupRM.exe")))
            {
                file.OnDisk = false;
            }
        }

        foreach (RoyalBundle bundle in context.RoyalBundles.Include("BuildFiles").ToList())
        {
            if (!bundle.BuildFiles.All(x => x.OnDisk) || bundle.BuildFiles.Count < 1)
            {
                bundle.IsReadyForBuild = false;
            }
            else
            {
                bundle.IsReadyForBuild = true;
                logger.LogInformation($"{Settings.DirectoryName} Bundle ready to build: {bundle.DataMonth}/{bundle.DataYear}");
            }

            if (Directory.EnumerateFileSystemEntries(Path.Combine(Settings.OutputPath, bundle.DataYearMonth, @"3.0")).Any())
            {
                bundle.IsBuildComplete = true;
            }
            else
            {
                bundle.IsBuildComplete = false;
            }
        }

        // Save changes
        context.SaveChanges();
        logger.LogInformation("Database scanned, files and bundles modified accordingly");
    }

    public void ScanFilesystem()
    {
        // SmartMatch
        Settings.DirectoryName = "SmartMatch";
        Settings.Validate(config);

        foreach (string folder in Directory.GetDirectories(Settings.AddressDataPath))
        {
            DirectoryInfo directoryInfo = new(folder);
            string dataYearMonth = directoryInfo.Name;

            foreach (string file in Directory.GetFiles(Path.Combine(folder, "Cycle-O")))
            {
                if (context.UspsFiles.Any(x => x.FileName == Path.GetFileName(file) && x.DataYearMonth == dataYearMonth && x.Cycle == "Cycle-O"))
                {
                    continue;
                }

                if (Path.GetFileName(file) == "zipmovenatl.tar" || Path.GetFileName(file) == "zip4natl.tar")
                {
                    continue;
                }

                UspsFile newFile = new()
                {
                    FileName = Path.GetFileName(file),
                    DataMonth = int.Parse(dataYearMonth.Substring(4, 2)),
                    DataYear = int.Parse(dataYearMonth[..4]),
                    DataYearMonth = dataYearMonth,
                    OnDisk = true,

                    Cycle = "Cycle-O"
                };

                context.UspsFiles.Add(newFile);
                logger.LogInformation($"File added: {newFile.FileName} {newFile.DataYearMonth} {newFile.Cycle}");
            }
            foreach (string file in Directory.GetFiles(Path.Combine(folder, "Cycle-N")))
            {
                if (context.UspsFiles.Any(x => x.FileName == Path.GetFileName(file) && x.DataYearMonth == dataYearMonth && x.Cycle == "Cycle-N"))
                {
                    continue;
                }

                UspsFile newFile = new()
                {
                    FileName = Path.GetFileName(file),
                    DataMonth = int.Parse(dataYearMonth.Substring(4, 2)),
                    DataYear = int.Parse(dataYearMonth[..4]),
                    DataYearMonth = dataYearMonth,
                    OnDisk = true,

                    Cycle = "Cycle-N"
                };

                context.UspsFiles.Add(newFile);
                logger.LogInformation($"File added: {newFile.FileName} {newFile.DataYearMonth} {newFile.Cycle}");
            }
        }

        // Parascript
        Settings.DirectoryName = "Parascript";
        Settings.Validate(config);

        foreach (string folder in Directory.GetDirectories(Settings.AddressDataPath))
        {
            DirectoryInfo directoryInfo = new(folder);
            string dataYearMonth = directoryInfo.Name;

            if (Directory.GetFiles(folder).Length > 0)
            {
                continue;
            }

            ParaFile adsFile = new()
            {
                FileName = "DPVandLACS",
                DataMonth = int.Parse(dataYearMonth.Substring(4, 2)),
                DataYear = int.Parse(dataYearMonth[..4]),
                DataYearMonth = dataYearMonth,
                OnDisk = true,
            };

            context.ParaFiles.Add(adsFile);
            logger.LogInformation($"File added: {adsFile.FileName} {adsFile.DataYearMonth}");

            ParaFile dpvFile = new()
            {
                FileName = "ads6",
                DataMonth = int.Parse(dataYearMonth.Substring(4, 2)),
                DataYear = int.Parse(dataYearMonth[..4]),
                DataYearMonth = dataYearMonth,
                OnDisk = true,
            };

            context.ParaFiles.Add(dpvFile);
            logger.LogInformation($"File added: {dpvFile.FileName} {dpvFile.DataYearMonth}");
        }

        // RoyalMail
        Settings.DirectoryName = "RoyalMail";
        Settings.Validate(config);

        foreach (string folder in Directory.GetDirectories(Settings.AddressDataPath))
        {
            DirectoryInfo directoryInfo = new(folder);
            string dataYearMonth = directoryInfo.Name;

            foreach (string file in Directory.GetFiles(folder))
            {
                if (context.RoyalFiles.Any(x => x.FileName == Path.GetFileName(file) && x.DataYearMonth == dataYearMonth))
                {
                    continue;
                }

                RoyalFile newFile = new()
                {
                    FileName = Path.GetFileName(file),
                    DataMonth = int.Parse(dataYearMonth.Substring(4, 2)),
                    DataYear = int.Parse(dataYearMonth[..4]),
                    DataYearMonth = dataYearMonth,
                    OnDisk = true,
                };

                context.RoyalFiles.Add(newFile);
                logger.LogInformation($"File added: {newFile.FileName} {newFile.DataYearMonth}");
            }
        }

        // Save changes
        context.SaveChanges();
        logger.LogInformation("Filesystem scanned, files added to Db accordingly");
    }
}
