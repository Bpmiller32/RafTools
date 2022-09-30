using MailKit.Net.Imap;
using MailKit;
using MimeKit;
using System.Text.RegularExpressions;
using MailKit.Search;
using Common.Data;

namespace Crawler;

public class EmailCrawler
{
    public Settings Settings { get; set; } = new Settings() { Name = "Email" };
    public ComponentStatus Status { get; set; }

    private readonly ILogger logger;
    private readonly DatabaseContext context;

    private readonly PafKey tempKey = new();

    public EmailCrawler(ILogger<EmailCrawler> logger, IConfiguration config, DatabaseContext context)
    {
        this.logger = logger;
        this.context = context;

        Settings = Settings.Validate(Settings, config);
    }

    public async Task ExecuteAsyncAuto(CancellationToken stoppingToken)
    {
        if (!Settings.CrawlerEnabled)
        {
            logger.LogInformation("Crawler disabled");
            Status = ComponentStatus.Disabled;
            return;
        }

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Starting Crawler");
                Status = ComponentStatus.InProgress;

                GetKey(stoppingToken);
                SaveKey();

                TimeSpan waitTime = Settings.CalculateWaitTime(logger, Settings);
                await Task.Delay(TimeSpan.FromHours(waitTime.TotalHours), stoppingToken);
            }
        }
        catch (Exception e)
        {
            Status = ComponentStatus.Error;
            logger.LogError("{Message}", e.Message);
        }
    }

    private void GetKey(CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        using var client = new ImapClient();
        client.Connect("outlook.office365.com", 993, true, stoppingToken);
        client.Authenticate(Settings.UserName, Settings.Password, stoppingToken);

        client.Inbox.Open(FolderAccess.ReadOnly, stoppingToken);

        var query = SearchQuery.FromContains("paf.production@afd.co.uk");
        IList<UniqueId> uids = client.Inbox.Search(query, stoppingToken);

        var latestEmail = client.Inbox.GetMessage(uids[uids.Count - 1]);

        tempKey.Value = FilterKey(latestEmail);
        tempKey.DataMonth = latestEmail.Date.Month;
        tempKey.DataYear = latestEmail.Date.Year;

        client.Disconnect(true, stoppingToken);
    }

    private void SaveKey()
    {
        if (string.IsNullOrEmpty(tempKey.Value))
        {
            throw new Exception("Key is empty, cannot save to db");
        }

        bool keyInDb = context.PafKeys.Any(x => tempKey.Value == x.Value);

        if (!keyInDb)
        {
            logger.LogInformation("Unique PafKey added: {DataMonth}/{DataYear}", tempKey.DataMonth, tempKey.DataYear);
            context.PafKeys.Add(tempKey);
            context.SaveChanges();
        }
    }

    private static string FilterKey(MimeMessage latestEmail)
    {
        if (latestEmail.TextBody == null)
        {
            throw new Exception("Email body missing/key is in rich HTML");
        }

        Regex regex = new("(...)( / )(...)( / )(...)( / )(...)( / )(...)( / )(...)( / )(...)( / )(...)");
        Match match = regex.Match(latestEmail.TextBody);

        if (match == null)
        {
            throw new Exception("Key could not be found in email body");
        }

        return match.Groups[1].Value + match.Groups[3].Value + match.Groups[5].Value + match.Groups[7].Value + match.Groups[9].Value + match.Groups[11].Value + match.Groups[13].Value + match.Groups[15].Value;
    }
}