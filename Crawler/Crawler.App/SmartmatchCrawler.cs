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

// ✅ Refactor downloading to download more than one file at once
// ✅ Revert WaitForDownload to look for a list instead of single file
// ✅ Make download paths consistent
// - Pass cancelationTokens into async functions for clean cancel/service stop
// - Integrate Discord bot with logger
// ✅ Implement CheckBuildReady
namespace Crawler.App
{
    public class SmartmatchCrawler : BackgroundService
    {
        private readonly ILogger<SmartmatchCrawler> logger;
        private readonly DatabaseContext context;
        private List<UspsFile> TempFiles = new List<UspsFile>();

        public SmartmatchCrawler(ILogger<SmartmatchCrawler> logger, IServiceScopeFactory factory)
        {
            this.logger = logger;
            this.context = factory.CreateScope().ServiceProvider.GetRequiredService<DatabaseContext>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Hello from SmCrawler!");

            context.Database.EnsureCreated();

            while (!stoppingToken.IsCancellationRequested)
            {
                await PullFiles();
                CheckFiles();
                await DownloadFiles();
                CheckBuildReady();

                logger.LogInformation("Pass completed, starting sleep for 5 min");
                await Task.Delay(300000, stoppingToken);
            }
        }



        private async Task PullFiles()
        {
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
                        await Task.Delay(5000);
                        await page.WaitForSelectorAsync(@"#r2");
                        await page.ClickAsync(@"#r2");
                        await Task.Delay(5000);

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
                        logger.LogError(e.Message);
                    }
                }
            }
        }

        private void CheckFiles()
        {
            foreach (var file in TempFiles)
            {
                // Check if file is unique against the db
                bool fileInDb = context.UspsFiles.Any(x => file.FileId == x.FileId);

                if (!fileInDb)
                {
                    // Check if file exists on the disk 
                    if (!File.Exists(Directory.GetCurrentDirectory() + @"\Downloads\" + file.DataYear + @"\" + file.DataMonth + @"\" + file.FileName))
                    {
                        file.OnDisk = false;
                    }
                    // regardless of check file is unique, add to db
                    context.UspsFiles.Add(file);
                    logger.LogInformation("Discovered and not on disk: " + file.FileName + " " + file.DataMonth + "/" + file.DataYear);

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

        private async Task DownloadFiles()
        {
            List<UspsFile> offDisk = context.UspsFiles.Where(x => x.OnDisk == false).ToList();

            logger.LogInformation("New files found for download: " + offDisk.Count);
            
            // if all files are downloaded, no need to kick open new browser
            if (offDisk.Count == 0)
            {
                return;
            }

            foreach (var file in offDisk)
            {
                // Ensure there is a folder to land in (this will punch through recursively btw, Downloads gets created as well if does not exist)
                Directory.CreateDirectory(Directory.GetCurrentDirectory() + @"\Downloads\" + file.DataYear + @"\" + file.DataMonth);
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
                        await Task.Delay(5000);
                        await page.WaitForSelectorAsync(@"#r2");
                        await page.ClickAsync(@"#r2");
                        await Task.Delay(5000);

                        await page.WaitForSelectorAsync(@"#tblFileList > tbody");


                        // Changed behavior, start download and wait for indivdual file to download. USPS website corrupts downloads if you do them all at once sometimes
                        foreach (var file in offDisk)
                        {
                            string path = Directory.GetCurrentDirectory() + @"\Downloads\" + file.DataYear + @"\" + file.DataMonth;
                            await page.Client.SendAsync(@"Page.setDownloadBehavior", new { behavior = @"allow", downloadPath = path });
                            
                            await page.EvaluateExpressionAsync(@"getFileForDownload(" + file.ProductKey + "," + file.FileId + ",rw_" + file.FileId + ");");
                            await Task.Delay(5000);
                        
                            logger.LogInformation("Currently downloading: " + file.FileName + " " + file.DataMonth + "/" + file.DataYear);
                            await WaitForDownload(file);
                        }
                    }
                    catch (System.Exception e)
                    {
                        logger.LogError(e.Message);
                    }
                }
            }
        }

        private void CheckBuildReady()
        {
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
    

        private async Task WaitForDownload(UspsFile file)
        {
            string path = Directory.GetCurrentDirectory() + @"\Downloads\" + file.DataYear + @"\" + file.DataMonth;
            if (!File.Exists(path + @"\" + file.FileName + @".CRDOWNLOAD"))
            {
                logger.LogInformation("Finished downloading");
                file.OnDisk = true;
                file.DateDownloaded = DateTime.Now;
                context.UspsFiles.Update(file);
                context.SaveChanges();
                return;
            }
            else
            {
                await Task.Delay(180000);                
            }

            await WaitForDownload(file);
        }
    }
}
