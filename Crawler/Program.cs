using System.Xml;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using PuppeteerSharp;
using PuppeteerSharp.Input;
using HtmlAgilityPack;
using System.Linq;

namespace Crawler
{
    class UspsFile
    {
        public string Name { get; set; }
        public bool Downloaded { get; set; }
        public string Date { get; set; }
        public string Size { get; set; }

        public string ProductKey { get; set; }
        public string FileId { get; set; }
    }

    class Program
    {
        static async Task Main(string[] args)
        {

            // Download local chromium binary to launch browser
            BrowserFetcher fetcher = new BrowserFetcher();
            await fetcher.DownloadAsync(BrowserFetcher.DefaultChromiumRevision);

            // Set launchoptions, create browser instance
            LaunchOptions options = new LaunchOptions()
            {
                Headless = false
            };

            using (Browser browser = await Puppeteer.LaunchAsync(options))
            {
                using (Page page = await browser.NewPageAsync())
                {
                    // Set up headers for requests
                    // Dictionary<string, string> headers = new Dictionary<string, string>()
                    // {
                    //     {@"user-agent", @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/94.0.4606.71 Safari/537.36"},
                    //     {@"accept", @"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9"},
                    //     {@"referer", @"https://epf.usps.gov/"}
                    // };
                    // await page.SetExtraHttpHeadersAsync(headers);

                    // Set up browser behavior and options
                    ClickOptions doubleClick = new ClickOptions()
                    {
                        Button = MouseButton.Left,
                        ClickCount = 2
                    };

                    await page.Client.SendAsync(@"Page.setDownloadBehavior", new { behavior = @"allow", downloadPath = @"C:\Users\billy\Desktop" });

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

                        // Arrrived at download portal page, pull page HTML
                        HtmlDocument doc = new HtmlDocument();
                        doc.LoadHtml(page.GetContentAsync().Result);

                        HtmlNodeCollection fileRows = doc.DocumentNode.SelectNodes(@"/html/body/div[2]/table/tbody/tr/td/div[3]/table/tbody/tr/td/div/table/tbody/tr");

                        // Format downloadables into list
                        List<UspsFile> fileList = new List<UspsFile>();
                        foreach (var fileRow in fileRows)
                        {
                            UspsFile file = new UspsFile();
                            file.Name = fileRow.ChildNodes[5].InnerText.Trim();
                            file.Date = fileRow.ChildNodes[4].InnerText.Trim();
                            file.Size = fileRow.ChildNodes[6].InnerText.Trim();

                            file.ProductKey = fileRow.Attributes[0].Value.Trim().Substring(19, 5);
                            file.FileId = fileRow.Attributes[1].Value.Trim().Substring(3, 7);
                            if (fileRow.ChildNodes[1].InnerText.Trim() == "Downloaded")
                            {
                                file.Downloaded = true;
                            }

                            fileList.Add(file);
                        }

                        // Download files
                        foreach (var file in fileList)
                        {
                            bool isTar = file.Name.Contains(".tar");

                            if ((file.Downloaded == true) && (isTar == true))
                            {
                                await page.EvaluateExpressionAsync(@"getFileForDownload(" + file.ProductKey + "," + file.FileId + ",rw_" + file.FileId + ");");
                                await Task.Delay(5000);
                            }
                        }

                        // Poll and check that all files are finished downloading
                        // TODO make this recursive
                        string[] files = Directory.GetFiles(@"C:\Users\billy\Desktop", @"*.crdownload");
                        foreach (var file in files)
                        {
                            if (File.Exists(file))
                            {
                                // Wait for 60s
                                System.Console.WriteLine("waiting for... :\t" + file);
                                await Task.Delay(60000);
                            }
                        }


                        // Move all completed files to an output folder
                        string[] subDirFiles = Directory.GetFiles(@"C:\Users\billy\Desktop", @"*.tar");

                        foreach (var file in files)
                        {
                            File.Move(file, @"C:\Users\billy\Desktop\Done\" + Path.GetFileName(file), true);
                        }

                        System.Console.WriteLine(@"All done!");

                    }
                    catch (System.Exception e)
                    {
                        System.Console.WriteLine(e.Message);
                    }
                }
            }
        }
    }
}