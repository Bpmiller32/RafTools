using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MailKit.Net.Imap;
using MailKit;
using MimeKit;
using System;
using System.Text.RegularExpressions;
using MailKit.Search;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Common.Data;
using Crawler.App.Utils;

namespace Crawler.App
{
    public class EmailCrawler
    {
        private readonly ILogger logger;
        private readonly IConfiguration config;
        private readonly ComponentTask tasks;
        private readonly SocketConnection connection;
        private readonly DatabaseContext context;

        private Settings settings = new Settings() { Name = "Email" };
        private PafKey tempKey = new PafKey();

        public EmailCrawler(ILogger<EmailCrawler> logger, IConfiguration config, ComponentTask tasks, SocketConnection connection, DatabaseContext context)
        {
            this.logger = logger;
            this.config = config;
            this.tasks = tasks;
            this.connection = connection;
            this.context = context;
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            settings = Settings.Validate(settings, config);

            if (settings.CrawlerEnabled == false)
            {
                logger.LogInformation("Crawler disabled");
                tasks.Email = ComponentStatus.Disabled;
                // connection.SendMessage();
                return;
            }

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    logger.LogInformation("Starting Crawler");
                    tasks.Email = ComponentStatus.InProgress;
                    // connection.SendMessage();

                    GetKey(stoppingToken);
                    SaveKey(stoppingToken);

                    TimeSpan waitTime = Settings.CalculateWaitTime(logger, settings);
                    await Task.Delay(TimeSpan.FromHours(waitTime.TotalHours), stoppingToken);
                }
            }
            catch (System.Exception e)
            {
                tasks.Email = ComponentStatus.Error;
                // connection.SendMessage();
                logger.LogError(e.Message);
            }
        }

        private void GetKey(CancellationToken stoppingToken)
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

        private void SaveKey(CancellationToken stoppingToken)
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
                context.SaveChanges();
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