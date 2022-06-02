using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Common.Data;
using Crawler.App;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WebSocketSharp;
using WebSocketSharp.Server;

#pragma warning disable CS4014 // ignore that I'm not awaiting a task, by design 

public class SocketConnection : WebSocketBehavior
{
    public static WebSocketServiceManager SocketServer { get; set; }
    public static EmailCrawler EmailCrawler { get; set; }
    public static SmartmatchCrawler SmartMatchCrawler { get; set; }
    public static ParascriptCrawler ParascriptCrawler { get; set; }
    public static RoyalCrawler RoyalCrawler { get; set; }

    private readonly ILogger<SocketConnection> logger;
    private readonly IConfiguration config;
    private readonly ComponentTask tasks;
    private readonly DatabaseContext context;

    private CancellationTokenSource emailTokenSource = new CancellationTokenSource();
    private CancellationTokenSource smTokenSource = new CancellationTokenSource();
    private CancellationTokenSource psTokenSource = new CancellationTokenSource();
    private CancellationTokenSource rmTokenSource = new CancellationTokenSource();
    private System.Net.IPAddress ipAddress;

    public SocketConnection(ILogger<SocketConnection> logger, IConfiguration config, ComponentTask tasks, DatabaseContext context)
    {
        this.logger = logger;
        this.config = config;
        this.tasks = tasks;
        this.context = context;
    }

    protected override void OnOpen()
    {
        ipAddress = Context.UserEndPoint.Address;
        logger.LogInformation("Connected to client: {0}, Total clients: {1}", ipAddress, Sessions.Count);

        Task.Run(() => EmailCrawler.ExecuteAsyncAuto(emailTokenSource.Token));
        Task.Run(() => SmartMatchCrawler.ExecuteAsyncAuto(smTokenSource.Token));
        Task.Run(() => ParascriptCrawler.ExecuteAsyncAuto(psTokenSource.Token));
        Task.Run(() => RoyalCrawler.ExecuteAsyncAuto(rmTokenSource.Token));
    }

    protected override void OnClose(CloseEventArgs e)
    {
        logger.LogInformation("Connection closed: {0}, Total clients: {1}", ipAddress, Sessions.Count);
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
                await Task.Run(() => SmartMatchCrawler.ExecuteAsync(smTokenSource.Token));
            }
            if (message.Property == "AutoEnabled")
            {
                SmartMatchCrawler.Settings.AutoCrawlEnabled = bool.Parse(message.Value);
            }
            if (message.Property == "AutoDate")
            {
                string[] newDay = message.Value.Split('/');
                SmartMatchCrawler.Settings.ExecMonth = int.Parse(newDay[0]);
                SmartMatchCrawler.Settings.ExecDay = int.Parse(newDay[1]);
                SmartMatchCrawler.Settings.ExecYear = int.Parse(newDay[2]);
            }

