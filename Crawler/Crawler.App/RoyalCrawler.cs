using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private readonly ILogger<RoyalCrawler> logger;
        private readonly IConfiguration config;
        private readonly DatabaseContext context;
        private RoyalFile TempFile = new RoyalFile();
        private AppSettings settings = new AppSettings();

        public RoyalCrawler(ILogger<RoyalCrawler> logger, IServiceScopeFactory factory, IConfiguration config)
        {
            this.logger = logger;
            this.config = config;
            this.context = factory.CreateScope().ServiceProvider.GetRequiredService<DatabaseContext>();
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Hello from RoyalCrawler!");
            context.Database.EnsureCreated();

            // Check if appsettings.json is present, set values. Also TODO, put this in AppSettings setter?
            if (File.Exists(Directory.GetCurrentDirectory() + @"\appsettings.json"))
            {
                settings.ServiceEnabled = config.GetValue<bool>("settings:ServiceEnabled:RoyalMail");
                // Should probably also add a valid check to these values later
                if (config.GetValue<string>("settings:DownloadPath:RoyalMail") != "")
                {
                    settings.DownloadPath = config.GetValue<string>("settings:DownloadPath:RoyalMail");
                }
                settings.ExecDay = config.GetValue<int>("settings:ExecTime:RoyalMail:Day");
                settings.ExecHour = config.GetValue<int>("settings:ExecTime:RoyalMail:Hour");
                settings.ExecMinute = config.GetValue<int>("settings:ExecTime:RoyalMail:Minute");
                settings.ExecSecond = config.GetValue<int>("settings:ExecTime:RoyalMail:Second");
            }

            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Successfully stopped RoyalCrawler");

            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (settings.ServiceEnabled == false)
            {
                CancellationTokenSource ts = new CancellationTokenSource();
                stoppingToken = ts.Token;
                ts.Cancel();
                                
                logger.LogWarning("RoyalCrawler service disabled");
            }

            // Set values for service sleep time
            DateTime execTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, settings.ExecDay, settings.ExecHour, settings.ExecMinute, settings.ExecSecond);
            DateTime endOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month), 23, 23, 59);
            TimeSpan waitTime = execTime - DateTime.Now;

            while (!stoppingToken.IsCancellationRequested)
            {
                PullFile(stoppingToken);
                CheckFile(stoppingToken);
                await DownloadFile(stoppingToken);
                CheckBuildReady(stoppingToken);

                waitTime = execTime - DateTime.Now;
                if (waitTime.TotalSeconds <= 0)
                {
                    waitTime = (endOfMonth - DateTime.Now) + TimeSpan.FromSeconds(5);
                    logger.LogInformation("Pass completed, starting sleep until: " + endOfMonth);
                }
                else
                {
                    logger.LogInformation("Waiting for pass, starting sleep until: " + execTime);                    
                }

                await Task.Delay(TimeSpan.FromHours(waitTime.TotalHours), stoppingToken);
            }
        }

        private void PullFile(CancellationToken stoppingToken)
        {
            if (stoppingToken.IsCancellationRequested == true)
            {
                return;
            }

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(@"ftp://pafdownload.afd.co.uk/SetupRM.exe");
            request.Credentials = new NetworkCredential(@"S30145074-138", @"N49LB0TjJhLQhdoY");
            request.Method = WebRequestMethods.Ftp.GetDateTimestamp;
            
            DateTime lastModified;

            try
            {
                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    lastModified = response.LastModified;             
                }
            
                TempFile.FileName = "SetupRM.exe";
                TempFile.DataMonth = lastModified.Month;
                TempFile.DataDay = lastModified.Day;
                TempFile.DataYear = lastModified.Year;
            }
            catch (System.Exception e)
            {
                logger.LogError(e.Message);
            }
        }

        private void CheckFile(CancellationToken stoppingToken)
        {
            if ((TempFile.FileName == null) || (stoppingToken.IsCancellationRequested == true))
            {
                return;
            }

            // Check if file is unique against the db
            bool fileInDb = context.RoyalFiles.Any(x => (TempFile.FileName == x.FileName) && (TempFile.DataMonth == x.DataMonth) && (TempFile.DataYear == x.DataYear));

            if (!fileInDb)
            {
                // Check if the folder exists on the disk
                if (!Directory.Exists(settings.DownloadPath + @"\RoyalMail\" + TempFile.DataYear + @"\" + TempFile.DataMonth + @"\" + TempFile.FileName))
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

        private async Task DownloadFile(CancellationToken stoppingToken)
        {
            List<RoyalFile> offDisk = context.RoyalFiles.Where(x => x.OnDisk == false).ToList();

            if ((TempFile.FileName == null) || (offDisk.Count == 0) || stoppingToken.IsCancellationRequested == true)
            {
                return;
            }

            logger.LogInformation("New files found for download: " + offDisk.Count);

            try
            {
                using (WebClient request = new WebClient())
                {
                    request.Credentials = new NetworkCredential(@"S30145074-138", @"N49LB0TjJhLQhdoY");
                    byte[] fileData;


                    using (CancellationTokenRegistration registration = stoppingToken.Register(() => request.CancelAsync()))
                    {
                        logger.LogInformation("Currently downloading: " + TempFile.FileName + " " + TempFile.DataMonth + "/" + TempFile.DataYear);
                        // Throws error is request is canceled, caught in catch
                        fileData = await request.DownloadDataTaskAsync(@"ftp://pafdownload.afd.co.uk/SetupRM.exe"); 
                    }

                    Directory.CreateDirectory(settings.DownloadPath + @"\RoyalMail\" + TempFile.DataYear + @"\" + TempFile.DataMonth);

                    using (FileStream file = File.Create(settings.DownloadPath + @"\RoyalMail\" + TempFile.DataYear + @"\" + TempFile.DataMonth + @"\SetupRM.exe"))
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
            catch (System.Net.WebException)
            {
                logger.LogInformation("Download in progress was stopped due to cancellation");
            }
            catch (System.Exception e)
            {
                logger.LogError(e.Message);
            }
        }
    
        private void CheckBuildReady(CancellationToken stoppingToken)
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
    }
}
