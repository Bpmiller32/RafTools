using WebSocketSharp.Server;

public class SocketServer : BackgroundService
{
    private readonly ILogger<SocketServer> logger;
    private readonly IServiceScopeFactory factory;

    public SocketServer(ILogger<SocketServer> logger, IServiceScopeFactory factory)
    {
        this.logger = logger;
        this.factory = factory;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        WebSocketServer server = new WebSocketServer(10023);
        server.Log.Output = (logdata, _) => logger.LogError(logdata.Message);

        SocketConnection.SocketServer = server.WebSocketServices;

        server.AddWebSocketService<SocketConnection>("/", () =>
        {
            SocketConnection connection = factory.CreateScope().ServiceProvider.GetRequiredService<SocketConnection>();
            return connection;
        });

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
