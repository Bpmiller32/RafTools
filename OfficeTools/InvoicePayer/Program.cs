using HtmlAgilityPack;
using PuppeteerSharp;
using Serilog;
using Serilog.Events;
using Serilog.Templates;

string applicationName = "InvoicePayer";
using var mutex = new Mutex(false, applicationName);

// Configure logger
Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .WriteTo.Console(new ExpressionTemplate("[{@t:MM-dd-yyyy HH:mm:ss}]  [{@l:u3}] {@m}\n{@x}"))
        // .WriteTo.File(new ExpressionTemplate("[{@t:MM-dd-yyyy HH:mm:ss}]  [{@l:u3}] {@m}\n{@x}"), String.Format(".\\Log\\{0}.txt", applicationName))
        .CreateLogger();

// Set exe directory to current directory, important when doing Windows services otherwise runs out of System32
Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

try
{
    // Single instance of application check
    bool isAnotherInstanceOpen = !mutex.WaitOne(TimeSpan.Zero);
    if (isAnotherInstanceOpen)
    {
        throw new Exception("Only one instance of the application allowed");
    }
}
catch (System.Exception e)
{
    Log.Error(e.Message);
}

Console.WriteLine("Hello, World!");

List<Receipt> receipts = new();

await RunVultr();
await RunConcur();

