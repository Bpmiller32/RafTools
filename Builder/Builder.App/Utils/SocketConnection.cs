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

    private readonly ILogger<SocketConnection> logger;
    private readonly IConfiguration config;
    private readonly ComponentTask tasks;
    private readonly DatabaseContext context;

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

        // Task.Run(() => SmartMatchCrawler.ExecuteAsyncAuto(smTokenSource.Token));
        // Task.Run(() => ParascriptCrawler.ExecuteAsyncAuto(psTokenSource.Token));
        // Task.Run(() => RoyalCrawler.ExecuteAsyncAuto(rmTokenSource.Token));
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
                // await Task.Run(() => ParascriptCrawler.ExecuteAsync(psTokenSource.Token));
                await Task.Run(() => ParaBuilder.ExecuteAsync(message.Value, psTokenSource.Token));
            }
            if (message.Property == "AutoEnabled")
            {
                // ParascriptCrawler.Settings.AutoCrawlEnabled = bool.Parse(message.Value);
            }
            if (message.Property == "AutoDate")
            {
                // string[] newDay = message.Value.Split('/');
                // ParascriptCrawler.Settings.ExecMonth = int.Parse(newDay[0]);
                // ParascriptCrawler.Settings.ExecDay = int.Parse(newDay[1]);
                // ParascriptCrawler.Settings.ExecYear = int.Parse(newDay[2]);
            }

            // Task.Run(() => ParascriptCrawler.ExecuteAsyncAuto(psTokenSource.Token));
        }
    }

    public void SendMessage(bool smartMatch = false, bool parascript = false, bool royalMail = false)
    {
        // string data = ReportStatus(smartMatch, parascript, royalMail);
        // SocketServer.Broadcast(data);
    }
}
