using HtmlAgilityPack;
using PuppeteerSharp;
using Common.Data;

namespace Crawler;

public class SmartmatchCrawler
{
    // Props
    public Settings Settings { get; set; } = new Settings { Name = "SmartMatch" };
    public ComponentStatus Status { get; set; }

    // Constructor injected fields
    private readonly ILogger<SmartmatchCrawler> logger;
    private readonly DatabaseContext context;

    // Private fields
    private readonly List<UspsFile> tempFiles = new();

    public SmartmatchCrawler(ILogger<SmartmatchCrawler> logger, IConfiguration config, DatabaseContext context)
    {
        this.logger = logger;
        this.context = context;

        Settings = Settings.Validate(Settings, config);
    }

    public async Task ExecuteAuto(CancellationToken stoppingToken)
    {
        // Check if crawler is enabled
        if (!Settings.CrawlerEnabled)
        {
            logger.LogInformation("Crawler disabled");
            Status = ComponentStatus.Disabled;
            return;
        }
        // Check if autocrawling is enabled
        if (!Settings.AutoCrawlEnabled)
        {
            logger.LogDebug("AutoCrawl disabled");
            return;
        }

        try
        {
            // Wait for autocrawl time, execute
            while (!stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Starting Crawler - Auto mode");
                TimeSpan waitTime = Settings.CalculateWaitTime(logger, Settings);
                await Task.Delay(TimeSpan.FromHours(waitTime.TotalHours), stoppingToken);

                await Execute(stoppingToken);
            }
        }
        catch (TaskCanceledException e)
        {
            logger.LogDebug("{Message}", e.Message);
        }
        catch (Exception e)
        {
            logger.LogError("{Message}", e.Message);
        }
    }

