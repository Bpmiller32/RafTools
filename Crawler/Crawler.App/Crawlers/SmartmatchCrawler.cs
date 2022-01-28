using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Crawler.Data;
using HtmlAgilityPack;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using System.Linq;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace Crawler.App
{
    public class SmartmatchCrawler : BackgroundService
    {
        private readonly ILogger<SmartmatchCrawler> logger;
        private readonly IConfiguration config;
        private readonly DatabaseContext context;
        private List<UspsFile> TempFiles = new List<UspsFile>();
        private AppSettings settings = new AppSettings();

        public SmartmatchCrawler(ILogger<SmartmatchCrawler> logger, IServiceScopeFactory factory, IConfiguration config)
        {
            this.logger = logger;
            this.config = config;
            this.context = factory.CreateScope().ServiceProvider.GetRequiredService<DatabaseContext>();
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Hello from SmCrawler!");

            context.Database.EnsureCreated();

            // Check if appsettings.json is present, set values. Also TODO, put this in AppSettings setter?
            if (!File.Exists(Directory.GetCurrentDirectory() + @"\appsettings.json"))
            {
                logger.LogError(@"File not found: appsettings.json");
                settings.ServiceEnabled = false;
                return base.StartAsync(cancellationToken);
            }
            
            if(!config.GetValue<bool>("settings:SmartMatch:ServiceEnabled"))
            {
                logger.LogWarning("SmCrawler service disabled");
                settings.ServiceEnabled = false;
                return base.StartAsync(cancellationToken);
            }

            // Should probably also add a valid check to these values later
            if (config.GetValue<string>("settings:SmartMatch:DownloadPath") != "")
            {
                settings.DownloadPath = config.GetValue<string>("settings:SmartMatch:DownloadPath");
            }

            settings.UserName = config.GetValue<string>("settings:SmartMatch:Login:User");
            settings.Password = config.GetValue<string>("settings:SmartMatch:Login:Pass");

            settings.ExecDay = config.GetValue<int>("settings:SmartMatch:ExecTime:Day");
            settings.ExecHour = config.GetValue<int>("settings:SmartMatch:ExecTime:Hour");
            settings.ExecMinute = config.GetValue<int>("settings:SmartMatch:ExecTime:Minute");
            settings.ExecSecond = config.GetValue<int>("settings:SmartMatch:ExecTime:Second");
            

            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Successfully stopped SmCrawler");

            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (settings.ServiceEnabled == false)
            {
                return;
            }

            // Set values for service sleep time
            DateTime execTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, settings.ExecDay, settings.ExecHour, settings.ExecMinute, settings.ExecSecond);
            DateTime endOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month), 23, 23, 59);
            TimeSpan waitTime = execTime - DateTime.Now;

            while (!stoppingToken.IsCancellationRequested)
            {
                await PullFiles(stoppingToken);
                CheckFiles(stoppingToken);
                await DownloadFiles(stoppingToken);
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

        private async Task PullFiles(CancellationToken stoppingToken)
        {
            if (stoppingToken.IsCancellationRequested == true)
            {
                return;
            }

            // Download local chromium binary to launch browser
            BrowserFetcher fetcher = new BrowserFetcher();
            await fetcher.DownloadAsync(BrowserFetcher.DefaultChromiumRevision);

            // Set launchoptions, create browser instance
            LaunchOptions options = new LaunchOptions() { Headless = true };

            // TODO: Register cancellationToken to browser.Close() like in Builder's SocketServer

            // Create a browser instance, page instance
            using (Browser browser = await Puppeteer.LaunchAsync(options))
            {
                using (Page page = await browser.NewPageAsync())
                {
                    try
                    {
                        // Navigate to download portal page
                        await page.GoToAsync("https://epf.usps.gov/");

                        await page.WaitForSelectorAsync(@"#email");
                        await page.FocusAsync(@"#email");
                        await page.Keyboard.TypeAsync(settings.UserName);
                        await page.FocusAsync(@"#password");
                        await page.Keyboard.TypeAsync(settings.Password);

                        await page.ClickAsync(@"#login");

                        await page.WaitForSelectorAsync(@"#r1");
                        await page.ClickAsync(@"#r1");
                        await Task.Delay(TimeSpan.FromSeconds(5));
                        await page.WaitForSelectorAsync(@"#r2");
                        await page.ClickAsync(@"#r2");
                        await Task.Delay(TimeSpan.FromSeconds(5));

                        // Has a 30s timeout, should throw exception if tbody/filelist is not found
                        await page.WaitForSelectorAsync(@"#tblFileList > tbody");

                        // Arrrived at download portal page, pull page HTML
                        HtmlDocument doc = new HtmlDocument();
                        doc.LoadHtml(page.GetContentAsync().Result);

                        HtmlNodeCollection fileRows = doc.DocumentNode.SelectNodes(@"/html/body/div[2]/table/tbody/tr/td/div[3]/table/tbody/tr/td/div/table/tbody/tr");

                        // Format downloadables into list
                        foreach (var fileRow in fileRows)
                        {
                            UspsFile file = new UspsFile();
                            file.FileName = fileRow.ChildNodes[5].InnerText.Trim();
                            file.UploadDate = DateTime.Parse(fileRow.ChildNodes[4].InnerText.Trim());
                            file.Size = fileRow.ChildNodes[6].InnerText.Trim();
                            file.OnDisk = true;

                            file.ProductKey = fileRow.Attributes[0].Value.Trim().Substring(19, 5);
                            file.FileId = fileRow.Attributes[1].Value.Trim().Substring(3, 7);

                            file.DataMonth = DateTime.Parse(fileRow.ChildNodes[4].InnerText.Trim()).Month;
                            file.DataYear = DateTime.Parse(fileRow.ChildNodes[4].InnerText.Trim()).Year;

                            if (fileRow.ChildNodes[1].InnerText.Trim() == "Downloaded")
                            {
                                file.Downloaded = true;
                            }

                            TempFiles.Add(file);
                        }

                        // Exit page, browser
                    }
                    catch (System.Exception e)
                    {
                        settings.ServiceEnabled = false;
                        logger.LogError(e.Message);
                    }
                }
            }
        }

        private void CheckFiles(CancellationToken stoppingToken)
        {
            // Cancellation requested or PullFile failed
            if ((settings.ServiceEnabled == false) || (stoppingToken.IsCancellationRequested == true))
            {
                return;
            }

            foreach (var file in TempFiles)
            {
                // Check if file is unique against the db
                bool fileInDb = context.UspsFiles.Any(x => file.FileId == x.FileId);

                if (!fileInDb)
                {
                    // Check if file exists on the disk 
                    if (!File.Exists(settings.DownloadPath + @"\SmartMatch\" + file.DataYear + @"\" + file.DataMonth + @"\" + file.FileName))
                    {
                        file.OnDisk = false;
                    }
                    // regardless of check file is unique, add to db
                    context.UspsFiles.Add(file);
                    logger.LogDebug("Discovered and not on disk: " + file.FileName + " " + file.DataMonth + "/" + file.DataYear);

                    bool bundleExists = context.UspsBundles.Any(x => (file.DataMonth == x.DataMonth) && (file.DataYear == x.DataYear));

                    if (!bundleExists)
                    {
                        UspsBundle newBundle = new UspsBundle()
                        {
                            DataMonth = file.DataMonth,
                            DataYear = file.DataYear,
                            IsReadyForBuild = false
                        };

                        newBundle.BuildFiles.Add(file);
                        context.UspsBundles.Add(newBundle);
                    }
                    else
                    {
                        UspsBundle existingBundle = context.UspsBundles.Where(x => (file.DataMonth == x.DataMonth) && (file.DataYear == x.DataYear)).FirstOrDefault();

                        existingBundle.BuildFiles.Add(file);
                    }

                    context.SaveChanges();
                }
            }

            TempFiles.Clear();
        }

        private async Task DownloadFiles(CancellationToken stoppingToken)
        {
            List<UspsFile> offDisk = context.UspsFiles.Where(x => x.OnDisk == false).ToList();

            // if all files are downloaded, no need to kick open new browser
            if ((settings.ServiceEnabled == false) || (offDisk.Count == 0) || stoppingToken.IsCancellationRequested == true)
            {
                return;
            }

            logger.LogInformation("New files found for download: " + offDisk.Count);

            foreach (var file in offDisk)
            {
                // Ensure there is a folder to land in (this will punch through recursively btw, Downloads gets created as well if does not exist)
                Directory.CreateDirectory(settings.DownloadPath + @"\SmartMatch\" + file.DataYear + @"\" + file.DataMonth);
                Cleanup(settings.DownloadPath + @"\SmartMatch\" + file.DataYear + @"\" + file.DataMonth, stoppingToken);
            }

            // Download local chromium binary to launch browser
            BrowserFetcher fetcher = new BrowserFetcher();
            await fetcher.DownloadAsync(BrowserFetcher.DefaultChromiumRevision);

            // Set launchoptions, create browser instance
            LaunchOptions options = new LaunchOptions() { Headless = true };

            // Create a browser instance, page instance
            using (Browser browser = await Puppeteer.LaunchAsync(options))
            {
                using (Page page = await browser.NewPageAsync())
                {
                    try
                    {
                        // Navigate to download portal page
                        await page.GoToAsync("https://epf.usps.gov/");

                        await page.WaitForSelectorAsync(@"#email");
                        await page.FocusAsync(@"#email");
                        await page.Keyboard.TypeAsync("billy.miller@raf.com");
                        await page.FocusAsync(@"#password");
                        await page.Keyboard.TypeAsync("Trixiedog10021002$");

                        await page.ClickAsync(@"#login");

                        await page.WaitForSelectorAsync(@"#r1");
                        await page.ClickAsync(@"#r1");
                        await Task.Delay(TimeSpan.FromSeconds(5));
                        await page.WaitForSelectorAsync(@"#r2");
                        await page.ClickAsync(@"#r2");
                        await Task.Delay(TimeSpan.FromSeconds(5));

                        await page.WaitForSelectorAsync(@"#tblFileList > tbody");


                        // Changed behavior, start download and wait for indivdual file to download. USPS website corrupts downloads if you do them all at once sometimes
                        foreach (var file in offDisk)
                        {
                            string path = settings.DownloadPath + @"\SmartMatch\" + file.DataYear + @"\" + file.DataMonth;
                            await page.Client.SendAsync(@"Page.setDownloadBehavior", new { behavior = @"allow", downloadPath = path });
                            
                            await page.EvaluateExpressionAsync(@"getFileForDownload(" + file.ProductKey + "," + file.FileId + ",rw_" + file.FileId + ");");
                            await Task.Delay(TimeSpan.FromSeconds(5));
                        
                            logger.LogInformation("Currently downloading: " + file.FileName + " " + file.DataMonth + "/" + file.DataYear);
                            await WaitForDownload(file, stoppingToken);
                        }
                    }
                    catch (System.Exception e)
                    {
                        settings.ServiceEnabled = false;
                        logger.LogError(e.Message);
                    }
                }
            }
        }

        private void CheckBuildReady(CancellationToken stoppingToken)
        {
            if ((settings.ServiceEnabled == false) || (stoppingToken.IsCancellationRequested == true))
            {
                return;
            }

            List<UspsBundle> bundles = context.UspsBundles.ToList();

            foreach (var bundle in bundles)
            {
                // idk why but you need to do some linq query to populate bundle.buildfiles? 
                // Something to do with one -> many relationship between the tables, investigate
                List<UspsFile> files = context.UspsFiles.Where(x => (x.DataMonth == bundle.DataMonth) && (x.DataYear == bundle.DataYear)).ToList();

                if (!bundle.BuildFiles.Any(x => x.OnDisk == false) && bundle.BuildFiles.Count >= 6)
                {
                    bundle.IsReadyForBuild = true;
                    logger.LogInformation("Bundle ready to build: " + bundle.DataMonth + "/" + bundle.DataYear);
                }

                context.SaveChanges();
            }
        }
    
        private async Task WaitForDownload(UspsFile file, CancellationToken stoppingToken)
        {
            if (stoppingToken.IsCancellationRequested == true)
            {
                logger.LogInformation("Download in progress was stopped due to cancellation");
                return;
            }

            string path = settings.DownloadPath + @"\SmartMatch\" + file.DataYear + @"\" + file.DataMonth;
            if (!File.Exists(path + @"\" + file.FileName + @".CRDOWNLOAD"))
            {
                // logger.LogInformation("Finished downloading");
                file.OnDisk = true;
                file.DateDownloaded = DateTime.Now;
                context.UspsFiles.Update(file);
                context.SaveChanges();
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(5));
            await WaitForDownload(file, stoppingToken);
        }
    
        private void Cleanup(string path, CancellationToken stoppingToken)
        {
            if (stoppingToken.IsCancellationRequested == true)
            {
                return;
            }

            // Ensure there is a folder to land in (this will punch through recursively btw, Downloads gets created as well if does not exist)
            Directory.CreateDirectory(path);

            // Cleanup from previous run
            DirectoryInfo op = new DirectoryInfo(path);

            foreach (var file in op.GetFiles())
            {
                file.Delete();
            }
            foreach (var dir in op.GetDirectories())
            {
                dir.Delete(true);
            }
        }
    }
}
