using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Crawler.Data;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using System.Linq;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Crawler.App
{
    public class SmartmatchCrawler : BackgroundService
    {
        private readonly ILogger<SmartmatchCrawler> logger;
        private readonly IConfiguration config;
        private readonly CrawlTask tasks;
        private readonly DatabaseContext context;

        private CancellationToken stoppingToken;
        private Settings settings = new Settings() { Name = "SmartMatch" };

        private List<UspsFile> TempFiles = new List<UspsFile>();
        private string dataYearMonth = "";

        public SmartmatchCrawler(ILogger<SmartmatchCrawler> logger, IConfiguration config, IServiceScopeFactory factory, CrawlTask tasks)
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
                tasks.SmartMatch = CrawlStatus.Disabled;
                logger.LogInformation("Crawler disabled");
                return;
            }

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    logger.LogInformation("Starting Crawler");
                    tasks.SmartMatch = CrawlStatus.Enabled;

                    await PullFiles();
                    CheckFiles();
                    await DownloadFiles();
                    CheckBuildReady();

                    TimeSpan waitTime = Settings.CalculateWaitTime(logger, settings);
                    await Task.Delay(TimeSpan.FromHours(waitTime.TotalHours), stoppingToken);
                }
            }
            catch (System.Exception e)
            {
                tasks.SmartMatch = CrawlStatus.Error;
                logger.LogError(e.Message);
            }
        }

        private async Task PullFiles()
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

            // Create a browser instance, page instance
            using (Browser browser = await Puppeteer.LaunchAsync(options))
            {
                using (stoppingToken.Register(async () => await browser.CloseAsync()))
                {
                    using (Page page = await browser.NewPageAsync())
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

                            // New logic needed because epf behavior change, cannot G00424KL files because of access change + Cycle O added
                            string productDescription = fileRow.ChildNodes[2].InnerText.Trim();
                            if (productDescription.Contains(@"Cycle O"))
                            {
                                file.Cycle = "Cycle-O";
                            }
                            else
                            {
                                file.Cycle = "Cycle-N";
                            }

                            if (file.FileName == "G00424KL.ERZ.ZIP" || file.FileName == "G00424KL.ZIP")
                            {
                                continue;
                            }

                            TempFiles.Add(file);
                        }

                        // Exit page, browser
                    }
                }
            }
        }

        private void CheckFiles()
        {
            // Cancellation requested or PullFile failed
            if (stoppingToken.IsCancellationRequested == true)
            {
                return;
            }

            foreach (var file in TempFiles)
            {
                // Set dataYearMonth here in case the file is in db but not on disk
                dataYearMonth = SetDataYearMonth(file);

                // Check if file is unique against the db
                bool fileInDb = context.UspsFiles.Any(x => file.FileId == x.FileId);

                if (!fileInDb)
                {
                    // Check if file exists on the disk 
                    if (!File.Exists(Path.Combine(settings.AddressDataPath, dataYearMonth, file.Cycle, file.FileName)))
                    {
                        file.OnDisk = false;
                    }
                    // regardless of check file is unique, add to db
                    context.UspsFiles.Add(file);
                    logger.LogDebug("Discovered and not on disk: " + file.FileName + " " + file.DataMonth + "/" + file.DataYear + " " + file.Cycle);

                    bool bundleExists = context.UspsBundles.Any(x => (file.DataMonth == x.DataMonth) && (file.DataYear == x.DataYear) && (file.Cycle == x.Cycle));

                    if (!bundleExists)
                    {
                        UspsBundle newBundle = new UspsBundle()
                        {
                            DataMonth = file.DataMonth,
                            DataYear = file.DataYear,
                            Cycle = file.Cycle,
                            IsReadyForBuild = false
                        };

                        newBundle.BuildFiles.Add(file);
                        context.UspsBundles.Add(newBundle);
                    }
                    else
                    {
                        UspsBundle existingBundle = context.UspsBundles.Where(x => (file.DataMonth == x.DataMonth) && (file.DataYear == x.DataYear) && (file.Cycle == x.Cycle)).FirstOrDefault();

                        existingBundle.BuildFiles.Add(file);
                    }

                    context.SaveChanges();
                }
            }

            TempFiles.Clear();
        }

        private async Task DownloadFiles()
        {
            List<UspsFile> offDisk = context.UspsFiles.Where(x => x.OnDisk == false).ToList();

            // if all files are downloaded, no need to kick open new browser
            if (offDisk.Count == 0 || stoppingToken.IsCancellationRequested == true)
            {
                return;
            }

            logger.LogInformation("New files found for download: " + offDisk.Count);

            foreach (UspsFile file in offDisk)
            {
                // Ensure there is a folder to land in (this will punch through recursively btw, Downloads gets created as well if does not exist)
                Directory.CreateDirectory(Path.Combine(settings.AddressDataPath, dataYearMonth, file.Cycle));
                Cleanup(Path.Combine(settings.AddressDataPath, dataYearMonth, file.Cycle));
            }

            // Download local chromium binary to launch browser
            BrowserFetcher fetcher = new BrowserFetcher();
            await fetcher.DownloadAsync(BrowserFetcher.DefaultChromiumRevision);

            // Set launchoptions, create browser instance
            LaunchOptions options = new LaunchOptions() { Headless = true };

            // Create a browser instance, page instance
            using (Browser browser = await Puppeteer.LaunchAsync(options))
            {
                using (stoppingToken.Register(async () => await browser.CloseAsync()))
                {
                    using (Page page = await browser.NewPageAsync())
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

                        await page.WaitForSelectorAsync(@"#tblFileList > tbody");

                        // Changed behavior, start download and wait for indivdual file to download. USPS website corrupts downloads if you do them all at once sometimes
                        foreach (var file in offDisk)
                        {
                            string path = Path.Combine(settings.AddressDataPath, dataYearMonth, file.Cycle);
                            await page.Client.SendAsync(@"Page.setDownloadBehavior", new { behavior = @"allow", downloadPath = path });

                            await page.EvaluateExpressionAsync(@"getFileForDownload(" + file.ProductKey + "," + file.FileId + ",rw_" + file.FileId + ");");
                            await Task.Delay(TimeSpan.FromSeconds(5));

                            logger.LogInformation("Currently downloading: " + file.FileName + " " + file.DataMonth + "/" + file.DataYear + " " + file.Cycle);
                            await WaitForDownload(file);
                        }
                    }
                }
            }
        }

        private void CheckBuildReady()
        {
            if (stoppingToken.IsCancellationRequested == true)
            {
                return;
            }

            List<UspsBundle> bundles = context.UspsBundles.ToList();

            foreach (var bundle in bundles)
            {
                // idk why but you need to do some linq query to populate bundle.buildfiles? 
                // Something to do with one -> many relationship between the tables, investigate
                List<UspsFile> files = context.UspsFiles.Where(x => (x.DataMonth == bundle.DataMonth) && (x.DataYear == bundle.DataYear) && (x.Cycle == bundle.Cycle)).ToList();

                if (!bundle.BuildFiles.Any(x => x.OnDisk == false) && bundle.BuildFiles.Count >= 6)
                {
                    bundle.IsReadyForBuild = true;
                    logger.LogInformation("Bundle ready to build: " + bundle.DataMonth + "/" + bundle.DataYear + " " + bundle.Cycle);
                }

                context.SaveChanges();
            }
        }

        private async Task WaitForDownload(UspsFile file)
        {
            if (stoppingToken.IsCancellationRequested == true)
            {
                logger.LogInformation("Download in progress was stopped due to cancellation");
                return;
            }

            string path = Path.Combine(settings.AddressDataPath, dataYearMonth, file.Cycle);
            if (!File.Exists(Path.Combine(path, file.FileName + @".CRDOWNLOAD")))
            {
                logger.LogDebug("Finished downloading");
                file.OnDisk = true;
                file.DateDownloaded = DateTime.Now;
                context.UspsFiles.Update(file);
                context.SaveChanges();
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(5));
            await WaitForDownload(file);
        }

        private void Cleanup(string path)
        {
            if (stoppingToken.IsCancellationRequested == true)
            {
                return;
            }

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

        private string SetDataYearMonth(UspsFile file)
        {
            if (file.DataMonth < 10)
            {
                return file.DataYear.ToString() + "0" + file.DataMonth.ToString();
            }

            return file.DataYear.ToString() + file.DataMonth.ToString();
        }
    }
}
