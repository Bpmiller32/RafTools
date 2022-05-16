using System.Collections.Generic;
using System.Linq;
using Common.Data;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Crawler.App.Utils;

public class SocketConnection : WebSocketBehavior
{
    static public WebSocketServiceManager SocketServer;

    private readonly ILogger logger;
    private readonly ComponentTask tasks;
    private readonly DatabaseContext context;

    public SocketConnection(ILogger logger, ComponentTask tasks, DatabaseContext context)
    {
        this.logger = logger;
        this.tasks = tasks;
        this.context = context;
    }

    protected override void OnOpen()
    {
        logger.LogInformation("Connected to client: {0}, Total clients: {1}", Context.UserEndPoint.Address, Sessions.Count);
    }

    protected override void OnClose(CloseEventArgs e)
    {
        logger.LogInformation("Connection closed, Total clients: {0}", Sessions.Count);
    }

    protected override void OnError(ErrorEventArgs e)
    {
        logger.LogInformation("Connection error, Total clients: {0}", Sessions.Count);
    }

    protected override void OnMessage(MessageEventArgs e)
    {
        string data = ReportStatus();
        Send(data);
    }

    public void SendMessage()
    {
        string data = ReportStatus();
        SocketServer.Broadcast(data);
    }



    private string ReportStatus()
    {
        List<List<string>> buildBundle = GetAvailableBuilds();

        Dictionary<ComponentStatus, string> statusMap = new Dictionary<ComponentStatus, string>() { { ComponentStatus.Ready, "Ready" }, { ComponentStatus.InProgress, "In Progress" }, { ComponentStatus.Error, "Error" }, { ComponentStatus.Disabled, "Disabled" } };

        SocketResponse SmartMatch = new SocketResponse() { AutoCrawlStatus = statusMap[tasks.SmartMatch], AvailableBuilds = buildBundle[0] };
        SocketResponse Parascript = new SocketResponse() { AutoCrawlStatus = statusMap[tasks.Parascript], AvailableBuilds = buildBundle[1] };
        SocketResponse RoyalMail = new SocketResponse() { AutoCrawlStatus = statusMap[tasks.RoyalMail], AvailableBuilds = buildBundle[2] };

        string serializedObject = JsonConvert.SerializeObject(new { SmartMatch, Parascript, RoyalMail });

        return serializedObject;
    }

    private List<List<string>> GetAvailableBuilds()
    {
        List<string> smBuilds = new List<string>();
        List<string> psBuilds = new List<string>();
        List<string> rmBuilds = new List<string>();

        List<UspsBundle> uspsBundles = context.UspsBundles.Where(x => (x.IsReadyForBuild == true)).ToList();
        List<ParaBundle> paraBundles = context.ParaBundles.Where(x => (x.IsReadyForBuild == true)).ToList();
        List<RoyalBundle> royalBundles = context.RoyalBundles.Where(x => (x.IsReadyForBuild == true)).ToList();

        foreach (UspsBundle bundle in uspsBundles)
        {
            smBuilds.Add(bundle.DataYearMonth);
        }
        foreach (ParaBundle bundle in paraBundles)
        {
            string dataMonth;
            if (bundle.DataMonth < 10)
            {
                dataMonth = "0" + bundle.DataMonth;
            }
            else
            {
                dataMonth = bundle.DataMonth.ToString();
            }

            string dataYearMonth = bundle.DataYear.ToString() + dataMonth;
            psBuilds.Add(dataYearMonth);
        }
        foreach (RoyalBundle bundle in royalBundles)
        {
            string dataMonth;
            if (bundle.DataMonth < 10)
            {
                dataMonth = "0" + bundle.DataMonth;
            }
            else
            {
                dataMonth = bundle.DataMonth.ToString();
            }

            string dataYearMonth = bundle.DataYear.ToString() + dataMonth;
            rmBuilds.Add(dataYearMonth);
        }

        List<List<string>> buildBundle = new List<List<string>>();
        buildBundle.Add(smBuilds);
        buildBundle.Add(psBuilds);
        buildBundle.Add(rmBuilds);

        return buildBundle;
    }
}
