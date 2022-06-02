using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using System.Linq;
using System.IO;
using Microsoft.Extensions.Configuration;
using HtmlAgilityPack;
using Common.Data;

namespace Crawler.App
{
    public class ParascriptCrawler
    {
        public Settings Settings { get; set; } = new Settings { Name = "Parascript" };

        private readonly ILogger<ParascriptCrawler> logger;
        private readonly IConfiguration config;
        private readonly ComponentTask tasks;
        private readonly SocketConnection connection;
        private readonly DatabaseContext context;

        private List<ParaFile> tempFiles = new List<ParaFile>();

        public ParascriptCrawler(ILogger<ParascriptCrawler> logger, IConfiguration config, ComponentTask tasks, SocketConnection connection, DatabaseContext context)
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
            connection.SendMessage(parascript: true);

            if (Settings.CrawlerEnabled == false)
            {
                logger.LogInformation("Crawler disabled");
                tasks.Parascript = ComponentStatus.Disabled;
                connection.SendMessage(parascript: true);
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
                tasks.Parascript = ComponentStatus.InProgress;
                connection.SendMessage(parascript: true);

                await PullFiles(stoppingToken);
                CheckFiles(stoppingToken);
                await DownloadFiles(stoppingToken);
                CheckBuildReady(stoppingToken);

                tasks.Parascript = ComponentStatus.Ready;
                // connection.SendMessage(parascript: true);
            }
            catch (TaskCanceledException e)
            {
                tasks.Parascript = ComponentStatus.Ready;
                connection.SendMessage(parascript: true);
                logger.LogDebug(e.Message);
            }
            catch (System.Exception e)
            {
                tasks.Parascript = ComponentStatus.Error;
                connection.SendMessage(parascript: true);
                logger.LogError(e.Message);
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

            // Create a browser instance, page instance
            using (Browser browser = await Puppeteer.LaunchAsync(options))
            {
                using (stoppingToken.Register(async () => await browser.CloseAsync()))
                {
                    using (Page page = await browser.NewPageAsync())
                    {
                        await page.Client.SendAsync(@"Page.setDownloadBehavior", new { behavior = @"allow", downloadPath = Path.Combine(Settings.AddressDataPath, "Temp") });

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
                        adsFile.DataMonth = int.Parse(foundDataYearMonth.Substring(0, 2));
                        adsFile.DataYear = int.Parse("20" + foundDataYearMonth.Substring(2, 2));
                        adsFile.DataYearMonth = "20" + foundDataYearMonth.Substring(2, 2) + foundDataYearMonth.Substring(0, 2);
                        adsFile.OnDisk = true;

                        tempFiles.Add(adsFile);

                        ParaFile dpvFile = new ParaFile();
                        dpvFile.FileName = "DPVandLACS";
                        dpvFile.DataMonth = int.Parse(foundDataYearMonth.Substring(0, 2));
                        dpvFile.DataYear = int.Parse("20" + foundDataYearMonth.Substring(2, 2));
                        dpvFile.DataYearMonth = "20" + foundDataYearMonth.Substring(2, 2) + foundDataYearMonth.Substring(0, 2);
                        dpvFile.OnDisk = true;

                        tempFiles.Add(dpvFile);
                    }
                }
            }
        }

        private void CheckFiles(CancellationToken stoppingToken)
        {
            // Cancellation requested or PullFile failed
            if (stoppingToken.IsCancellationRequested == true)
            {
                return;
            }

            foreach (var file in tempFiles)
            {
                // Check if file is unique against the db
                bool fileInDb = context.ParaFiles.Any(x => (file.FileName == x.FileName) && (file.DataMonth == x.DataMonth) && (file.DataYear == x.DataYear));

                if (!fileInDb)
                {
                    // Check if the folder exists on the disk
                    if (!Directory.Exists(Path.Combine(Settings.AddressDataPath, file.DataYearMonth, file.FileName)))
                    {
                        file.OnDisk = false;
                    }

                    // regardless of check file is unique, add to db
                    context.ParaFiles.Add(file);
                    logger.LogInformation("Discovered and not on disk: " + file.FileName + " " + file.DataMonth + "/" + file.DataYear);

                    bool bundleExists = context.ParaBundles.Any(x => (file.DataMonth == x.DataMonth) && (file.DataYear == x.DataYear));

                    if (!bundleExists)
                    {
                        ParaBundle newBundle = new ParaBundle()
                        {
                            DataMonth = file.DataMonth,
                            DataYear = file.DataYear,
                            DataYearMonth = file.DataYearMonth,
                            IsReadyForBuild = false
                        };

                        newBundle.BuildFiles.Add(file);
                        context.ParaBundles.Add(newBundle);
                    }
                    else
                    {
                        ParaBundle existingBundle = context.ParaBundles.Where(x => (file.DataMonth == x.DataMonth) && (file.DataYear == x.DataYear)).FirstOrDefault();

                        existingBundle.BuildFiles.Add(file);
                    }

                    context.SaveChanges();
                }
            }

            tempFiles.Clear();
        }

        private async Task DownloadFiles(CancellationToken stoppingToken)
        {
            List<ParaFile> offDisk = context.ParaFiles.Where(x => x.OnDisk == false).ToList();

            if (offDisk.Count == 0 || stoppingToken.IsCancellationRequested == true)
            {
                return;
            }

            logger.LogInformation("New files found for download: " + offDisk.Count);

            foreach (ParaFile file in offDisk)
            {
                Directory.CreateDirectory(Path.Combine(Settings.AddressDataPath, file.DataYearMonth));
                Cleanup(Path.Combine(Settings.AddressDataPath, file.DataYearMonth), stoppingToken);
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
                        await page.Client.SendAsync(@"Page.setDownloadBehavior", new { behavior = @"allow", downloadPath = Path.Combine(Settings.AddressDataPath, offDisk[0].DataYearMonth) });

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
                            await WaitForDownload(file, stoppingToken);
                        }
                    }
                }
            }
        }

        private void CheckBuildReady(CancellationToken stoppingToken)
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
                List<ParaFile> files = context.ParaFiles.Where(x => (x.DataMonth == bundle.DataMonth) && (x.DataYear == bundle.DataYear)).ToList();

                if (!bundle.BuildFiles.Any(x => x.OnDisk == false) && bundle.BuildFiles.Count >= 2)
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

        private async Task WaitForDownload(ParaFile file, CancellationToken stoppingToken)
        {
            if (stoppingToken.IsCancellationRequested == true)
            {
                logger.LogInformation("Download in progress was stopped due to cancellation");
                return;
            }

            string path = Path.Combine(Settings.AddressDataPath, file.DataYearMonth);
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
            await WaitForDownload(file, stoppingToken);
        }

        private void Cleanup(string path, CancellationToken stoppingToken)
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
