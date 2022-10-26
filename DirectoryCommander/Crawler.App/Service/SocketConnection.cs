using System.Text.Json;
using Common.Data;
using WebSocketSharp.NetCore;
using WebSocketSharp.NetCore.Server;

namespace Crawler;

#pragma warning disable CS4014 // ignore that I'm not awaiting a task, by design 

public class SocketConnection : WebSocketBehavior
{
    private readonly ILogger<SocketConnection> logger;
    private readonly EmailCrawler emailCrawler;
    private readonly SmartmatchCrawler smartMatchCrawler;
    private readonly ParascriptCrawler parascriptCrawler;
    private readonly RoyalCrawler royalCrawler;

    private readonly CancellationTokenSource emailTokenSource = new();
    private CancellationTokenSource smTokenSource = new();
    private CancellationTokenSource psTokenSource = new();
    private CancellationTokenSource rmTokenSource = new();

    private WebSocketSessionManager server;
    private System.Net.IPAddress ipAddress;

    public SocketConnection(ILogger<SocketConnection> logger, EmailCrawler emailCrawler, SmartmatchCrawler smartMatchCrawler, ParascriptCrawler parascriptCrawler, RoyalCrawler royalCrawler)
    {
        this.logger = logger;

        this.emailCrawler = emailCrawler;

        this.smartMatchCrawler = smartMatchCrawler;
        smartMatchCrawler.SendMessage = SendMessage;

        this.parascriptCrawler = parascriptCrawler;
        parascriptCrawler.SendMessage = SendMessage;

        this.royalCrawler = royalCrawler;
        royalCrawler.SendMessage = SendMessage;
    }

    protected override void OnOpen()
    {
        server = Sessions;

        ipAddress = Context.UserEndPoint.Address;
        logger.LogInformation("Connected to client: {ipAddress}, Total clients: {Count}", ipAddress, Sessions.Count);

        Task.Run(() => emailCrawler.ExecuteAsyncAuto(emailTokenSource.Token));
        Task.Run(() => smartMatchCrawler.ExecuteAsyncAuto(smTokenSource.Token));
        Task.Run(() => parascriptCrawler.ExecuteAsyncAuto(psTokenSource.Token));
        Task.Run(() => royalCrawler.ExecuteAsyncAuto(rmTokenSource.Token));
    }

    protected override void OnClose(CloseEventArgs e)
    {
        logger.LogInformation("Connection closed: {ipAddress}, Total clients: {Count}", ipAddress, Sessions.Count);
    }

    protected override async void OnMessage(MessageEventArgs e)
    {
        SocketMessage message = JsonSerializer.Deserialize<SocketMessage>(e.Data);

        if (message.Directory == "SmartMatch")
        {
            smTokenSource.Cancel();
            smTokenSource = new CancellationTokenSource();

            if (message.Property == "Force")
            {
                await Task.Run(() => smartMatchCrawler.ExecuteAsync(smTokenSource.Token));
            }
            if (message.Property == "AutoEnabled")
            {
                smartMatchCrawler.Settings.AutoCrawlEnabled = bool.Parse(message.Value);
            }
            if (message.Property == "AutoDate")
            {
                string[] newDay = message.Value.Split('/');
                smartMatchCrawler.Settings.ExecMonth = int.Parse(newDay[0]);
                smartMatchCrawler.Settings.ExecDay = int.Parse(newDay[1]);
                smartMatchCrawler.Settings.ExecYear = int.Parse(newDay[2]);
            }

            Task.Run(() => smartMatchCrawler.ExecuteAsyncAuto(smTokenSource.Token));
        }
        if (message.Directory == "Parascript")
        {
            psTokenSource.Cancel();
            psTokenSource = new CancellationTokenSource();

            if (message.Property == "Force")
            {
                await Task.Run(() => parascriptCrawler.ExecuteAsync(psTokenSource.Token));
            }
            if (message.Property == "AutoEnabled")
            {
                parascriptCrawler.Settings.AutoCrawlEnabled = bool.Parse(message.Value);
            }
            if (message.Property == "AutoDate")
            {
                string[] newDay = message.Value.Split('/');
                parascriptCrawler.Settings.ExecMonth = int.Parse(newDay[0]);
                parascriptCrawler.Settings.ExecDay = int.Parse(newDay[1]);
                parascriptCrawler.Settings.ExecYear = int.Parse(newDay[2]);
            }

            Task.Run(() => parascriptCrawler.ExecuteAsyncAuto(psTokenSource.Token));
        }
        if (message.Directory == "RoyalMail")
        {
            rmTokenSource.Cancel();
            rmTokenSource = new CancellationTokenSource();

            if (message.Property == "Force")
            {
                await Task.Run(() => royalCrawler.ExecuteAsync(rmTokenSource.Token));
            }
            if (message.Property == "AutoEnabled")
            {
                royalCrawler.Settings.AutoCrawlEnabled = bool.Parse(message.Value);
            }
            if (message.Property == "AutoDate")
            {
                string[] newDay = message.Value.Split('/');
                royalCrawler.Settings.ExecMonth = int.Parse(newDay[0]);
                royalCrawler.Settings.ExecDay = int.Parse(newDay[1]);
                royalCrawler.Settings.ExecYear = int.Parse(newDay[2]);
            }

            Task.Run(() => royalCrawler.ExecuteAsyncAuto(rmTokenSource.Token));
        }
    }

