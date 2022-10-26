using System.Text.Json;
using Common.Data;
using WebSocketSharp.NetCore;
using WebSocketSharp.NetCore.Server;

#pragma warning disable CS4014 // ignore that I'm not awaiting a task, by design 
#pragma warning disable CS1998 // ignore that I'm not awaiting a task, by design 

namespace Builder;

public class SocketConnection : WebSocketBehavior
{
    private readonly ILogger<SocketConnection> logger;
    private readonly ParaBuilder paraBuilder;
    private readonly RoyalBuilder royalBuilder;

    private CancellationTokenSource psTokenSource = new();
    private CancellationTokenSource rmTokenSource = new();

    private WebSocketSessionManager server;
    private System.Net.IPAddress ipAddress;

    public SocketConnection(ILogger<SocketConnection> logger, ParaBuilder paraBuilder, RoyalBuilder royalBuilder)
    {
        this.logger = logger;

        this.paraBuilder = paraBuilder;
        paraBuilder.SendMessage = SendMessage;

        this.royalBuilder = royalBuilder;
        royalBuilder.SendMessage = SendMessage;
    }

    protected override void OnOpen()
    {
        server = Sessions;

        ipAddress = Context.UserEndPoint.Address;
        logger.LogInformation("Connected to client: {ipAddress}, Total clients: {Count}", ipAddress, Sessions.Count);

        // Task.Run(() => SmartMatchCrawler.ExecuteAsyncAuto(smTokenSource.Token));
        Task.Run(() => paraBuilder.ExecuteAsyncAuto(psTokenSource.Token));
        Task.Run(() => royalBuilder.ExecuteAsyncAuto(rmTokenSource.Token));
    }

    protected override void OnClose(CloseEventArgs e)
    {
        logger.LogInformation("Connection closed: {ipAddress}, Total clients: {Count}", ipAddress, Sessions.Count);
    }

    protected override async void OnMessage(MessageEventArgs e)
    {
        SocketMessage message = JsonSerializer.Deserialize<SocketMessage>(e.Data);

        if (message.Directory == "Parascript")
        {
            psTokenSource.Cancel();
            psTokenSource = new CancellationTokenSource();

            if (message.Property == "Force")
            {
                await Task.Run(() => paraBuilder.ExecuteAsync(message.Value));
            }
            if (message.Property == "AutoEnabled")
            {
                paraBuilder.Settings.AutoBuildEnabled = bool.Parse(message.Value);
            }

            Task.Run(() => paraBuilder.ExecuteAsyncAuto(psTokenSource.Token));
        }
        if (message.Directory == "RoyalMail")
        {
            rmTokenSource.Cancel();
            rmTokenSource = new CancellationTokenSource();

            if (message.Property == "Force")
            {
                await Task.Run(() => royalBuilder.ExecuteAsync(message.Value));
            }
            if (message.Property == "AutoEnabled")
            {
                royalBuilder.Settings.AutoBuildEnabled = bool.Parse(message.Value);
            }

            Task.Run(() => royalBuilder.ExecuteAsyncAuto(psTokenSource.Token));
        }
    }

    public void SendMessage(DirectoryType directoryType, DatabaseContext context)
    {
        // Get available builds
        List<BuildInfo> smBuilds = new();
        List<BuildInfo> psBuilds = new();
        List<BuildInfo> rmBuilds = new();
        List<List<BuildInfo>> buildBundle = new();

        if (directoryType == DirectoryType.SmartMatch)
        {
            foreach (UspsBundle bundle in context.UspsBundles.Where(x => x.IsBuildComplete).OrderByDescending(x => x.DataYearMonth).ToList())
            {
                smBuilds.Add(new BuildInfo()
                {
                    Name = bundle.DataYearMonth,
                    Date = bundle.CompileDate,
                    Time = bundle.CompileTime,
                });
            }
        }
        if (directoryType == DirectoryType.Parascript)
        {
            foreach (ParaBundle bundle in context.ParaBundles.Where(x => x.IsBuildComplete).OrderByDescending(x => x.DataYearMonth).ToList())
            {
                psBuilds.Add(new BuildInfo()
                {
                    Name = bundle.DataYearMonth,
                    Date = bundle.CompileDate,
                    Time = bundle.CompileTime,
                });
            }
        }
        if (directoryType == DirectoryType.RoyalMail)
        {
            foreach (RoyalBundle bundle in context.RoyalBundles.Where(x => x.IsBuildComplete).OrderByDescending(x => x.DataYearMonth).ToList())
            {
                rmBuilds.Add(new BuildInfo()
                {
                    Name = bundle.DataYearMonth,
                    Date = bundle.CompileDate,
                    Time = bundle.CompileTime,
                });
            }
        }

        buildBundle.Add(smBuilds);
        buildBundle.Add(psBuilds);
        buildBundle.Add(rmBuilds);

        // Create SocketResponses
        Dictionary<ComponentStatus, string> statusMap = new() { { ComponentStatus.Ready, "Ready" }, { ComponentStatus.InProgress, "In Progress" }, { ComponentStatus.Error, "Error" }, { ComponentStatus.Disabled, "Disabled" } };
        string serializedObject = "";

        if (directoryType == DirectoryType.Parascript)
        {
            SocketResponse Parascript = new()
            {
                DirectoryStatus = statusMap[paraBuilder.Status],
                AutoEnabled = paraBuilder.Settings.AutoBuildEnabled,
                AutoDate = paraBuilder.Settings.ExecMonth + "/" + paraBuilder.Settings.ExecDay + "/" + paraBuilder.Settings.ExecYear,
                CurrentBuild = paraBuilder.Settings.DataYearMonth,
                Progress = paraBuilder.Progress,
                CompiledBuilds = buildBundle[1]
            };

            serializedObject = JsonSerializer.Serialize(new { Parascript });
        }
        if (directoryType == DirectoryType.RoyalMail)
        {
            SocketResponse RoyalMail = new()
            {
                DirectoryStatus = statusMap[royalBuilder.Status],
                AutoEnabled = royalBuilder.Settings.AutoBuildEnabled,
                AutoDate = royalBuilder.Settings.ExecMonth + "/" + royalBuilder.Settings.ExecDay + "/" + royalBuilder.Settings.ExecYear,
                CurrentBuild = royalBuilder.Settings.DataYearMonth,
                Progress = royalBuilder.Progress,
                CompiledBuilds = buildBundle[2]
            };

            serializedObject = JsonSerializer.Serialize(new { RoyalMail });
        }

        server.Broadcast(serializedObject);
    }
}