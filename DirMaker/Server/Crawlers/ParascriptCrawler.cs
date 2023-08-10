using Microsoft.EntityFrameworkCore;
using PuppeteerSharp;
using Server.Common;

namespace Server.Crawlers;

public class ParascriptCrawler : DirModule
{
    private readonly ILogger<ParascriptCrawler> logger;
    private readonly DatabaseContext context;

    private readonly List<ParaFile> tempFiles = new();

    public ParascriptCrawler(ILogger<ParascriptCrawler> logger, IConfiguration config, DatabaseContext context)
    {
        this.logger = logger;
        this.context = context;

        Settings.Directory = "Parascript";
        Settings.Validate(config);
    }

    public async Task Start(CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation("Starting Crawler");
            Status = ModuleStatus.InProgress;

            await PullFiles(stoppingToken);
            CheckFiles(stoppingToken);
            await DownloadFiles(stoppingToken);
            CheckBuildReady(stoppingToken);

            logger.LogInformation("Finished Crawling");
            Status = ModuleStatus.Ready;
        }
        catch (TaskCanceledException e)
        {
            Status = ModuleStatus.Ready;
            logger.LogDebug("{Message}", e.Message);
        }
        catch (Exception e)
        {
            Status = ModuleStatus.Error;
            logger.LogError("{Message}", e.Message);
        }
    }

    private async Task PullFiles(CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        // Clear tempfiles in case of leftovers from last pass
        tempFiles.Clear();

        // Download local chromium binary to launch browser
        BrowserFetcher fetcher = new();
        await fetcher.DownloadAsync(BrowserFetcher.DefaultChromiumRevision);

        // Set launchoptions, create browser instance
        LaunchOptions options = new() { Headless = true };

        // Create a browser instance, page instance
        using Browser browser = (Browser)await Puppeteer.LaunchAsync(options);
        using (stoppingToken.Register(async () => await browser.CloseAsync()))
        using (Page page = (Page)await browser.NewPageAsync())
        {
            await page.Client.SendAsync("Page.setDownloadBehavior", new { behavior = "allow", downloadPath = Path.Combine(Settings.AddressDataPath, "Temp") });

            // Navigate to download portal page
            await page.GoToAsync("https://parascript.sharefile.com/share/view/s80765117d4441b88");

            // Arrived a download portal page
            await page.WaitForSelectorAsync("#applicationHost > div.shell_19a1hjv > div > div.downloadPage_1gtget5 > div.container_cdvlrd > div > div.gridHeader_ubbr06 > label > label > span > span");
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

            // Click the ads tag to see inside file
            await page.ClickAsync("#applicationHost > div.shell_19a1hjv > div > div.downloadPage_1gtget5 > div.container_cdvlrd > div > div.grid_1joc06t > div:nth-child(1) > div.metadataSlot_1kvnsfa > div > span.name_eol401");
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

            // Arrived at 2nd page
            await page.WaitForSelectorAsync("#applicationHost > div.shell_19a1hjv > div > div.downloadPage_1gtget5 > div.container_cdvlrd > div > div.grid_1joc06t > div:nth-child(1) > div.metadataSlot_1kvnsfa > div > span.name_eol401");
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

            HtmlAgilityPack.HtmlDocument doc = new();
            doc.LoadHtml(page.GetContentAsync().Result);

            HtmlAgilityPack.HtmlNode node = doc.DocumentNode.SelectSingleNode("/html/body/div/div[1]/div/div[1]/div[5]/div/div[2]/div[1]/div[2]/div/span[1]");

            string foundDataYearMonth = node.InnerText.Substring(11, 4);

            ParaFile adsFile = new()
            {
                FileName = "ads6",
                DataMonth = int.Parse(foundDataYearMonth[..2]),
                DataYear = int.Parse(string.Concat("20", foundDataYearMonth.AsSpan(2, 2))),
                DataYearMonth = string.Concat("20", foundDataYearMonth.AsSpan(2, 2), foundDataYearMonth.AsSpan(0, 2)),
                OnDisk = true
            };

            tempFiles.Add(adsFile);

            ParaFile dpvFile = new()
            {
                FileName = "DPVandLACS",
                DataMonth = int.Parse(foundDataYearMonth[..2]),
                DataYear = int.Parse(string.Concat("20", foundDataYearMonth.AsSpan(2, 2))),
                DataYearMonth = string.Concat("20", foundDataYearMonth.AsSpan(2, 2), foundDataYearMonth.AsSpan(0, 2)),
                OnDisk = true
            };

            tempFiles.Add(dpvFile);
        }
    }

    private void CheckFiles(CancellationToken stoppingToken)
    {
        // Cancellation requested or PullFile failed
        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        foreach (ParaFile file in tempFiles)
        {
            // Check if file is unique against the db
            bool fileInDb = context.ParaFiles.Any(x => (file.FileName == x.FileName) && (file.DataMonth == x.DataMonth) && (file.DataYear == x.DataYear));

            if (fileInDb)
            {
                continue;
            }

            // Regardless of file check is unique, add to db
            context.ParaFiles.Add(file);

            // Check if the folder exists on the disk
            if (!Directory.Exists(Path.Combine(Settings.AddressDataPath, file.DataYearMonth, file.FileName)))
            {
                file.OnDisk = false;
            }
            logger.LogInformation("Discovered and not on disk: {FileName} {DataMonth}/{DataYear}", file.FileName, file.DataMonth, file.DataYear);

            bool bundleExists = context.ParaBundles.Any(x => (file.DataMonth == x.DataMonth) && (file.DataYear == x.DataYear));

            if (!bundleExists)
            {
                ParaBundle newBundle = new()
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

    private async Task DownloadFiles(CancellationToken stoppingToken)
    {
        List<ParaFile> offDisk = context.ParaFiles.Where(x => !x.OnDisk).ToList();

        if (offDisk.Count == 0 || stoppingToken.IsCancellationRequested)
        {
            return;
        }

        logger.LogInformation("New files found for download: {Count}", offDisk.Count);

        foreach (ParaFile file in offDisk)
        {
            Directory.CreateDirectory(Path.Combine(Settings.AddressDataPath, file.DataYearMonth));
            Cleanup(Path.Combine(Settings.AddressDataPath, file.DataYearMonth), stoppingToken);
        }

        // Download local chromium binary to launch browser
        BrowserFetcher fetcher = new();
        await fetcher.DownloadAsync(BrowserFetcher.DefaultChromiumRevision);

        // Set launchoptions, create browser instance
        LaunchOptions options = new() { Headless = true };

        // Create a browser instance, page instance
        using Browser browser = (Browser)await Puppeteer.LaunchAsync(options);
        using (stoppingToken.Register(async () => await browser.CloseAsync()))
        using (Page page = (Page)await browser.NewPageAsync())
        {
            await page.Client.SendAsync("Page.setDownloadBehavior", new { behavior = "allow", downloadPath = Path.Combine(Settings.AddressDataPath, offDisk[0].DataYearMonth) });

            // Navigate to download portal page
            await page.GoToAsync("https://parascript.sharefile.com/share/view/s80765117d4441b88");

            // Arrived a download portal page
            await page.WaitForSelectorAsync("#applicationHost > div.shell_19a1hjv > div > div.downloadPage_1gtget5 > div.container_cdvlrd > div > div.gridHeader_ubbr06 > label > label > span > span");
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

            // Click the select all checkbox
            await page.ClickAsync("#applicationHost > div.shell_19a1hjv > div > div.downloadPage_1gtget5 > div.container_cdvlrd > div > div.gridHeader_ubbr06 > label > label > span > span");
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

            // Click the download button
            await page.ClickAsync("#applicationHost > div.shell_19a1hjv > div > div.downloadPage_1gtget5 > div:nth-child(6) > div.footer_1pnvz17 > div > div > div.downloadButton_4mfu3n > button > div");
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

            logger.LogInformation("Currently downloading: Parascript files");
            // Cancellation closes page and browser using statement, clears crdownload so no cleanup there

            foreach (ParaFile file in offDisk)
            {
                await WaitForDownload(logger, context, file, "Parascript", stoppingToken);
            }
        }
    }

    private void CheckBuildReady(CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        foreach (ParaBundle bundle in context.ParaBundles.ToList())
        {
            // idk why but you need to do some linq query to populate bundle.buildfiles? 
            // Something to do with one -> many relationship between the tables, investigate
            List<ParaFile> files = context.ParaFiles.Where(x => (x.DataMonth == bundle.DataMonth) && (x.DataYear == bundle.DataYear)).ToList();

            if (bundle.BuildFiles.All(x => x.OnDisk) && bundle.BuildFiles.Count >= 2)
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

                logger.LogInformation("Bundle ready to build: {DataMonth}/{DataYear}", bundle.DataMonth, bundle.DataYear);
            }

            context.SaveChanges();
        }
    }
}
