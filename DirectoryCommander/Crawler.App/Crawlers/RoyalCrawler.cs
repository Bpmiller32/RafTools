using System.Net;
using Common.Data;

// TODO: switch to httpClient? There is no replacement for FtpRequest....
#pragma warning disable SYSLIB0014 // ignore that WebRequest and WebClient are deprecated in net6.0, replace with httpClient later

namespace Crawler;

public class RoyalCrawler
{
    public Settings Settings { get; set; } = new Settings { Name = "RoyalMail" };
    public ComponentStatus Status { get; set; }
    public Action<DirectoryType, DatabaseContext> SendMessage { get; set; }

    private readonly ILogger<RoyalCrawler> logger;
    private readonly DatabaseContext context;

    private readonly RoyalFile tempFile = new();

    public RoyalCrawler(ILogger<RoyalCrawler> logger, IConfiguration config, DatabaseContext context)
    {
        this.logger = logger;
        this.context = context;

        Settings = Settings.Validate(Settings, config);
    }

    public async Task ExecuteAsyncAuto(CancellationToken stoppingToken)
    {
        SendMessage(DirectoryType.RoyalMail, context);

        if (!Settings.CrawlerEnabled)
        {
            logger.LogInformation("Crawler disabled");
            Status = ComponentStatus.Disabled;
            SendMessage(DirectoryType.RoyalMail, context);
            return;
        }
        if (!Settings.AutoCrawlEnabled)
        {
            logger.LogDebug("AutoCrawl disabled");
            return;
        }

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Starting Crawler - Auto mode");
                TimeSpan waitTime = Settings.CalculateWaitTime(logger, Settings);
                await Task.Delay(TimeSpan.FromHours(waitTime.TotalHours), stoppingToken);

                await ExecuteAsync(stoppingToken);
            }
        }
        catch (TaskCanceledException e)
        {
            logger.LogDebug("{Message}", e.Message);
        }
        catch (System.Exception e)
        {
            logger.LogError("{Message}", e.Message);
        }
    }

    public async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation("Starting Crawler");
            Status = ComponentStatus.InProgress;
            SendMessage(DirectoryType.RoyalMail, context);

            PullFile(stoppingToken);
            CheckFile(stoppingToken);
            await DownloadFile(stoppingToken);
            CheckBuildReady(stoppingToken);

            Status = ComponentStatus.Ready;
        }
        catch (TaskCanceledException e)
        {
            Status = ComponentStatus.Ready;
            SendMessage(DirectoryType.RoyalMail, context);
            logger.LogDebug("{Message}", e.Message);
        }
        catch (Exception e)
        {
            Status = ComponentStatus.Error;
            SendMessage(DirectoryType.RoyalMail, context);
            logger.LogError("{Message}", e.Message);
        }
    }

    public void PullFile(CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        FtpWebRequest request = (FtpWebRequest)WebRequest.Create("ftp://pafdownload.afd.co.uk/SetupRM.exe");
        request.Credentials = new NetworkCredential(Settings.UserName, Settings.Password);
        request.Method = WebRequestMethods.Ftp.GetDateTimestamp;

        DateTime lastModified;

        using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
        {
            lastModified = response.LastModified;
        }

        tempFile.FileName = "SetupRM.exe";
        tempFile.DataMonth = lastModified.Month;
        tempFile.DataDay = lastModified.Day;
        tempFile.DataYear = lastModified.Year;

        if (tempFile.DataMonth < 10)
        {
            tempFile.DataYearMonth = tempFile.DataYear.ToString() + "0" + tempFile.DataMonth.ToString();
        }
        else
        {
            tempFile.DataYearMonth = tempFile.DataYear.ToString() + tempFile.DataMonth.ToString();
        }
    }

    public void CheckFile(CancellationToken stoppingToken)
    {
        // Cancellation requested or PullFile failed
        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        // Check if file is unique against the db
        bool fileInDb = context.RoyalFiles.Any(x => (tempFile.FileName == x.FileName) && (tempFile.DataMonth == x.DataMonth) && (tempFile.DataYear == x.DataYear));

        if (!fileInDb)
        {
            // Check if the folder exists on the disk
            if (!Directory.Exists(Path.Combine(Settings.AddressDataPath, tempFile.DataYearMonth, tempFile.FileName)))
            {
                tempFile.OnDisk = false;
            }

            // regardless of check file is unique, add to db
            context.RoyalFiles.Add(tempFile);
            logger.LogInformation("Discovered and not on disk: {FileName} {DataMonth}/{DataYear}", tempFile.FileName, tempFile.DataMonth, tempFile.DataYear);

            bool bundleExists = context.RoyalBundles.Any(x => (tempFile.DataMonth == x.DataMonth) && (tempFile.DataYear == x.DataYear));

            if (!bundleExists)
            {
                RoyalBundle newBundle = new()
                {
                    DataMonth = tempFile.DataMonth,
                    DataYear = tempFile.DataYear,
                    DataYearMonth = tempFile.DataYearMonth,
                    IsReadyForBuild = false
                };

                newBundle.BuildFiles.Add(tempFile);
                context.RoyalBundles.Add(newBundle);
            }
            else
            {
                RoyalBundle existingBundle = context.RoyalBundles.Where(x => (tempFile.DataMonth == x.DataMonth) && (tempFile.DataYear == x.DataYear)).FirstOrDefault();

                existingBundle.BuildFiles.Add(tempFile);
            }

            context.SaveChanges();
        }
    }

    public async Task DownloadFile(CancellationToken stoppingToken)
    {
        List<RoyalFile> offDisk = context.RoyalFiles.Where(x => !x.OnDisk).ToList();

        // Cancellation requested, CheckFile sees that nothing is offDisk, PullFile failed
        if (offDisk.Count == 0 || stoppingToken.IsCancellationRequested)
        {
            return;
        }

        logger.LogInformation("New files found for download: {Count}", offDisk.Count);

        using WebClient request = new();
        request.Credentials = new NetworkCredential(Settings.UserName, Settings.Password);
        byte[] fileData;

        using (CancellationTokenRegistration registration = stoppingToken.Register(() => request.CancelAsync()))
        {
            logger.LogInformation("Currently downloading: {FileName} {DataMonth}/{DataYear}", tempFile.FileName, tempFile.DataMonth, tempFile.DataYear);
            // Throws error is request is canceled, caught in catch
            fileData = await request.DownloadDataTaskAsync("ftp://pafdownload.afd.co.uk/SetupRM.exe");
        }

        Directory.CreateDirectory(Path.Combine(Settings.AddressDataPath, tempFile.DataYearMonth));

        using FileStream file = File.Create(Path.Combine(Settings.AddressDataPath, tempFile.DataYearMonth, "SetupRM.exe"));
        file.Write(fileData, 0, fileData.Length);
        file.Close();
        fileData = null;
        // TODO: assign TempFile.Size to fileData.Length / ? before assigning to null

        tempFile.OnDisk = true;
        tempFile.DateDownloaded = DateTime.Now;
        context.RoyalFiles.Update(tempFile);
        context.SaveChanges();
    }

    public void CheckBuildReady(CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        foreach (var bundle in context.RoyalBundles.ToList())
        {
            // idk why but you need to do some linq query to populate bundle.buildfiles? 
            // Something to do with one -> many relationship between the tables, investigate
            List<RoyalFile> files = context.RoyalFiles.Where(x => (x.DataMonth == bundle.DataMonth) && (x.DataYear == bundle.DataYear)).ToList();

            if (bundle.BuildFiles.All(x => x.OnDisk) && bundle.BuildFiles.Count >= 1)
            {
                bundle.IsReadyForBuild = true;

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
                bundle.DownloadDate = timestamp.Month.ToString() + "/" + timestamp.Day + "/" + timestamp.Year.ToString();
                bundle.DownloadTime = hour + ":" + minute + ampm;
                bundle.FileCount = bundle.BuildFiles.Count;

                logger.LogInformation("Bundle ready to build: {DataMonth}/{DataYear}", bundle.DataMonth, bundle.DataYear);
            }

            context.SaveChanges();
        }
    }
}
