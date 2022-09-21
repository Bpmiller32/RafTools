using Common.Data;
using WebSocketSharp.Server;

public class SocketServer : BackgroundService
{
    private readonly ILogger<SocketServer> logger;
    private readonly DatabaseContext context;
    private readonly IServiceScopeFactory factory;

    private readonly ParaBuilder paraBuilder;
    private readonly RoyalBuilder royalBuilder;

    public SocketServer(ILogger<SocketServer> logger, DatabaseContext context, IServiceScopeFactory factory, ParaBuilder paraBuilder, RoyalBuilder royalBuilder)
    {
        this.logger = logger;
        this.context = context;
        this.factory = factory;

        this.paraBuilder = paraBuilder;
        this.royalBuilder = royalBuilder;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        context.Database.EnsureCreated();

        WebSocketServer server = new WebSocketServer(10022);
        server.Log.Output = (logdata, str) => { logger.LogError(logdata.Message); };

        SocketConnection.SocketServer = server.WebSocketServices;
        SocketConnection.ParaBuilder = paraBuilder;
        SocketConnection.RoyalBuilder = royalBuilder;

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