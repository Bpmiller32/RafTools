using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MailKit.Net.Pop3;
using MailKit.Net.Imap;
using HtmlAgilityPack;
using MailKit;
using MimeKit;
using System;
using System.Text.RegularExpressions;
using System.Text;
using MailKit.Search;
using System.Collections.Generic;
using Crawler.Data;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Crawler.App
{
    public class EmailCrawler : BackgroundService
    {
        private readonly ILogger logger;
        private readonly IConfiguration config;
        private readonly CrawlTask tasks;
        private readonly DatabaseContext context;

        private CancellationToken stoppingToken;
        private Settings settings = new Settings() { Name = "Email" };

        PafKey tempKey = new PafKey();

        public EmailCrawler(ILogger<EmailCrawler> logger, IConfiguration config, IServiceScopeFactory factory, CrawlTask tasks)
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
                tasks.Email = CrawlStatus.Disabled;
                logger.LogInformation("Crawler disabled");
                return;
            }

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    logger.LogInformation("Starting Crawler");
                    tasks.Email = CrawlStatus.Enabled;

                    GetKey();
                    SaveKey();

                    TimeSpan waitTime = Settings.CalculateWaitTime(logger, settings);
                    await Task.Delay(TimeSpan.FromHours(waitTime.TotalHours), stoppingToken);
                }
            }
            catch (System.Exception e)
            {
                tasks.Email = CrawlStatus.Error;
                logger.LogError(e.Message);
            }
        }

        private void GetKey()
        {
            if (stoppingToken.IsCancellationRequested == true)
            {
                return;
            }

            using (var client = new ImapClient())
            {
                client.Connect(@"outlook.office365.com", 993, true);
                client.Authenticate(settings.UserName, settings.Password);

                client.Inbox.Open(FolderAccess.ReadOnly);

                var query = SearchQuery.FromContains(@"paf.production@afd.co.uk");
                IList<UniqueId> uids = client.Inbox.Search(query);

                var latestEmail = client.Inbox.GetMessage(uids[uids.Count - 1]);

                tempKey.Value = FilterKey(latestEmail);
                tempKey.DataMonth = latestEmail.Date.Month;
                tempKey.DataYear = latestEmail.Date.Year;

                client.Disconnect(true);
            }
        }

        private void SaveKey()
        {
            if (String.IsNullOrEmpty(tempKey.Value))
            {
                throw new Exception("Key is empty, cannot save to db");
            }

            bool keyInDb = context.PafKeys.Any(x => (tempKey.Value == x.Value));

            if (!keyInDb)
            {
                logger.LogInformation("Unique PafKey added: " + tempKey.DataMonth + "/" + tempKey.DataYear);
                context.PafKeys.Add(tempKey);
            }
        }

        private string FilterKey(MimeMessage latestEmail)
        {
            if (latestEmail.TextBody == null)
            {
                throw new Exception("Email body missing/key is in rich HTML");
            }

            Regex regex = new Regex(@"(...)( / )(...)( / )(...)( / )(...)( / )(...)( / )(...)( / )(...)( / )(...)");
            Match match = regex.Match(latestEmail.TextBody);

            if (match == null)
            {
                throw new Exception("Key could not be found in email body");
            }

            string key = match.Groups[1].Value + match.Groups[3].Value + match.Groups[5].Value + match.Groups[7].Value + match.Groups[9].Value + match.Groups[11].Value + match.Groups[13].Value + match.Groups[15].Value;

            return key;
        }
    }
}