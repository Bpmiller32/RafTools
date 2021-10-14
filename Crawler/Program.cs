using System.Xml;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using PuppeteerSharp;

namespace Crawler
{
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
                    // Dictionary<string, string> headers = new Dictionary<string, string>()
                    // {
                    //     {@"user-agent", @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/94.0.4606.71 Safari/537.36"},
                    //     {@"accept", @"text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9"},
                    //     {@"referer", @"https://epf.usps.gov/"}
                    // };
                    // await page.SetExtraHttpHeadersAsync(headers);

                    try
                    {
                        // page.Response += (sender, e) =>
                        // {
                        //     System.Console.WriteLine(e.Response);
                        // };

                        await page.GoToAsync("https://epf.usps.gov/");

                        await page.WaitForSelectorAsync(@"#email");
                        await page.FocusAsync(@"#email");
                        await page.Keyboard.TypeAsync("billy.miller@raf.com");
                        await page.FocusAsync(@"#password");
                        await page.Keyboard.TypeAsync("Trixiedog10021002$");

                        await page.ClickAsync(@"#login");

                        await page.WaitForSelectorAsync(@"#r2");
                        await page.ClickAsync(@"#r2");

                        Thread.Sleep(5000);
                        
                        string html = page.GetContentAsync().Result;
                        // XmlDocument xml = new XmlDocument();
                        // xml.LoadXml(html);
                        // XmlNodeList nodes = xml.SelectNodes("//");



                        // CookieParam[] cookies = page.GetCookiesAsync().Result;




                        File.WriteAllText(@"C:\Users\billy\Desktop\usps.html", html);

                        System.Console.WriteLine("stop");
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