            Task.Run(() => SmartMatchCrawler.ExecuteAsyncAuto(smTokenSource.Token));
        }
        if (message.Directory == "Parascript")
        {
            psTokenSource.Cancel();
            psTokenSource = new CancellationTokenSource();

            if (message.Property == "Force")
            {
                await Task.Run(() => ParascriptCrawler.ExecuteAsync(psTokenSource.Token));
            }
            if (message.Property == "AutoEnabled")
            {
                ParascriptCrawler.Settings.AutoCrawlEnabled = bool.Parse(message.Value);
            }
            if (message.Property == "AutoDate")
            {
                string[] newDay = message.Value.Split('/');
                ParascriptCrawler.Settings.ExecMonth = int.Parse(newDay[0]);
                ParascriptCrawler.Settings.ExecDay = int.Parse(newDay[1]);
                ParascriptCrawler.Settings.ExecYear = int.Parse(newDay[2]);
            }

            Task.Run(() => ParascriptCrawler.ExecuteAsyncAuto(psTokenSource.Token));
        }
        if (message.Directory == "RoyalMail")
        {
            rmTokenSource.Cancel();
            rmTokenSource = new CancellationTokenSource();

            if (message.Property == "Force")
            {
                await Task.Run(() => RoyalCrawler.ExecuteAsync(rmTokenSource.Token));
            }
            if (message.Property == "AutoEnabled")
            {
                RoyalCrawler.Settings.AutoCrawlEnabled = bool.Parse(message.Value);
            }
            if (message.Property == "AutoDate")
            {
                string[] newDay = message.Value.Split('/');
                RoyalCrawler.Settings.ExecMonth = int.Parse(newDay[0]);
                RoyalCrawler.Settings.ExecDay = int.Parse(newDay[1]);
                RoyalCrawler.Settings.ExecYear = int.Parse(newDay[2]);
            }

            Task.Run(() => RoyalCrawler.ExecuteAsyncAuto(rmTokenSource.Token));
        }
    }

    public void SendMessage(bool smartMatch = false, bool parascript = false, bool royalMail = false)
    {
        string data = ReportStatus(smartMatch, parascript, royalMail);
        SocketServer.Broadcast(data);
    }



    private string ReportStatus(bool smartMatch = false, bool parascript = false, bool royalMail = false)
    {
        List<List<BuildInfo>> buildBundle = GetAvailableBuilds(smartMatch, parascript, royalMail);

        Dictionary<ComponentStatus, string> statusMap = new Dictionary<ComponentStatus, string>() { { ComponentStatus.Ready, "Ready" }, { ComponentStatus.InProgress, "In Progress" }, { ComponentStatus.Error, "Error" }, { ComponentStatus.Disabled, "Disabled" } };

        if (smartMatch)
        {
            SocketResponse SmartMatch = new SocketResponse()
            {
                DirectoryStatus = statusMap[tasks.SmartMatch],
                AutoEnabled = SmartMatchCrawler.Settings.AutoCrawlEnabled,
                AvailableBuilds = buildBundle[0],
                AutoDate = SmartMatchCrawler.Settings.ExecMonth + "/" + SmartMatchCrawler.Settings.ExecDay + "/" + SmartMatchCrawler.Settings.ExecYear
            };

            string serializedObject = JsonSerializer.Serialize(new { SmartMatch });
            return serializedObject;
        }
        if (parascript)
        {
            SocketResponse Parascript = new SocketResponse()
            {
                DirectoryStatus = statusMap[tasks.Parascript],
                AutoEnabled = ParascriptCrawler.Settings.AutoCrawlEnabled,
                AvailableBuilds = buildBundle[1],
                AutoDate = ParascriptCrawler.Settings.ExecMonth + "/" + ParascriptCrawler.Settings.ExecDay + "/" + ParascriptCrawler.Settings.ExecYear
            };

            string serializedObject = JsonSerializer.Serialize(new { Parascript });
            return serializedObject;
        }
        if (royalMail)
        {
            SocketResponse RoyalMail = new SocketResponse()
            {
                DirectoryStatus = statusMap[tasks.RoyalMail],
                AutoEnabled = RoyalCrawler.Settings.AutoCrawlEnabled,
                AvailableBuilds = buildBundle[2],
                AutoDate = RoyalCrawler.Settings.ExecMonth + "/" + RoyalCrawler.Settings.ExecDay + "/" + RoyalCrawler.Settings.ExecYear
            };

            string serializedObject = JsonSerializer.Serialize(new { RoyalMail });
            return serializedObject;
        }

        return "No Directory specified";
    }

    private List<List<BuildInfo>> GetAvailableBuilds(bool smartMatch = false, bool parascript = false, bool royalMail = false)
    {
        List<BuildInfo> smBuilds = new List<BuildInfo>();
        List<BuildInfo> psBuilds = new List<BuildInfo>();
        List<BuildInfo> rmBuilds = new List<BuildInfo>();
        List<List<BuildInfo>> buildBundle = new List<List<BuildInfo>>();

        if (smartMatch)
        {
            List<UspsBundle> uspsBundles = context.UspsBundles.Where(x => (x.IsReadyForBuild == true)).OrderByDescending(x => x.DataYearMonth).ToList();
            foreach (UspsBundle bundle in uspsBundles)
            {
                smBuilds.Add(new BuildInfo()
                {
                    Name = bundle.DataYearMonth,
                    DownloadDate = bundle.DownloadDate,
                    DownloadTime = bundle.DownloadTime,
                    FileCount = bundle.FileCount
                });
            }
        }
        if (parascript)
        {
            List<ParaBundle> paraBundles = context.ParaBundles.Where(x => (x.IsReadyForBuild == true)).OrderByDescending(x => x.DataYearMonth).ToList();
            foreach (ParaBundle bundle in paraBundles)
            {
                psBuilds.Add(new BuildInfo()
                {
                    Name = bundle.DataYearMonth,
                    DownloadDate = bundle.DownloadDate,
                    DownloadTime = bundle.DownloadTime,
                    FileCount = bundle.FileCount
                });
            }
        }
        if (royalMail)
        {
            List<RoyalBundle> royalBundles = context.RoyalBundles.Where(x => (x.IsReadyForBuild == true)).OrderByDescending(x => x.DataYearMonth).ToList();
            foreach (RoyalBundle bundle in royalBundles)
            {
                rmBuilds.Add(new BuildInfo()
                {
                    Name = bundle.DataYearMonth,
                    DownloadDate = bundle.DownloadDate,
                    DownloadTime = bundle.DownloadTime,
                    FileCount = bundle.FileCount
                });
            }
        }

        buildBundle.Add(smBuilds);
        buildBundle.Add(psBuilds);
        buildBundle.Add(rmBuilds);

        return buildBundle;
    }
}
