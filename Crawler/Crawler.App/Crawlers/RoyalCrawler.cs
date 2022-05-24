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
using Microsoft.Extensions.Logging;

#pragma warning disable SYSLIB0014 // ignore that WebRequest and WebClient are deprecated in net6.0, replace with httpClient later
// TODO: switch to httpClient

namespace Crawler.App
{
    public class RoyalCrawler
    {
        public Settings Settings { get; set; } = new Settings { Name = "RoyalMail" };

        private readonly ILogger<RoyalCrawler> logger;
        private readonly IConfiguration config;
        private readonly ComponentTask tasks;
        private readonly SocketConnection connection;
        private readonly DatabaseContext context;

        private RoyalFile tempFile = new RoyalFile();

        public RoyalCrawler(ILogger<RoyalCrawler> logger, IConfiguration config, ComponentTask tasks, SocketConnection connection, DatabaseContext context)
        {
            this.logger = logger;
            this.config = config;
            this.tasks = tasks;
            this.connection = connection;
            this.context = context;

            Settings = Settings.Validate(Settings, config);
        }

        public async Task ExecuteAsyncAuto(CancellationToken stoppingToken)
        {
            connection.SendMessage(royalMail: true);

            if (Settings.CrawlerEnabled == false)
            {
                logger.LogInformation("Crawler disabled");
                tasks.RoyalMail = ComponentStatus.Disabled;
                connection.SendMessage(royalMail: true);
                return;
            }
            if (Settings.AutoCrawlEnabled == false)
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

                    await this.ExecuteAsync(stoppingToken);
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

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                logger.LogInformation("Starting Crawler");
                tasks.RoyalMail = ComponentStatus.InProgress;
                connection.SendMessage(royalMail: true);

                PullFile(stoppingToken);
                CheckFile(stoppingToken);
                await DownloadFile(stoppingToken);
                CheckBuildReady(stoppingToken);

                tasks.RoyalMail = ComponentStatus.Ready;
                // connection.SendMessage(royalMail: true);
            }
            catch (TaskCanceledException e)
            {
                tasks.RoyalMail = ComponentStatus.Ready;
                connection.SendMessage(royalMail: true);
                logger.LogDebug(e.Message);
            }
            catch (System.Exception e)
            {
                tasks.RoyalMail = ComponentStatus.Error;
                connection.SendMessage(royalMail: true);
                logger.LogError(e.Message);
            }
        }

        public void PullFile(CancellationToken stoppingToken)
        {
            if (stoppingToken.IsCancellationRequested == true)
            {
                return;
            }

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(@"ftp://pafdownload.afd.co.uk/SetupRM.exe");
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
            if (stoppingToken.IsCancellationRequested == true)
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

        public async Task DownloadFile(CancellationToken stoppingToken)
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
                request.Credentials = new NetworkCredential(Settings.UserName, Settings.Password);
                byte[] fileData;

                using (CancellationTokenRegistration registration = stoppingToken.Register(() => request.CancelAsync()))
                {
                    logger.LogInformation("Currently downloading: " + tempFile.FileName + " " + tempFile.DataMonth + "/" + tempFile.DataYear);
                    // Throws error is request is canceled, caught in catch
                    fileData = await request.DownloadDataTaskAsync(@"ftp://pafdownload.afd.co.uk/SetupRM.exe");
                }

                Directory.CreateDirectory(Path.Combine(Settings.AddressDataPath, tempFile.DataYearMonth));

                using (FileStream file = File.Create(Path.Combine(Settings.AddressDataPath, tempFile.DataYearMonth, @"SetupRM.exe")))
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

        public void CheckBuildReady(CancellationToken stoppingToken)
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
