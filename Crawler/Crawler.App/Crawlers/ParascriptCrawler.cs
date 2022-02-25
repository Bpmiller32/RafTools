using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Crawler.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.IO.Compression;
using Microsoft.Extensions.Configuration;
using HtmlAgilityPack;

namespace Crawler.App
{
    public class ParascriptCrawler : BackgroundService
    {
        private readonly ILogger logger;
        private readonly IConfiguration config;
        private readonly CrawlTask tasks;
        private readonly DatabaseContext context;

        private CancellationToken stoppingToken;
        private Settings settings = new Settings() { Name = "Parascript" };

        private List<ParaFile> TempFiles = new List<ParaFile>();
        private string dataYearMonth = "";

        public ParascriptCrawler(ILogger<ParascriptCrawler> logger, IConfiguration config, IServiceScopeFactory factory, CrawlTask tasks)
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
                tasks.Parascript = CrawlStatus.Disabled;
                logger.LogInformation("Crawler disabled");
                return;
            }

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    logger.LogInformation("Starting Crawler");
                    tasks.Parascript = CrawlStatus.Enabled;

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
                tasks.Parascript = CrawlStatus.Error;
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
                        await page.Client.SendAsync(@"Page.setDownloadBehavior", new { behavior = @"allow", downloadPath = Path.Combine(settings.AddressDataPath, "Temp") });

                        // Navigate to download portal page
                        await page.GoToAsync(@"https://parascript.sharefile.com/share/view/s80765117d4441b88");

                        // Arrived a download portal page
                        await page.WaitForSelectorAsync(@"#applicationHost > div.shell_19a1hjv > div > div.downloadPage_1gtget5 > div.container_cdvlrd > div > div.gridHeader_ubbr06 > label > label > span > span");
                        await Task.Delay(TimeSpan.FromSeconds(3));

                        // Click the ads tag to see inside file
                        await page.ClickAsync(@"#applicationHost > div.shell_19a1hjv > div > div.downloadPage_1gtget5 > div.container_cdvlrd > div > div.grid_1joc06t > div:nth-child(1) > div.metadataSlot_1kvnsfa > div > span.name_eol401");
                        await Task.Delay(TimeSpan.FromSeconds(3));

                        // Arrived at 2nd page
                        await page.WaitForSelectorAsync(@"#applicationHost > div.shell_19a1hjv > div > div.downloadPage_1gtget5 > div.container_cdvlrd > div > div.grid_1joc06t > div:nth-child(1) > div.metadataSlot_1kvnsfa > div > span.name_eol401");
                        await Task.Delay(TimeSpan.FromSeconds(3));

                        HtmlDocument doc = new HtmlDocument();
                        doc.LoadHtml(page.GetContentAsync().Result);

                        HtmlNode node = doc.DocumentNode.SelectSingleNode(@"/html/body/div/div[1]/div/div[1]/div[5]/div/div[2]/div[1]/div[2]/div/span[1]");

                        string foundDataYearMonth = node.InnerText.Substring(11, 4);

                        ParaFile adsFile = new ParaFile();
                        adsFile.FileName = "ads6";
                        adsFile.DataMonth = foundDataYearMonth.Substring(0, 2);
                        adsFile.DataYear = foundDataYearMonth.Substring(2, 2);
                        adsFile.OnDisk = true;

                        TempFiles.Add(adsFile);

                        ParaFile dpvFile = new ParaFile();
                        dpvFile.FileName = "DPVandLACS";
                        dpvFile.DataMonth = foundDataYearMonth.Substring(0, 2);
                        dpvFile.DataYear = foundDataYearMonth.Substring(2, 2);
                        dpvFile.OnDisk = true;

                        TempFiles.Add(dpvFile);
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
                dataYearMonth = "20" + file.DataYear + file.DataMonth;
                
                // Check if file is unique against the db
                bool fileInDb = context.ParaFiles.Any(x => (file.FileName == x.FileName) && (file.DataMonth == x.DataMonth) && (file.DataYear == x.DataYear));

