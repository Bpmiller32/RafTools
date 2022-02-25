using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Crawler.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Crawler.App
{
    public class RoyalCrawler : BackgroundService
    {
        private readonly ILogger logger;
        private readonly IConfiguration config;
        private readonly CrawlTask tasks;
        private readonly DatabaseContext context;

        private CancellationToken stoppingToken;
        private Settings settings = new Settings() { Name = "RoyalMail" };

        private RoyalFile TempFile = new RoyalFile();
        private string dataYearMonth = "";

        public RoyalCrawler(ILogger<RoyalCrawler> logger, IConfiguration config, IServiceScopeFactory factory, CrawlTask tasks)
        {
            this.logger = logger;
            this.config = config;
            this.tasks = tasks;
            this.context = factory.CreateScope().ServiceProvider.GetRequiredService<DatabaseContext>();
        }

        protected override async Task ExecuteAsync(CancellationToken serviceStoppingToken)
        {
            stoppingToken = serviceStoppingToken;
            settings = Settings.Validate(settings, config);

            if (settings.CrawlerEnabled == false)
            {
                tasks.RoyalMail = CrawlStatus.Disabled;
                logger.LogInformation("Crawler disabled");
                return;
            }

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    logger.LogInformation("Starting Crawler");
                    tasks.RoyalMail = CrawlStatus.Enabled;

                    PullFile();
                    CheckFile();
                    await DownloadFile();
                    CheckBuildReady();

                    TimeSpan waitTime = Settings.CalculateWaitTime(logger, settings);
                    await Task.Delay(TimeSpan.FromHours(waitTime.TotalHours), stoppingToken);
                }
            }
            catch (System.Exception e)
            {
                tasks.RoyalMail = CrawlStatus.Error;
                logger.LogError(e.Message);
            }
        }

        public void PullFile()
        {
            if (stoppingToken.IsCancellationRequested == true)
            {
                return;
            }

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(@"ftp://pafdownload.afd.co.uk/SetupRM.exe");
            request.Credentials = new NetworkCredential(settings.UserName, settings.Password);
            request.Method = WebRequestMethods.Ftp.GetDateTimestamp;

            DateTime lastModified;

            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
            {
                lastModified = response.LastModified;
            }

            TempFile.FileName = "SetupRM.exe";
            TempFile.DataMonth = lastModified.Month;
            TempFile.DataDay = lastModified.Day;
            TempFile.DataYear = lastModified.Year;
        }

        public void CheckFile()
        {
            // Cancellation requested or PullFile failed
            if (stoppingToken.IsCancellationRequested == true)
            {
                return;
            }

            // Set dataYearMonth here in case the file is in db but not on disk
            dataYearMonth = SetDataYearMonth(TempFile);

            // Check if file is unique against the db
            bool fileInDb = context.RoyalFiles.Any(x => (TempFile.FileName == x.FileName) && (TempFile.DataMonth == x.DataMonth) && (TempFile.DataYear == x.DataYear));

            if (!fileInDb)
            {
                // Check if the folder exists on the disk
                if (!Directory.Exists(Path.Combine(settings.AddressDataPath, dataYearMonth, TempFile.FileName)))
                {
                    TempFile.OnDisk = false;
                }

                // regardless of check file is unique, add to db
                context.RoyalFiles.Add(TempFile);
                logger.LogInformation("Discovered and not on disk: " + TempFile.FileName + " " + TempFile.DataMonth + "/" + TempFile.DataYear);

                bool bundleExists = context.RoyalBundles.Any(x => (TempFile.DataMonth == x.DataMonth) && (TempFile.DataYear == x.DataYear));

                if (!bundleExists)
                {
                    RoyalBundle newBundle = new RoyalBundle()
                    {
                        DataMonth = TempFile.DataMonth,
                        DataYear = TempFile.DataYear,
                        IsReadyForBuild = false
                    };

                    newBundle.BuildFiles.Add(TempFile);
                    context.RoyalBundles.Add(newBundle);
                }
                else
                {
                    RoyalBundle existingBundle = context.RoyalBundles.Where(x => (TempFile.DataMonth == x.DataMonth) && (TempFile.DataYear == x.DataYear)).FirstOrDefault();

                    existingBundle.BuildFiles.Add(TempFile);
                }

                context.SaveChanges();
            }
        }

        public async Task DownloadFile()
        {
            List<RoyalFile> offDisk = context.RoyalFiles.Where(x => x.OnDisk == false).ToList();

            // Cancellation requested, CheckFile sees that nothing is offDisk, PullFile failed
            if (offDisk.Count == 0 || stoppingToken.IsCancellationRequested == true)
            {
                return;
            }

            logger.LogInformation("New files found for download: " + offDisk.Count);

            using (WebClient request = new WebClient())
            {
                request.Credentials = new NetworkCredential(settings.UserName, settings.Password);
                byte[] fileData;

                using (CancellationTokenRegistration registration = stoppingToken.Register(() => request.CancelAsync()))
                {
                    logger.LogInformation("Currently downloading: " + TempFile.FileName + " " + TempFile.DataMonth + "/" + TempFile.DataYear);
                    // Throws error is request is canceled, caught in catch
                    fileData = await request.DownloadDataTaskAsync(@"ftp://pafdownload.afd.co.uk/SetupRM.exe");
                }

                Directory.CreateDirectory(Path.Combine(settings.AddressDataPath, dataYearMonth));

                using (FileStream file = File.Create(Path.Combine(settings.AddressDataPath, dataYearMonth, @"SetupRM.exe")))
                {
                    file.Write(fileData, 0, fileData.Length);
                    file.Close();
                    fileData = null;

                    TempFile.OnDisk = true;
                    TempFile.DateDownloaded = DateTime.Now;
                    context.RoyalFiles.Update(TempFile);
                    context.SaveChanges();
                }
            }
        }

        public void CheckBuildReady()
        {
            if (stoppingToken.IsCancellationRequested == true)
            {
                return;
            }

            List<RoyalBundle> bundles = context.RoyalBundles.ToList();

            foreach (var bundle in bundles)
            {
                // idk why but you need to do some linq query to populate bundle.buildfiles? 
                // Something to do with one -> many relationship between the tables, investigate
                List<RoyalFile> files = context.RoyalFiles.Where(x => (x.DataMonth == bundle.DataMonth) && (x.DataYear == bundle.DataYear)).ToList();

                if (!bundle.BuildFiles.Any(x => x.OnDisk == false) && bundle.BuildFiles.Count >= 1)
                {
                    bundle.IsReadyForBuild = true;
                    logger.LogInformation("Bundle ready to build: " + bundle.DataMonth + "/" + bundle.DataYear);
                }

                context.SaveChanges();
            }
        }

        private string SetDataYearMonth(RoyalFile file)
        {
            if (file.DataMonth < 10)
            {
                return file.DataYear.ToString() + "0" + file.DataMonth.ToString();
            }

            return file.DataYear.ToString() + file.DataMonth.ToString();
        }
    }
}
