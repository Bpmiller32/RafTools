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

namespace Crawler.App
{
    public class ParascriptCrawler : BackgroundService
    {
        private readonly ILogger<ParascriptCrawler> logger;
        private readonly DatabaseContext context;
        private List<ParaFile> TempFiles = new List<ParaFile>();

        public ParascriptCrawler(ILogger<ParascriptCrawler> logger, IServiceScopeFactory factory)
        {
            this.logger = logger;
            this.context = factory.CreateScope().ServiceProvider.GetRequiredService<DatabaseContext>();
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Hello from ParaCrawler!");
            context.Database.EnsureCreated();

            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Successfully stopped ParaCrawler");

            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            DateTime execTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 27, 7, 59, 59);
            DateTime endOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month), 23, 23, 59);
            TimeSpan waitTime = execTime - DateTime.Now;

            while (!stoppingToken.IsCancellationRequested)
            {
                Cleanup(Directory.GetCurrentDirectory() + @"\Downloads\Parascript\Temp\", stoppingToken);
                await DownloadFiles(stoppingToken);
                Inspect(stoppingToken);
                CheckFiles(stoppingToken);
                SortFiles(stoppingToken);
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

        private void Cleanup(string path, CancellationToken stoppingToken)
        {
            if (stoppingToken.IsCancellationRequested == true)
            {
                return;
            }

            // Ensure there is a folder to land in (this will punch through recursively btw, Downloads gets created as well if does not exist)
            Directory.CreateDirectory(Directory.GetCurrentDirectory() + @"\Downloads\Parascript\Temp");

            // Cleanup from previous run
            DirectoryInfo op = new DirectoryInfo(Directory.GetCurrentDirectory() + @"\Downloads\Parascript\Temp\");

            foreach (var file in op.GetFiles())
            {
                file.Delete();
            }
            foreach (var dir in op.GetDirectories())
            {
                dir.Delete(true);
            }

        }

        private async Task DownloadFiles(CancellationToken stoppingToken)
        {
            if (stoppingToken.IsCancellationRequested == true)
            {
                return;
            }

            // Download local chromium binary to launch browser
            BrowserFetcher fetcher = new BrowserFetcher();
            await fetcher.DownloadAsync(BrowserFetcher.DefaultChromiumRevision);

            // Set launchoptions, create browser instance
            LaunchOptions options = new LaunchOptions() { Headless = false };

            // Create a browser instance, page instance
            using (Browser browser = await Puppeteer.LaunchAsync(options))
            {
                using (Page page = await browser.NewPageAsync())
                {
                    try
                    {
                        await page.Client.SendAsync(@"Page.setDownloadBehavior", new { behavior = @"allow", downloadPath = Directory.GetCurrentDirectory() + @"\Downloads\Parascript\Temp" });

                        // Navigate to download portal page
                        await page.GoToAsync(@"https://parascript.sharefile.com/share/view/s80765117d4441b88");

                        await page.WaitForSelectorAsync(@"#applicationHost > div.shell_19a1hjv > div > div.downloadPage_1gtget5 > div.container_cdvlrd > div > div.gridHeader_ubbr06 > label > label > span > span");
                        await Task.Delay(3000);
                        await page.ClickAsync(@"#applicationHost > div.shell_19a1hjv > div > div.downloadPage_1gtget5 > div.container_cdvlrd > div > div.gridHeader_ubbr06 > label > label > span > span");
                        await Task.Delay(3000);

                        await page.ClickAsync(@"#applicationHost > div.shell_19a1hjv > div > div.downloadPage_1gtget5 > div:nth-child(6) > div.footer_1pnvz17 > div > div > div.downloadButton_4mfu3n > button > div");
                        await Task.Delay(3000);

                        await WaitForDownload(stoppingToken);
                    }
                    catch (System.Exception e)
                    {
                        logger.LogError(e.Message);
                    }
                }
            }
        }

        private void Inspect(CancellationToken stoppingToken)
        {
            // Check if you were able to download anything from the website
            if (!File.Exists(Directory.GetCurrentDirectory() + @"\Downloads\Parascript\Temp\Files.zip"))
            {
                return;
            }

            // Extract zip file
            ZipFile.ExtractToDirectory(Directory.GetCurrentDirectory() + @"\Downloads\Parascript\Temp\Files.zip", Directory.GetCurrentDirectory() + @"\Downloads\Parascript\Temp");
            var dirs = Directory.GetDirectories(Directory.GetCurrentDirectory() + @"\Downloads\Parascript\Temp");

            foreach (var dir in dirs)
            {
                ParaFile file = new ParaFile();
                file.FileName = dir.Split(Path.DirectorySeparatorChar).Last();

                // Find the month and date in the downloaded file
                using (StreamReader sr = new StreamReader(Directory.GetCurrentDirectory() + @"\Downloads\Parascript\Temp\ads6\readme.txt"))
                {
                    string line;
                    Regex regex = new Regex(@"(Issue Date:)(\s+)(\d\d\/\d\d\/\d\d\d\d)");

                    while ((line = sr.ReadLine()) != null)
                    {
                        Match match = regex.Match(line);

                        if (match.Success == true)
                        {
                            file.DataMonth = match.Groups[3].Value.Substring(0, 2);
                            file.DataYear = match.Groups[3].Value.Substring(8, 2);
                        }
                    }
                }

                TempFiles.Add(file);
            }
        }

        private void CheckFiles(CancellationToken stoppingToken)
        {
            foreach (var file in TempFiles)
            {
                // Check if file is unique against the db
                bool fileInDb = context.ParaFiles.Any(x => (file.FileName == x.FileName) && (file.DataMonth == x.DataMonth) && (file.DataYear == x.DataYear));

                if (!fileInDb)
                {
                    // Check if the folder exists on the disk
                    if (!Directory.Exists(Directory.GetCurrentDirectory() + @"\Downloads\Parascript\" + file.DataYear + @"\" + file.DataMonth + @"\" + file.FileName))
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

            TempFiles.Clear();
        }

        private void SortFiles(CancellationToken stoppingToken)
        {
            // Find files to keep
            List<ParaFile> offDisk = context.ParaFiles.Where(x => x.OnDisk == false).ToList();

            logger.LogInformation("New files found for storing: " + offDisk.Count);

            // if all files are downloaded, no need to do anything
            if (offDisk.Count == 0)
            {
                return;
            }

            foreach (var file in offDisk)
            {
                // Ensure there is a folder to land in (this will punch through recursively btw, Downloads gets created as well if does not exist)
                Directory.CreateDirectory(Directory.GetCurrentDirectory() + @"\Downloads\Parascript\" + file.DataYear + @"\" + file.DataMonth);
                // Cleanup if a file happens to be left over?
                Cleanup(Directory.GetCurrentDirectory() + @"\Downloads\Parascript\" + file.DataYear + @"\" + file.DataMonth, stoppingToken);

                Directory.Move(Directory.GetCurrentDirectory() + @"\Downloads\Parascript\Temp\" + file.FileName, Directory.GetCurrentDirectory() + @"\Downloads\Parascript\" + file.DataYear + @"\" + file.DataMonth + @"\" + file.FileName);
                logger.LogInformation(@"File stored: " + file.FileName + " " + file.DataMonth + "/" + file.DataYear);

                file.OnDisk = true;
                file.DateDownloaded = DateTime.Now;
                context.ParaFiles.Update(file);
                context.SaveChanges();
            }
        }

        private void CheckBuildReady(CancellationToken stoppingToken)
        {
            List<ParaBundle> bundles = context.ParaBundles.ToList();

            foreach (var bundle in bundles)
            {
                // idk why but you need to do some linq query to populate bundle.buildfiles? 
                // Something to do with one -> many relationship between the tables, investigate
                List<ParaFile> files = context.ParaFiles.Where(x => (x.DataMonth == bundle.DataMonth) && (x.DataYear == bundle.DataYear)).ToList();

                if (!bundle.BuildFiles.Any(x => x.OnDisk == false) && bundle.BuildFiles.Count >= 2)
                {
                    bundle.IsReadyForBuild = true;
                    logger.LogInformation("Bundle ready to build: " + bundle.DataMonth + "/" + bundle.DataYear);
                }

                context.SaveChanges();
            }
        }

        private async Task WaitForDownload(CancellationToken stoppingToken)
        {
            if (stoppingToken.IsCancellationRequested == true)
            {
                return;
            }

            string path = Directory.GetCurrentDirectory() + @"\Downloads\Parascript\Temp";
            string[] files = Directory.GetFiles(path, @"*.CRDOWNLOAD");

            if (files.Length < 1)
            {
                logger.LogInformation("Finished downloading");
                return;
            }
            else
            {
                await Task.Delay(180000);
            }

            await WaitForDownload(stoppingToken);
        }
    }
}