                if (!fileInDb)
                {
                    // Check if the folder exists on the disk
                    if (!Directory.Exists(Path.Combine(settings.AddressDataPath, dataYearMonth, file.FileName)))
                    {
                        file.OnDisk = false;
                    }

                    // regardless of check file is unique, add to db
                    context.ParaFiles.Add(file);
                    logger.LogInformation("Discovered and not on disk: " + file.FileName + " " + file.DataMonth + "/" + file.DataYear);

                    bool bundleExists = context.ParaBundles.Any(x => (int.Parse(file.DataMonth) == x.DataMonth) && (int.Parse(file.DataYear) == x.DataYear));

                    if (!bundleExists)
                    {
                        ParaBundle newBundle = new ParaBundle()
                        {
                            DataMonth = int.Parse(file.DataMonth),
                            DataYear = int.Parse(file.DataYear),
                            IsReadyForBuild = false
                        };

                        newBundle.BuildFiles.Add(file);
                        context.ParaBundles.Add(newBundle);
                    }
                    else
                    {
                        ParaBundle existingBundle = context.ParaBundles.Where(x => (int.Parse(file.DataMonth) == x.DataMonth) && (int.Parse(file.DataYear) == x.DataYear)).FirstOrDefault();

                        existingBundle.BuildFiles.Add(file);
                    }

                    context.SaveChanges();
                }
            }

            TempFiles.Clear();
        }

        private async Task DownloadFiles()
        {
            List<ParaFile> offDisk = context.ParaFiles.Where(x => x.OnDisk == false).ToList();

            if (offDisk.Count == 0 || stoppingToken.IsCancellationRequested == true)
            {
                return;
            }

            logger.LogInformation("New files found for download: " + offDisk.Count);

            foreach (ParaFile file in offDisk)
            {
                Directory.CreateDirectory(Path.Combine(settings.AddressDataPath, dataYearMonth));
                Cleanup(Path.Combine(settings.AddressDataPath, dataYearMonth));
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
                        await page.Client.SendAsync(@"Page.setDownloadBehavior", new { behavior = @"allow", downloadPath = Path.Combine(settings.AddressDataPath, dataYearMonth) });

                        // Navigate to download portal page
                        await page.GoToAsync(@"https://parascript.sharefile.com/share/view/s80765117d4441b88");

                        // Arrived a download portal page
                        await page.WaitForSelectorAsync(@"#applicationHost > div.shell_19a1hjv > div > div.downloadPage_1gtget5 > div.container_cdvlrd > div > div.gridHeader_ubbr06 > label > label > span > span");
                        await Task.Delay(TimeSpan.FromSeconds(3));

                        // Click the select all checkbox
                        await page.ClickAsync(@"#applicationHost > div.shell_19a1hjv > div > div.downloadPage_1gtget5 > div.container_cdvlrd > div > div.gridHeader_ubbr06 > label > label > span > span");
                        await Task.Delay(TimeSpan.FromSeconds(3));

                        // Click the download button
                        await page.ClickAsync(@"#applicationHost > div.shell_19a1hjv > div > div.downloadPage_1gtget5 > div:nth-child(6) > div.footer_1pnvz17 > div > div > div.downloadButton_4mfu3n > button > div");
                        await Task.Delay(TimeSpan.FromSeconds(3));

                        logger.LogInformation("Currently downloading: Parascript files");
                        // Cancellation closes page and browser using statement, clears crdownload so no cleanup there
                        
                        foreach (var file in offDisk)
                        {
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

            List<ParaBundle> bundles = context.ParaBundles.ToList();

            foreach (var bundle in bundles)
            {
                // idk why but you need to do some linq query to populate bundle.buildfiles? 
                // Something to do with one -> many relationship between the tables, investigate
                List<ParaFile> files = context.ParaFiles.Where(x => (int.Parse(x.DataMonth) == bundle.DataMonth) && (int.Parse(x.DataYear) == bundle.DataYear)).ToList();

                if (!bundle.BuildFiles.Any(x => x.OnDisk == false) && bundle.BuildFiles.Count >= 2)
                {
                    bundle.IsReadyForBuild = true;
                    logger.LogInformation("Bundle ready to build: " + bundle.DataMonth + "/" + bundle.DataYear);
                }

                context.SaveChanges();
            }
        }

        private async Task WaitForDownload(ParaFile file)
        {
            if (stoppingToken.IsCancellationRequested == true)
            {
                logger.LogInformation("Download in progress was stopped due to cancellation");
                return;
            }

            string path = Path.Combine(settings.AddressDataPath, dataYearMonth);
            string[] files = Directory.GetFiles(path, @"*.CRDOWNLOAD");

            if (files.Length < 1)
            {
                logger.LogDebug("Finished downloading");
                file.OnDisk = true;
                file.DateDownloaded = DateTime.Now;
                context.ParaFiles.Update(file);
                context.SaveChanges();
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(10));
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
    }
}
