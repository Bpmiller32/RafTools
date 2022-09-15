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
using Microsoft.Extensions.Configuration;
using Common.Data;

namespace Crawler.App
{
    public class EmailCrawler
    {
        public Settings Settings { get; set; } = new Settings() { Name = "Email" };
        public ComponentStatus Status { get; set; }

        private readonly ILogger logger;
        private readonly IConfiguration config;
        private readonly SocketConnection connection;
        private readonly DatabaseContext context;

        private PafKey tempKey = new PafKey();

        public EmailCrawler(ILogger<EmailCrawler> logger, IConfiguration config, SocketConnection connection, DatabaseContext context)
        {
            this.logger = logger;
            this.config = config;
            this.connection = connection;
            this.context = context;

            Settings = Settings.Validate(Settings, config);
        }

        public async Task ExecuteAsyncAuto(CancellationToken stoppingToken)
        {
            if (Settings.CrawlerEnabled == false)
            {
                logger.LogInformation("Crawler disabled");
                Status = ComponentStatus.Disabled;
                // connection.SendMessage();
                return;
            }

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    logger.LogInformation("Starting Crawler");
                    Status = ComponentStatus.InProgress;
                    // connection.SendMessage();

                    GetKey(stoppingToken);
                    SaveKey(stoppingToken);

                    TimeSpan waitTime = Settings.CalculateWaitTime(logger, Settings);
                    await Task.Delay(TimeSpan.FromHours(waitTime.TotalHours), stoppingToken);
                }
            }
            catch (System.Exception e)
            {
                Status = ComponentStatus.Error;
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
                client.Authenticate(Settings.UserName, Settings.Password);

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