    public async Task Execute(CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation("Starting Crawler");
            Status = ComponentStatus.InProgress;

            await PullFiles(stoppingToken);
            CheckFiles(stoppingToken);
            await DownloadFiles(stoppingToken);
            CheckBuildReady(stoppingToken);

            Status = ComponentStatus.Ready;
        }
        catch (TaskCanceledException e)
        {
            // Current task was canceled (likely by switching between auto/manual mode), reset to ready status
            Status = ComponentStatus.Ready;
            logger.LogDebug("{Message}", e.Message);
        }
        catch (Exception e)
        {
            Status = ComponentStatus.Error;
            logger.LogError("{Message}", e.Message);
        }
    }

    private async Task PullFiles(CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        // Download local chromium binary to launch browser
        BrowserFetcher fetcher = new();
        await fetcher.DownloadAsync(BrowserFetcher.DefaultChromiumRevision);

        // Set launchoptions, create browser instance
        LaunchOptions options = new() { Headless = true };

        // Create a browser instance, page instance
        using Browser browser = await Puppeteer.LaunchAsync(options);
        using (stoppingToken.Register(async () => await browser.CloseAsync()))
        using (Page page = await browser.NewPageAsync())
        {
            // Navigate to download portal page
            await page.GoToAsync("https://epf.usps.gov/");

            await page.WaitForSelectorAsync("#email");
            await page.FocusAsync("#email");
            await page.Keyboard.TypeAsync(Settings.UserName);
            await page.FocusAsync("#password");
            await page.Keyboard.TypeAsync(Settings.Password);

            await page.ClickAsync("#login");

            await page.WaitForSelectorAsync("#r1");
            await page.ClickAsync("#r1");
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            await page.WaitForSelectorAsync("#r2");
            await page.ClickAsync("#r2");
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            // Has a 30s timeout, should throw exception if tbody/filelist is not found
            await page.WaitForSelectorAsync("#tblFileList > tbody");

            // Arrrived at download portal page, pull page HTML
            HtmlDocument doc = new();
            doc.LoadHtml(page.GetContentAsync().Result);

            // Format downloadables into list
            foreach (var fileRow in doc.DocumentNode.SelectNodes("/html/body/div[2]/table/tbody/tr/td/div[3]/table/tbody/tr/td/div/table/tbody/tr"))
            {
                UspsFile file = new()
                {
                    FileName = fileRow.ChildNodes[5].InnerText.Trim(),
                    UploadDate = DateTime.Parse(fileRow.ChildNodes[3].InnerText.Trim()),
                    Size = fileRow.ChildNodes[6].InnerText.Trim(),
                    OnDisk = true,

                    ProductKey = fileRow.Attributes[0].Value.Trim().Substring(19, 5),
                    FileId = fileRow.Attributes[1].Value.Trim().Substring(3, 7),

                    DataMonth = DateTime.Parse(fileRow.ChildNodes[3].InnerText.Trim()).Month,
                    DataYear = DateTime.Parse(fileRow.ChildNodes[3].InnerText.Trim()).Year
                };

                if (file.DataMonth < 10)
                {
                    file.DataYearMonth = file.DataYear.ToString() + "0" + file.DataMonth.ToString();
                }
                else
                {
                    file.DataYearMonth = file.DataYear.ToString() + file.DataMonth.ToString();
                }

                if (fileRow.ChildNodes[1].InnerText.Trim() == "Downloaded")
                {
                    file.Downloaded = true;
                }

                // New logic needed because epf behavior change, cannot G00424KL files because of access change + Cycle O added
                string productDescription = fileRow.ChildNodes[2].InnerText.Trim();
                if (productDescription.Contains("Cycle O"))
                {
                    file.Cycle = "Cycle-O";
                }
                else
                {
                    file.Cycle = "Cycle-N";
                }

                if (file.FileName.Contains(".zip", StringComparison.OrdinalIgnoreCase) || file.FileName.Contains(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                tempFiles.Add(file);
            }
        }
    }

    private void CheckFiles(CancellationToken stoppingToken)
    {
        // Cancellation requested or PullFile failed
        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        foreach (var file in tempFiles)
        {
            // Check if file is unique against the db
            bool fileInDb = context.UspsFiles.Any(x => file.FileId == x.FileId);

            if (!fileInDb)
            {
                // Check if file exists on the disk 
                if (!File.Exists(Path.Combine(Settings.AddressDataPath, file.DataYearMonth, file.Cycle, file.FileName)))
                {
                    file.OnDisk = false;
                }
                // regardless of check file is unique, add to db
                context.UspsFiles.Add(file);
                logger.LogDebug("Discovered and not on disk: {FileName} {DataMonth}/{DataYear} {Cycle}", file.FileName, file.DataMonth, file.DataYear, file.Cycle);

                bool bundleExists = context.UspsBundles.Any(x => (file.DataMonth == x.DataMonth) && (file.DataYear == x.DataYear) && (file.Cycle == x.Cycle));

                if (!bundleExists)
                {
                    UspsBundle newBundle = new()
                    {
                        DataMonth = file.DataMonth,
                        DataYear = file.DataYear,
                        DataYearMonth = file.DataYearMonth,
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

        tempFiles.Clear();
    }

    private async Task DownloadFiles(CancellationToken stoppingToken)
    {
        List<UspsFile> offDisk = context.UspsFiles.Where(x => !x.OnDisk).ToList();

        // if all files are downloaded, no need to kick open new browser
        if (offDisk.Count == 0 || stoppingToken.IsCancellationRequested)
        {
            return;
        }

        logger.LogInformation("New files found for download: {Count}", offDisk.Count);

        foreach (UspsFile file in offDisk)
        {
            // Ensure there is a folder to land in (this will punch through recursively btw, Downloads gets created as well if does not exist)
            Directory.CreateDirectory(Path.Combine(Settings.AddressDataPath, file.DataYearMonth, file.Cycle));
            Cleanup(Path.Combine(Settings.AddressDataPath, file.DataYearMonth, file.Cycle), stoppingToken);
        }

        // Download local chromium binary to launch browser
        BrowserFetcher fetcher = new();
        await fetcher.DownloadAsync(BrowserFetcher.DefaultChromiumRevision);

        // Set launchoptions, create browser instance
        LaunchOptions options = new() { Headless = false };

        // Create a browser instance, page instance
        using Browser browser = await Puppeteer.LaunchAsync(options);
        using (stoppingToken.Register(async () => await browser.CloseAsync()))
        using (Page page = await browser.NewPageAsync())
        {
            // Navigate to download portal page
            await page.GoToAsync("https://epf.usps.gov/");

            await page.WaitForSelectorAsync("#email");
            await page.FocusAsync("#email");
            await page.Keyboard.TypeAsync(Settings.UserName);
            await page.FocusAsync("#password");
            await page.Keyboard.TypeAsync(Settings.Password);

            await page.ClickAsync("#login");

            await page.WaitForSelectorAsync("#r1");
            await page.ClickAsync("#r1");
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            await page.WaitForSelectorAsync("#r2");
            await page.ClickAsync("#r2");
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            await page.WaitForSelectorAsync("#tblFileList > tbody");

            // Changed behavior, start download and wait for indivdual file to download. USPS website corrupts downloads if you do them all at once sometimes
            foreach (var file in offDisk)
            {
                string path = Path.Combine(Settings.AddressDataPath, file.DataYearMonth, file.Cycle);
                await page.Client.SendAsync("Page.setDownloadBehavior", new { behavior = "allow", downloadPath = path });

                // await page.EvaluateExpressionAsync(string.Format("getFileForDownload({0}, {1}, document.querySelector('#rw_{1}'))", file.ProductKey, file.FileId));
                await page.EvaluateExpressionAsync(string.Format("document.querySelector('#td_{0}').childNodes[0].click()", file.FileId));
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

                logger.LogInformation("Currently downloading: {FileName} {DataMonth}/{DataYear} {Cycle}", file.FileName, file.DataMonth, file.DataYear, file.Cycle);
                await WaitForDownload(file, stoppingToken);
            }
        }
    }

    private void CheckBuildReady(CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        foreach (var bundle in context.UspsBundles.ToList())
        {
            // idk why but you need to do some linq query to populate bundle.buildfiles? 
            // Something to do with one -> many relationship between the tables, investigate
            List<UspsFile> files = context.UspsFiles.Where(x => (x.DataMonth == bundle.DataMonth) && (x.DataYear == bundle.DataYear) && (x.Cycle == bundle.Cycle)).ToList();

            if (bundle.BuildFiles.All(x => x.OnDisk) && bundle.BuildFiles.Count >= 6)
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

                logger.LogInformation("Bundle ready to build: {DataMonth} / {DataYear}", bundle.DataMonth, bundle.DataYear);
            }

            context.SaveChanges();
        }
    }

    // Helper methods
    private async Task WaitForDownload(UspsFile file, CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Download in progress was stopped due to cancellation");
            return;
        }

        string path = Path.Combine(Settings.AddressDataPath, file.DataYearMonth, file.Cycle);
        if (!File.Exists(Path.Combine(path, file.FileName + ".CRDOWNLOAD")))
        {
            logger.LogDebug("Finished downloading");
            file.OnDisk = true;
            file.DateDownloaded = DateTime.Now;
            context.UspsFiles.Update(file);
            context.SaveChanges();
            return;
        }

        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        await WaitForDownload(file, stoppingToken);
    }

    private static void Cleanup(string path, CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        // Cleanup from previous run
        DirectoryInfo op = new(path);

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
