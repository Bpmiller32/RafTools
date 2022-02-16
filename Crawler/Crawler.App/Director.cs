using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Crawler.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Crawler.App
{
    public class Director : BackgroundService
    {
        private readonly ILogger<Director> logger;
        private readonly ILogger<EmailCrawler> emailLogger;
        private readonly ILogger<SmartmatchCrawler> smartMatchLogger;
        private readonly ILogger<ParascriptCrawler> parascriptLogger;
        private readonly ILogger<RoyalCrawler> royalLogger;
        private readonly IConfiguration config;
        private readonly DatabaseContext context;

        Settings emailSettings = new Settings() { Name = "Email" };
        Settings smSettings = new Settings() { Name = "SmartMatch" };
        Settings psSettings = new Settings() { Name = "Parascript" };
        Settings rmSettings = new Settings() { Name = "RoyalMail" };

        CrawlTask emailCrawl = new CrawlTask() { Name = "Email", Status = CrawlStatus.Disabled };
        CrawlTask smCrawl = new CrawlTask() { Name = "SmartMatch", Status = CrawlStatus.Disabled };
        CrawlTask psCrawl = new CrawlTask() { Name = "Parascript", Status = CrawlStatus.Disabled };
        CrawlTask rmCrawl = new CrawlTask() { Name = "RoyalMail", Status = CrawlStatus.Disabled };

        public Director(ILogger<Director> logger, ILogger<EmailCrawler> emailLogger, ILogger<SmartmatchCrawler> smartMatchLogger, ILogger<ParascriptCrawler> parascriptLogger, ILogger<RoyalCrawler> royalLogger, IConfiguration config, IServiceScopeFactory factory)
        {
            this.logger = logger;
            this.emailLogger = emailLogger;
            this.smartMatchLogger = smartMatchLogger;
            this.parascriptLogger = parascriptLogger;
            this.royalLogger = royalLogger;
            this.config = config;
            this.context = factory.CreateScope().ServiceProvider.GetRequiredService<DatabaseContext>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            smSettings = Settings.Validate(smSettings, config);
            psSettings = Settings.Validate(psSettings, config);
            rmSettings = Settings.Validate(rmSettings, config);
            emailSettings = Settings.Validate(emailSettings, config);

            context.Database.EnsureCreated();

            // Start EmailCrawler if enabled
            if (emailSettings.CrawlerEnabled == true)
            {
                EmailRunner(stoppingToken);
            }
            // Start SmartMatchCrawler if enabled
            if (smSettings.CrawlerEnabled == true)
            {
                SmartMatchRunner(stoppingToken);
            }
            // Start ParascriptCrawler if enabled
            if (psSettings.CrawlerEnabled == true)
            {
                ParascriptRunner(stoppingToken);
            }
            // Start RoyalCrawler if enabled
            if (rmSettings.CrawlerEnabled == true)
            {
                RoyalRunner(stoppingToken);
            }

            // Report status every minute unless error
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    ReportStatus();
                }
                catch (System.Exception e)
                {
                    logger.LogError(e.Message);
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
        }

        private void EmailRunner(CancellationToken stoppingToken)
        {
            emailLogger.LogInformation("Starting EmailCrawler");
            emailCrawl.Status = CrawlStatus.Enabled;

            emailCrawl.Task = Task.Run(async () =>
            {
                try
                {
                    EmailCrawler email = new EmailCrawler(emailLogger, stoppingToken, emailSettings, context);

                    while (!stoppingToken.IsCancellationRequested)
                    {
                        email.GetKey();
                        email.SaveKey();

                        TimeSpan waitTime = CalculateWaitTime(emailLogger, emailSettings);
                        await Task.Delay(TimeSpan.FromHours(waitTime.TotalHours), stoppingToken);
                    }
                }
                catch (System.Exception e)
                {
                    emailLogger.LogError(e.Message);
                    emailCrawl.Status = CrawlStatus.Error;
                }
            });
        }

        private void SmartMatchRunner(CancellationToken stoppingToken)
        {
            smartMatchLogger.LogInformation("Starting SmartMatchCrawler");
            smCrawl.Status = CrawlStatus.Enabled;

            smCrawl.Task = Task.Run(async () =>
            {
                try
                {
                    SmartmatchCrawler sm = new SmartmatchCrawler(smartMatchLogger, stoppingToken, smSettings, context);

                    while (!stoppingToken.IsCancellationRequested)
                    {
                        await sm.PullFiles();
                        sm.CheckFiles();
                        await sm.DownloadFiles();
                        sm.CheckBuildReady();

                        TimeSpan waitTime = CalculateWaitTime(smartMatchLogger, smSettings);
                        await Task.Delay(TimeSpan.FromHours(waitTime.TotalHours), stoppingToken);
                    }
                }
                catch (System.Exception e)
                {
                    smartMatchLogger.LogError(e.Message);
                    smCrawl.Status = CrawlStatus.Error;
                }
            });
        }

        private void ParascriptRunner(CancellationToken stoppingToken)
        {
            parascriptLogger.LogInformation("Starting ParascriptCrawler");
            psCrawl.Status = CrawlStatus.Enabled;

            psCrawl.Task = Task.Run(async () =>
            {
                try
                {
                    ParascriptCrawler ps = new ParascriptCrawler(parascriptLogger, stoppingToken, psSettings, context);

                    while (!stoppingToken.IsCancellationRequested)
                    {
                        await ps.PullFiles();
                        ps.CheckFiles();
                        await ps.DownloadFiles();
                        ps.CheckBuildReady();

                        TimeSpan waitTime = CalculateWaitTime(parascriptLogger, psSettings);
                        await Task.Delay(TimeSpan.FromHours(waitTime.TotalHours), stoppingToken);
                    }
                }
                catch (System.Exception e)
                {
                    parascriptLogger.LogError(e.Message);
                    psCrawl.Status = CrawlStatus.Error;
                }
            });
        }

        private void RoyalRunner(CancellationToken stoppingToken)
        {
            royalLogger.LogInformation("Starting RoyalCrawler");
            rmCrawl.Status = CrawlStatus.Enabled;

            rmCrawl.Task = Task.Run(async () =>
            {
                try
                {
                    RoyalCrawler rm = new RoyalCrawler(royalLogger, stoppingToken, rmSettings, context);

                    while (!stoppingToken.IsCancellationRequested)
                    {
                        rm.PullFile();
                        rm.CheckFile();
                        await rm.DownloadFile();
                        rm.CheckBuildReady();

                        TimeSpan waitTime = CalculateWaitTime(royalLogger, rmSettings);
                        await Task.Delay(TimeSpan.FromHours(waitTime.TotalHours), stoppingToken);
                    }
                }
                catch (System.Exception e)
                {
                    royalLogger.LogError(e.Message);
                    rmCrawl.Status = CrawlStatus.Error;
                }
            });
        }

        private void ReportStatus()
        {
            TcpClient client = new TcpClient();
            if (!client.ConnectAsync("127.0.0.1", 11001).Wait(TimeSpan.FromSeconds(5)))
            {
                throw new Exception("Timeout trying to connect to server to report");
            }
        }

        private TimeSpan CalculateWaitTime(ILogger logger, Settings settings)
        {
            DateTime execTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, settings.ExecDay, settings.ExecHour, settings.ExecMinute, settings.ExecSecond);
            DateTime endOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month), 23, 23, 59);
            TimeSpan waitTime = execTime - DateTime.Now;

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

            return waitTime;
        }
    }
}