async Task RunVultr()
{
    // Download local chromium binary to launch browser
    BrowserFetcher fetcher = new();
    await fetcher.DownloadAsync(BrowserFetcher.DefaultChromiumRevision);

    // Set launchoptions, create browser instance
    LaunchOptions options = new() { Headless = false };

    // Create a browser instance, page instance
    using IBrowser browser = await Puppeteer.LaunchAsync(options);
    using IPage page = await browser.NewPageAsync();

    // Navigate to download portal page
    await page.GoToAsync("https://my.vultr.com/");
    await page.SetViewportAsync(new ViewPortOptions() { Height = 720, Width = 1280 });
    await Task.Delay(5000);

    // Username page
    await page.WaitForSelectorAsync("#login > form > div:nth-child(4) > input");
    await page.FocusAsync("#login > form > div:nth-child(4) > input");
    await page.Keyboard.TypeAsync("admin@raf.com");
    await page.FocusAsync("#login > form > div:nth-child(5) > input");
    await page.Keyboard.TypeAsync("evk-KEJ6jep0tfw4fnc");

    await page.FocusAsync("#login > form > div.form__actions > div > button");
    await page.ClickAsync("#login > form > div.form__actions > div > button");
    await Task.Delay(5000);

    await page.GoToAsync("https://my.vultr.com/billing/#billinghistory");
    await Task.Delay(5000);

    // Arrived at billing page, pull page HTML
    HtmlDocument doc = new();
    doc.LoadHtml(page.GetContentAsync().Result);

    // Grab last 2 receipts
    HtmlNodeCollection bills = doc.DocumentNode.SelectNodes("/html/body/div[8]/div[2]/table/tbody/tr");
    receipts.Add(new Receipt
    {
        Date = bills[1].ChildNodes[3].InnerText,
        Amount = bills[1].ChildNodes[5].InnerText,
        Url = bills[1].ChildNodes[11].ChildNodes[1].Attributes[1].Value,
    });
    receipts.Add(new Receipt
    {
        Date = bills[3].ChildNodes[3].InnerText,
        Amount = bills[3].ChildNodes[5].InnerText,
        Url = bills[3].ChildNodes[11].ChildNodes[1].Attributes[1].Value,
    });
    receipts.Add(new Receipt
    {
        Date = bills[5].ChildNodes[3].InnerText,
        Amount = bills[5].ChildNodes[5].InnerText,
        Url = bills[5].ChildNodes[11].ChildNodes[1].Attributes[1].Value,
    });

    foreach (var receipt in receipts)
    {
        receipt.Date = receipt.Date.Replace("\n", "");
        receipt.Date = receipt.Date.Replace("\t", "");

        receipt.Amount = receipt.Amount.Replace("\n", "");
        receipt.Amount = receipt.Amount.Replace("\t", "");
        receipt.Amount = receipt.Amount.Replace("-$", "");

        await page.GoToAsync("https://my.vultr.com" + receipt.Url);
        await Task.Delay(5000);
        await page.ScreenshotAsync(@".\" + receipt.Date + ".png");
    }
}

async Task RunConcur()
{
    // Download local chromium binary to launch browser
    BrowserFetcher fetcher = new();
    await fetcher.DownloadAsync(BrowserFetcher.DefaultChromiumRevision);

    // Set launchoptions, create browser instance
    LaunchOptions options = new() { Headless = false };

    // Create a browser instance, page instance
    using IBrowser browser = await Puppeteer.LaunchAsync(options);
    using IPage page = await browser.NewPageAsync();

    // Navigate to download portal page
    await page.GoToAsync("https://www.concursolutions.com/");

    // Username input, click next button
    await page.WaitForSelectorAsync("#username-input");
    await page.FocusAsync("#username-input");
    await page.Keyboard.TypeAsync("bpmiller@matw.com");

    await page.FocusAsync("#btnSubmit > span > div > span");
    await page.ClickAsync("#btnSubmit > span > div > span");
    await page.WaitForNavigationAsync();
    await Task.Delay(5000);

    // Password input, click next button
    await page.WaitForSelectorAsync("#password");
    await page.FocusAsync("#password");
    await page.Keyboard.TypeAsync("hpb5wut7xyt2PMK.qha");

    await page.FocusAsync("#btnSubmit");
    await page.ClickAsync("#btnSubmit");
    await page.WaitForNavigationAsync();
    await Task.Delay(5000);

    // Dismiss annoying popup
    await page.WaitForSelectorAsync("body > div.sapcnqr-modal.sapcnqr-messagebox.sapcnqr-modal__fade.sapcnqr-modal__fade--in > div > div");
    await page.WaitForSelectorAsync("body > div.sapcnqr-modal.sapcnqr-messagebox.sapcnqr-modal__fade.sapcnqr-modal__fade--in > div > div > div.sapcnqr-modal__header.sapcnqr-messagebox__header--warning > button > i");
    await page.FocusAsync("body > div.sapcnqr-modal.sapcnqr-messagebox.sapcnqr-modal__fade.sapcnqr-modal__fade--in > div > div > div.sapcnqr-modal__header.sapcnqr-messagebox__header--warning > button > i");
    await page.ClickAsync("body > div.sapcnqr-modal.sapcnqr-messagebox.sapcnqr-modal__fade.sapcnqr-modal__fade--in > div > div > div.sapcnqr-modal__header.sapcnqr-messagebox__header--warning > button > i");
    await Task.Delay(5000);

    // Click open reports
    await page.WaitForSelectorAsync("#cnqr-openreports-tourtip > a");
    await page.FocusAsync("#cnqr-openreports-tourtip > a");
    await page.ClickAsync("#cnqr-openreports-tourtip > a");
    await Task.Delay(5000);

    // Dismiss annoying popup 2
    await page.WaitForSelectorAsync("#walkme-balloon-9675904 > div.walkme-custom-balloon-mid-div");
    await page.WaitForSelectorAsync("#walkme-balloon-9675904-focusable-element-1");
    await page.FocusAsync("#walkme-balloon-9675904-focusable-element-1");
    await page.ClickAsync("#walkme-balloon-9675904-focusable-element-1");
    await Task.Delay(5000);

    // Arrived at expenses page, pull page HTML
    HtmlDocument doc = new();
    doc.LoadHtml(page.GetContentAsync().Result);

    // Grab expense report tiles, find tiles that aren't submitted
    HtmlNodeCollection tiles = doc.DocumentNode.SelectNodes("/html/body/div[2]/div/div/div/div[1]/div/div[1]/div/section/div/ul/li");
    List<string> tileUrls = new();

    foreach (HtmlNode tile in tiles)
    {
        if (tile.InnerText.Contains("Not Submitted"))
        {
            tileUrls.Add(tile.ChildNodes[0].Attributes["href"].Value);
        }
    }

    foreach (var tile in tileUrls)
    {
        // Arrived at expenses page, pull page HTML
        HtmlDocument expenseDoc = new();
        expenseDoc.LoadHtml(page.GetContentAsync().Result);

        await page.GoToAsync("https://us2.concursolutions.com" + tile);
        await Task.Delay(5000);

        // Click into expense on tile
        await page.WaitForSelectorAsync("#cnqr-app-content > div > div.nui-expense-grid.nuiexp-horizontal-scrollable-grid > table > tbody > tr");
        await page.FocusAsync("#cnqr-app-content > div > div.nui-expense-grid.nuiexp-horizontal-scrollable-grid > table > tbody > tr");
        await page.ClickAsync("#cnqr-app-content > div > div.nui-expense-grid.nuiexp-horizontal-scrollable-grid > table > tbody > tr");
        await Task.Delay(5000);

        // Remove any existing input, fill in expense type
        await page.WaitForSelectorAsync("#expName-1836245");
        await page.FocusAsync("#expName-1836245");
        for (int i = 0; i < 50; i++)
        {
            await page.Keyboard.PressAsync("Backspace");
            await Task.Delay(500);
        }
        await page.Keyboard.TypeAsync("Subscriptions");

        // Remove any existing input, fill in expense city
        await page.WaitForSelectorAsync("#locName-1836249");
        await page.FocusAsync("#locName-1836249");
        for (int i = 0; i < 50; i++)
        {
            await page.Keyboard.PressAsync("Backspace");
            await Task.Delay(500);
        }
        await page.Keyboard.TypeAsync("Seattle, Washington");
        await Task.Delay(500);

        // Click file upload dialog
        await page.ClickAsync("#entry-receipts > div > div > ul > li > div > button");
        await Task.Delay(5000);

        await page.ClickAsync(@"#cnqr-vf\@e9Jls\$I > ul > li.sapcnqr-grid-list-item.sapcnqr-grid-list-item--create");

        var fileChooserDialogTask = page.WaitForFileChooserAsync(); // Do not await here
        await Task.WhenAll(fileChooserDialogTask, page.ClickAsync("input[type='file']"));
        var fileChooser = await fileChooserDialogTask;
        await fileChooser.AcceptAsync(@".\12-01-2022.png");

        // Find expense amount, match to correct Vultr receipt

        await page.ClickAsync(@"#cnqr-app-content > div > div > div:nth-child(1) > div > div.sapcnqr-button-toolbar.toolbar-flex-container.sapcnqr-button-toolbar--muted > div > button");
        await Task.Delay(5000);

        await page.WaitForSelectorAsync("#cnqr-app-content > div > div:nth-child(2) > header > div > div > div > button");
        await page.FocusAsync("#cnqr-app-content > div > div:nth-child(2) > header > div > div > div > button");
        await page.ClickAsync("#cnqr-app-content > div > div:nth-child(2) > header > div > div > div > button");
        await Task.Delay(5000);
    }

    System.Console.WriteLine("stop");
}
