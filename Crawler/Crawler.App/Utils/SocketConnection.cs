using System;
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

namespace Crawler.App.Utils;

public class SocketConnection : WebSocketBehavior
{
    static public WebSocketServiceManager SocketServer;
    static public EmailCrawler EmailCrawler;
    static public ParascriptCrawler ParascriptCrawler;

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
        Task.Run(() => ParascriptCrawler.ExecuteAsyncAuto(psTokenSource.Token));
    }

    protected override void OnClose(CloseEventArgs e)
    {
        logger.LogInformation("Connection closed: {0}, Total clients: {1}", ipAddress, Sessions.Count);
    }

    protected override async void OnMessage(MessageEventArgs e)
    {
        SocketMessage message = JsonConvert.DeserializeObject<SocketMessage>(e.Data);

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
    }

    public void SendMessage()
    {
        string data = ReportStatus();
        SocketServer.Broadcast(data);
    }



    private string ReportStatus()
    {
        List<List<BuildInfo>> buildBundle = GetAvailableBuilds();

        Dictionary<ComponentStatus, string> statusMap = new Dictionary<ComponentStatus, string>() { { ComponentStatus.Ready, "Ready" }, { ComponentStatus.InProgress, "In Progress" }, { ComponentStatus.Error, "Error" }, { ComponentStatus.Disabled, "Disabled" } };

        SocketResponse SmartMatch = new SocketResponse()
        {
            AutoCrawlStatus = statusMap[tasks.SmartMatch],
            AvailableBuilds = buildBundle[0]
        };
        SocketResponse Parascript = new SocketResponse()
        {
            AutoCrawlStatus = statusMap[tasks.Parascript],
            AutoCrawlEnabled = ParascriptCrawler.Settings.AutoCrawlEnabled,
            AvailableBuilds = buildBundle[1],
            AutoCrawlDate = ParascriptCrawler.Settings.ExecMonth + "/" + ParascriptCrawler.Settings.ExecDay + "/" + ParascriptCrawler.Settings.ExecYear
        };
        SocketResponse RoyalMail = new SocketResponse()
        {
            AutoCrawlStatus = statusMap[tasks.RoyalMail],
            AvailableBuilds = buildBundle[2]
        };

        string serializedObject = JsonConvert.SerializeObject(new { SmartMatch, Parascript, RoyalMail });

        return serializedObject;
    }

    private List<List<BuildInfo>> GetAvailableBuilds()
    {
        List<BuildInfo> smBuilds = new List<BuildInfo>();
        List<BuildInfo> psBuilds = new List<BuildInfo>();
        List<BuildInfo> rmBuilds = new List<BuildInfo>();

        List<UspsBundle> uspsBundles = context.UspsBundles.Where(x => (x.IsReadyForBuild == true)).ToList();
        List<ParaBundle> paraBundles = context.ParaBundles.Where(x => (x.IsReadyForBuild == true)).ToList();
        List<RoyalBundle> royalBundles = context.RoyalBundles.Where(x => (x.IsReadyForBuild == true)).ToList();

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

        List<List<BuildInfo>> buildBundle = new List<List<BuildInfo>>();
        buildBundle.Add(smBuilds);
        buildBundle.Add(psBuilds);
        buildBundle.Add(rmBuilds);

        return buildBundle;
    }
}
