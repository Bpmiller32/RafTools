using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Common.Data;
using Crawler.App.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

#pragma warning disable SYSLIB0014 // ignore that WebRequest and WebClient are deprecated in net6.0, replace with httpClient later

namespace Crawler.App
{
    public class RoyalCrawler : BackgroundService
    {
        private readonly ILogger logger;
        private readonly IConfiguration config;
        private readonly ComponentTask tasks;
        private readonly DatabaseContext context;

        private CancellationToken stoppingToken;
        private Settings settings = new Settings() { Name = "RoyalMail" };
        private SocketConnection connection;

        private RoyalFile tempFile = new RoyalFile();

        public RoyalCrawler(ILogger<RoyalCrawler> logger, IConfiguration config, IServiceScopeFactory factory, ComponentTask tasks)
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
                tasks.RoyalMail = ComponentStatus.Disabled;
                connection.SendMessage();
                logger.LogInformation("Crawler disabled");
                return;
            }

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    logger.LogInformation("Starting Crawler");
                    tasks.RoyalMail = ComponentStatus.InProgress;
                    connection.SendMessage();

                    PullFile();
                    CheckFile();
                    await DownloadFile();
                    CheckBuildReady();

                    tasks.RoyalMail = ComponentStatus.Ready;
                    connection.SendMessage();

                    TimeSpan waitTime = Settings.CalculateWaitTime(logger, settings);
                    await Task.Delay(TimeSpan.FromHours(waitTime.TotalHours), stoppingToken);
                }
            }
            catch (System.Exception e)
            {
                tasks.RoyalMail = ComponentStatus.Error;
                connection.SendMessage();
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

        public void CheckFile()
        {
            // Cancellation requested or PullFile failed
            if (stoppingToken.IsCancellationRequested == true)
            {
                return;
            }

            // Check if file is unique against the db
            bool fileInDb = context.RoyalFiles.Any(x => (tempFile.FileName == x.FileName) && (tempFile.DataMonth == x.DataMonth) && (tempFile.DataYear == x.DataYear));

            if (!fileInDb)
            {
                // Check if the folder exists on the disk
                if (!Directory.Exists(Path.Combine(settings.AddressDataPath, tempFile.DataYearMonth, tempFile.FileName)))
                {
                    tempFile.OnDisk = false;
                }

                // regardless of check file is unique, add to db
                context.RoyalFiles.Add(tempFile);
                logger.LogInformation("Discovered and not on disk: " + tempFile.FileName + " " + tempFile.DataMonth + "/" + tempFile.DataYear);

                bool bundleExists = context.RoyalBundles.Any(x => (tempFile.DataMonth == x.DataMonth) && (tempFile.DataYear == x.DataYear));

                if (!bundleExists)
                {
                    RoyalBundle newBundle = new RoyalBundle()
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
                    logger.LogInformation("Currently downloading: " + tempFile.FileName + " " + tempFile.DataMonth + "/" + tempFile.DataYear);
                    // Throws error is request is canceled, caught in catch
                    fileData = await request.DownloadDataTaskAsync(@"ftp://pafdownload.afd.co.uk/SetupRM.exe");
                }

                Directory.CreateDirectory(Path.Combine(settings.AddressDataPath, tempFile.DataYearMonth));

                using (FileStream file = File.Create(Path.Combine(settings.AddressDataPath, tempFile.DataYearMonth, @"SetupRM.exe")))
                {
                    file.Write(fileData, 0, fileData.Length);
                    file.Close();
                    fileData = null;
                    // TODO: assign TempFile.Size to fileData.Length / ? before assigning to null

                    tempFile.OnDisk = true;
                    tempFile.DateDownloaded = DateTime.Now;
                    context.RoyalFiles.Update(tempFile);
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
