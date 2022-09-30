using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebSocketSharp.NetCore.Server;

namespace Common.Data;

public class SocketServer : BackgroundService
{
    public WebSocketServer Server { get; set; }
    public IServiceScopeFactory Factory { get; set; }

    private readonly ILogger<SocketServer> logger;

    public SocketServer(ILogger<SocketServer> logger)
    {
        this.logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Server.Log.Output = (logdata, _) => logger.LogError("{Message}", logdata.Message);
        Server.Start();

        logger.LogInformation("Listening for client connections");

        return Task.CompletedTask;
    }
}
