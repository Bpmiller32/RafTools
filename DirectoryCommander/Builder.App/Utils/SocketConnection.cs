using System.Text.Json;
using Common.Data;
using WebSocketSharp;
using WebSocketSharp.Server;

#pragma warning disable CS4014 // ignore that I'm not awaiting a task, by design 
#pragma warning disable CS1998 // ignore that I'm not awaiting a task, by design 

public class SocketConnection : WebSocketBehavior
{
    public static WebSocketServiceManager SocketServer { get; set; }
    public static ParaBuilder ParaBuilder { get; set; }
    public static RoyalBuilder RoyalBuilder { get; set; }

    private readonly ILogger<SocketConnection> logger;
    private readonly DatabaseContext context;

    private CancellationTokenSource smTokenSource = new CancellationTokenSource();
    private CancellationTokenSource psTokenSource = new CancellationTokenSource();
    private CancellationTokenSource rmTokenSource = new CancellationTokenSource();
    private System.Net.IPAddress ipAddress;

    public SocketConnection(ILogger<SocketConnection> logger, DatabaseContext context)
    {
        this.logger = logger;
        this.context = context;
    }

    protected override void OnOpen()
    {
        ipAddress = Context.UserEndPoint.Address;
        logger.LogInformation("Connected to client: {0}, Total clients: {1}", ipAddress, Sessions.Count);

        // Task.Run(() => SmartMatchCrawler.ExecuteAsyncAuto(smTokenSource.Token));
        Task.Run(() => ParaBuilder.ExecuteAsyncAuto(psTokenSource.Token));
        Task.Run(() => RoyalBuilder.ExecuteAsyncAuto(rmTokenSource.Token));
    }

    protected override void OnClose(CloseEventArgs e)
    {
        logger.LogInformation("Connection closed: {0}, Total clients: {1}", ipAddress, Sessions.Count);
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
                await Task.Run(() => ParaBuilder.ExecuteAsync(message.Value, psTokenSource.Token));
            }
            if (message.Property == "AutoEnabled")
            {
                ParaBuilder.Settings.AutoBuildEnabled = bool.Parse(message.Value);
            }

            Task.Run(() => ParaBuilder.ExecuteAsyncAuto(psTokenSource.Token));
        }
        if (message.Directory == "RoyalMail")
        {
            rmTokenSource.Cancel();
            rmTokenSource = new CancellationTokenSource();

            if (message.Property == "Force")
            {
                await Task.Run(() => RoyalBuilder.ExecuteAsync(message.Value, rmTokenSource.Token));
            }
            if (message.Property == "AutoEnabled")
            {
                RoyalBuilder.Settings.AutoBuildEnabled = bool.Parse(message.Value);
            }

            Task.Run(() => RoyalBuilder.ExecuteAsyncAuto(psTokenSource.Token));
        }
    }

    public void SendMessage(DirectoryType directoryType)
    {
        Dictionary<ComponentStatus, string> statusMap = new() { { ComponentStatus.Ready, "Ready" }, { ComponentStatus.InProgress, "In Progress" }, { ComponentStatus.Error, "Error" }, { ComponentStatus.Disabled, "Disabled" } };
        string serializedObject = "";

        if (directoryType == DirectoryType.Parascript)
        {
            List<List<BuildInfo>> buildBundle = GetCompiledBuilds(parascript: true);

            SocketResponse Parascript = new SocketResponse()
            {
                DirectoryStatus = statusMap[ParaBuilder.Status],
                AutoEnabled = ParaBuilder.Settings.AutoBuildEnabled,
                AutoDate = ParaBuilder.Settings.ExecMonth + "/" + ParaBuilder.Settings.ExecDay + "/" + ParaBuilder.Settings.ExecYear,
                CurrentBuild = ParaBuilder.Settings.DataYearMonth,
                Progress = ParaBuilder.Progress,
                CompiledBuilds = buildBundle[1]
            };

            serializedObject = JsonSerializer.Serialize(new { Parascript });
        }
        if (directoryType == DirectoryType.RoyalMail)
        {
            List<List<BuildInfo>> buildBundle = GetCompiledBuilds(royalMail: true);

            SocketResponse RoyalMail = new SocketResponse()
            {
                DirectoryStatus = statusMap[RoyalBuilder.Status],
                AutoEnabled = RoyalBuilder.Settings.AutoBuildEnabled,
                AutoDate = RoyalBuilder.Settings.ExecMonth + "/" + RoyalBuilder.Settings.ExecDay + "/" + RoyalBuilder.Settings.ExecYear,
                CurrentBuild = RoyalBuilder.Settings.DataYearMonth,
                Progress = RoyalBuilder.Progress,
                CompiledBuilds = buildBundle[2]
            };

            serializedObject = JsonSerializer.Serialize(new { RoyalMail });
        }

        SocketServer.Broadcast(serializedObject);
    }


    private List<List<BuildInfo>> GetCompiledBuilds(bool smartMatch = false, bool parascript = false, bool royalMail = false)
    {
        List<BuildInfo> smBuilds = new List<BuildInfo>();
        List<BuildInfo> psBuilds = new List<BuildInfo>();
        List<BuildInfo> rmBuilds = new List<BuildInfo>();
        List<List<BuildInfo>> buildBundle = new List<List<BuildInfo>>();

        if (smartMatch)
        {
            List<UspsBundle> uspsBundles = context.UspsBundles.Where(x => (x.IsBuildComplete == true)).OrderByDescending(x => x.DataYearMonth).ToList();
            foreach (UspsBundle bundle in uspsBundles)
            {
                smBuilds.Add(new BuildInfo()
                {
                    Name = bundle.DataYearMonth,
                    Date = bundle.CompileDate,
                    Time = bundle.CompileTime,
                });
            }
        }
        if (parascript)
        {
            List<ParaBundle> paraBundles = context.ParaBundles.Where(x => (x.IsBuildComplete == true)).OrderByDescending(x => x.DataYearMonth).ToList();
            foreach (ParaBundle bundle in paraBundles)
            {
                psBuilds.Add(new BuildInfo()
                {
                    Name = bundle.DataYearMonth,
                    Date = bundle.CompileDate,
                    Time = bundle.CompileTime,
                });
            }
        }
        if (royalMail)
        {
            List<RoyalBundle> royalBundles = context.RoyalBundles.Where(x => (x.IsBuildComplete == true)).OrderByDescending(x => x.DataYearMonth).ToList();
            foreach (RoyalBundle bundle in royalBundles)
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

        return buildBundle;
    }
}