    private void SendMessage(DirectoryType directoryType, DatabaseContext context)
    {
        // Get available builds
        List<BuildInfo> smBuilds = new();
        List<BuildInfo> psBuilds = new();
        List<BuildInfo> rmBuilds = new();
        List<List<BuildInfo>> buildBundle = new();

        if (directoryType == DirectoryType.SmartMatch)
        {
            foreach (UspsBundle bundle in context.UspsBundles.Where(x => x.IsReadyForBuild).OrderByDescending(x => x.DataYearMonth).ToList())
            {
                smBuilds.Add(new BuildInfo()
                {
                    Name = bundle.DataYearMonth,
                    Date = bundle.DownloadDate,
                    Time = bundle.DownloadTime,
                    FileCount = bundle.FileCount
                });
            }
        }
        if (directoryType == DirectoryType.Parascript)
        {
            foreach (ParaBundle bundle in context.ParaBundles.Where(x => x.IsReadyForBuild).OrderByDescending(x => x.DataYearMonth).ToList())
            {
                psBuilds.Add(new BuildInfo()
                {
                    Name = bundle.DataYearMonth,
                    Date = bundle.DownloadDate,
                    Time = bundle.DownloadTime,
                    FileCount = bundle.FileCount
                });
            }
        }
        if (directoryType == DirectoryType.RoyalMail)
        {
            foreach (RoyalBundle bundle in context.RoyalBundles.Where(x => x.IsReadyForBuild).OrderByDescending(x => x.DataYearMonth).ToList())
            {
                rmBuilds.Add(new BuildInfo()
                {
                    Name = bundle.DataYearMonth,
                    Date = bundle.DownloadDate,
                    Time = bundle.DownloadTime,
                    FileCount = bundle.FileCount
                });
            }
        }

        buildBundle.Add(smBuilds);
        buildBundle.Add(psBuilds);
        buildBundle.Add(rmBuilds);

        // Create SocketResponses
        Dictionary<ComponentStatus, string> statusMap = new() { { ComponentStatus.Ready, "Ready" }, { ComponentStatus.InProgress, "In Progress" }, { ComponentStatus.Error, "Error" }, { ComponentStatus.Disabled, "Disabled" } };
        string serializedObject = "";

        if (directoryType == DirectoryType.SmartMatch)
        {
            DateTime nextDate = Settings.CalculateNextDate(smartMatchCrawler.Settings);

            SocketResponse SmartMatch = new()
            {
                DirectoryStatus = statusMap[smartMatchCrawler.Status],
                AutoEnabled = smartMatchCrawler.Settings.AutoCrawlEnabled,
                AvailableBuilds = buildBundle[0],
                AutoDate = nextDate.Month + "/" + nextDate.Day + "/" + nextDate.Year
            };

            serializedObject = JsonSerializer.Serialize(new { SmartMatch });
        }
        if (directoryType == DirectoryType.Parascript)
        {
            DateTime nextDate = Settings.CalculateNextDate(parascriptCrawler.Settings);

            SocketResponse Parascript = new()
            {
                DirectoryStatus = statusMap[parascriptCrawler.Status],
                AutoEnabled = parascriptCrawler.Settings.AutoCrawlEnabled,
                AvailableBuilds = buildBundle[1],
                AutoDate = nextDate.Month + "/" + nextDate.Day + "/" + nextDate.Year
            };

            serializedObject = JsonSerializer.Serialize(new { Parascript });
        }
        if (directoryType == DirectoryType.RoyalMail)
        {
            DateTime nextDate = Settings.CalculateNextDate(royalCrawler.Settings);

            SocketResponse RoyalMail = new()
            {
                DirectoryStatus = statusMap[royalCrawler.Status],
                AutoEnabled = royalCrawler.Settings.AutoCrawlEnabled,
                AvailableBuilds = buildBundle[2],
                AutoDate = nextDate.Month + "/" + nextDate.Day + "/" + nextDate.Year
            };

            serializedObject = JsonSerializer.Serialize(new { RoyalMail });
        }

        server.Broadcast(serializedObject);
    }
}