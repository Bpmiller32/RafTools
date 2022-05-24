using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WebSocketSharp;
using WebSocketSharp.Server;

#pragma warning disable CS4014 // ignore that I'm not awaiting a task, by design 

namespace Crawler.App.Utils;

public class SocketConnection : WebSocketBehavior
{
    static public WebSocketServiceManager SocketServer;
    static public EmailCrawler EmailCrawler;
    static public SmartmatchCrawler SmartMatchCrawler;
    static public ParascriptCrawler ParascriptCrawler;
    static public RoyalCrawler RoyalCrawler;

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

        Task.Run(() => EmailCrawler.ExecuteAsync(emailTokenSource.Token));
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
        // TODO: switch to System.Text.Json to remove Newtonsoft dependency
        SocketMessage message = JsonConvert.DeserializeObject<SocketMessage>(e.Data);

        if (message.Crawler == "SmartMatch")
        {
            smTokenSource.Cancel();
            smTokenSource = new CancellationTokenSource();

            if (message.Property == "Force")
            {
                await Task.Run(() => SmartMatchCrawler.ExecuteAsync(smTokenSource.Token));
            }
            if (message.Property == "autoCrawlEnabled")
            {
                SmartMatchCrawler.Settings.AutoCrawlEnabled = bool.Parse(message.Value);
            }
            if (message.Property == "autoCrawlDate")
            {
                string[] newDay = message.Value.Split('/');
                SmartMatchCrawler.Settings.ExecMonth = int.Parse(newDay[0]);
                SmartMatchCrawler.Settings.ExecDay = int.Parse(newDay[1]);
                SmartMatchCrawler.Settings.ExecYear = int.Parse(newDay[2]);
            }

            Task.Run(() => SmartMatchCrawler.ExecuteAsyncAuto(smTokenSource.Token));
        }
        if (message.Crawler == "Parascript")
        {
            psTokenSource.Cancel();
            psTokenSource = new CancellationTokenSource();

            if (message.Property == "Force")
            {
                await Task.Run(() => ParascriptCrawler.ExecuteAsync(psTokenSource.Token));
            }
            if (message.Property == "autoCrawlEnabled")
            {
                ParascriptCrawler.Settings.AutoCrawlEnabled = bool.Parse(message.Value);
            }
            if (message.Property == "autoCrawlDate")
            {
                string[] newDay = message.Value.Split('/');
                ParascriptCrawler.Settings.ExecMonth = int.Parse(newDay[0]);
                ParascriptCrawler.Settings.ExecDay = int.Parse(newDay[1]);
                ParascriptCrawler.Settings.ExecYear = int.Parse(newDay[2]);
            }

            Task.Run(() => ParascriptCrawler.ExecuteAsyncAuto(psTokenSource.Token));
        }
        if (message.Crawler == "RoyalMail")
        {
            rmTokenSource.Cancel();
            rmTokenSource = new CancellationTokenSource();

            if (message.Property == "Force")
            {
                await Task.Run(() => RoyalCrawler.ExecuteAsync(rmTokenSource.Token));
            }
            if (message.Property == "autoCrawlEnabled")
            {
                RoyalCrawler.Settings.AutoCrawlEnabled = bool.Parse(message.Value);
            }
            if (message.Property == "autoCrawlDate")
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
                AutoCrawlStatus = statusMap[tasks.SmartMatch],
                AutoCrawlEnabled = SmartMatchCrawler.Settings.AutoCrawlEnabled,
                AvailableBuilds = buildBundle[0],
                AutoCrawlDate = SmartMatchCrawler.Settings.ExecMonth + "/" + SmartMatchCrawler.Settings.ExecDay + "/" + SmartMatchCrawler.Settings.ExecYear
            };

            string serializedObject = JsonConvert.SerializeObject(new { SmartMatch });
            return serializedObject;
        }
        if (parascript)
        {
            SocketResponse Parascript = new SocketResponse()
            {
                AutoCrawlStatus = statusMap[tasks.Parascript],
                AutoCrawlEnabled = ParascriptCrawler.Settings.AutoCrawlEnabled,
                AvailableBuilds = buildBundle[1],
                AutoCrawlDate = ParascriptCrawler.Settings.ExecMonth + "/" + ParascriptCrawler.Settings.ExecDay + "/" + ParascriptCrawler.Settings.ExecYear
            };

            string serializedObject = JsonConvert.SerializeObject(new { Parascript });
            return serializedObject;
        }
        if (royalMail)
        {
            SocketResponse RoyalMail = new SocketResponse()
            {
                AutoCrawlStatus = statusMap[tasks.RoyalMail],
                AutoCrawlEnabled = RoyalCrawler.Settings.AutoCrawlEnabled,
                AvailableBuilds = buildBundle[2],
                AutoCrawlDate = RoyalCrawler.Settings.ExecMonth + "/" + RoyalCrawler.Settings.ExecDay + "/" + RoyalCrawler.Settings.ExecYear
            };

            string serializedObject = JsonConvert.SerializeObject(new { RoyalMail });
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
