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

// - Refactor downloading to download more than one file at once
// - Revert WaitForDownload to look for a list instead of single file
// - Make download paths consistent
// - Implement CheckBuildReady
namespace Crawler.App
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> logger;
        private readonly DatabaseContext context;

        public Worker(ILogger<Worker> logger, IServiceScopeFactory factory)
        {
            this.logger = logger;
            this.context = factory.CreateScope().ServiceProvider.GetRequiredService<DatabaseContext>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            context.Database.EnsureCreated();

            while (!stoppingToken.IsCancellationRequested)
            {
                await PullFiles();
                CheckFiles();
                await DownloadFiles();
                CheckBuildReady();

                await Task.Delay(1000, stoppingToken);
            }
        }



        private async Task PullFiles()
        {
            // List of formatted files from website to return
            List<UspsFile> fileList = new List<UspsFile>();

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
                        // Set up page behavior and client options
                        await page.Client.SendAsync(@"Page.setDownloadBehavior", new { behavior = @"allow", downloadPath = @"C:\Users\billy\Desktop" });

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

                            context.TempFiles.Add(file);
                        }

                        context.SaveChanges();
                        // Exit page, browser
                    }
                    catch (System.Exception e)
                    {
                        System.Console.WriteLine(e.Message);
                    }
                }
            }
        }

        private void CheckFiles()
        {
            List<UspsFile> files = context.TempFiles.ToList();

            foreach (var file in files)
            {
                // Check if file is unique against the db
                bool fileExists = context.UspsFiles.Any(x => file.FileId == x.FileId);

                if (!fileExists)
                {
                    if (!File.Exists(Directory.GetCurrentDirectory() + @"\Downloads\" + file.DataYear + @"\" + file.DataMonth + @"\" + file.FileName))
                    {
                        file.OnDisk = false;
                    }               
                    context.UspsFiles.Add(file); 

                    bool bundleExists = context.UspsBundles.Any(x => file.DataMonth == x.DataMonth);

                    if (!bundleExists)
                    {
                        UspsBundle newBundle = new UspsBundle()
                        {
                            DataMonth = file.DataMonth,
                            DataYear = file.DataYear,
                            IsReadyForBuild = false
                        };

                        newBundle.BuildFiles.Add(file);
                    }
                    else
                    {
                        UspsBundle existingBundle = context.UspsBundles.Where(x => x.DataMonth == file.DataMonth).FirstOrDefault();

                        existingBundle.BuildFiles.Add(file);
                    }
                }
            }

            context.TempFiles.RemoveRange(files);

            context.SaveChanges();
        }

        private async Task WaitForDownload(string filePath)
        {
            if (File.Exists(filePath))
            {
                // Wait for 60s
                System.Console.WriteLine("waiting for... :\t" + filePath);
                await Task.Delay(60000);
            }
            else
            {
                return;
            }

            await WaitForDownload(filePath);

        }
        
        private async Task DownloadFiles()
        {
            List<UspsFile> offDisk = context.UspsFiles.Where(x => x.OnDisk == false).ToList();
        
            foreach (var file in offDisk)
            {
                // Ensure there is a folder to land in
                Directory.CreateDirectory(Directory.GetCurrentDirectory() + @"\Downloads\" + file.DataYear);
                Directory.CreateDirectory(Directory.GetCurrentDirectory() + @"\Downloads\" + file.DataYear + @"\" + file.DataMonth);
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
                        // Set up page behavior and client options
                        await page.Client.SendAsync(@"Page.setDownloadBehavior", new { behavior = @"allow", downloadPath = Directory.GetCurrentDirectory() + @"\Downloads\Temp" });

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


                        foreach (var file in offDisk)
                        {
                            string path = Directory.GetCurrentDirectory() + @"\Downloads\" + file.DataYear + @"\" + file.DataMonth;
                            await page.Client.SendAsync(@"Page.setDownloadBehavior", new { behavior = @"allow", downloadPath = path });
                            await page.EvaluateExpressionAsync(@"getFileForDownload(" + file.ProductKey + "," + file.FileId + ",rw_" + file.FileId + ");");
                            await Task.Delay(5000);
                            
                            //wait for file download
                            await WaitForDownload(path + @"\" + file.FileName + @".CRDOWNLOAD");
                            
                            file.OnDisk = true;
                            context.UspsFiles.Update(file);
                        }

                        context.SaveChanges();
                    }
                    catch (System.Exception e)
                    {
                        System.Console.WriteLine(e.Message);
                    }
                }
            }
        }

        private void CheckBuildReady()
        {

        }

    }
}
