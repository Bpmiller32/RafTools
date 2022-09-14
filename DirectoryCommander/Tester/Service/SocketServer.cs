using WebSocketSharp.Server;

public class SocketServer : BackgroundService
{
    private readonly ILogger<SocketServer> logger;
    private readonly IServiceScopeFactory factory;
    private readonly ParaTester paraTester;

    public SocketServer(ILogger<SocketServer> logger, IServiceScopeFactory factory, ParaTester paraTester)
    {
        this.logger = logger;
        this.factory = factory;
        this.paraTester = paraTester;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        WebSocketServer server = new(10023);
        server.Log.Output = (logdata, _) => logger.LogError(logdata.Message);

        SocketConnection.SocketServer = server.WebSocketServices;
        SocketConnection.ParaTester = paraTester;

        server.AddWebSocketService("/", () => factory.CreateScope().ServiceProvider.GetRequiredService<SocketConnection>());

        try
        {
            server.Start();
            logger.LogInformation("Listening for client connections");
        }
        catch (System.Exception e)
        {
            logger.LogError(e.Message);
        }

        return Task.CompletedTask;
    }
}
