using WebSocketSharp.Server;

namespace Tester
{
    public class SocketServer : BackgroundService
    {
        private readonly ILogger<SocketServer> logger;
        private readonly IServiceScopeFactory factory;
        private readonly SmartTester smartTester;
        private readonly ParaTester paraTester;
        private readonly RoyalTester royalTester;

        public SocketServer(ILogger<SocketServer> logger, IServiceScopeFactory factory, SmartTester smartTester, ParaTester paraTester, RoyalTester royalTester)
        {
            this.logger = logger;
            this.factory = factory;
            this.smartTester = smartTester;
            this.paraTester = paraTester;
            this.royalTester = royalTester;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            WebSocketServer server = new(10023);
            server.Log.Output = (logdata, _) => logger.LogError("{Message}", logdata.Message);

            SocketConnection.SocketServer = server.WebSocketServices;
            SocketConnection.SmartTester = smartTester;
            SocketConnection.ParaTester = paraTester;
            SocketConnection.RoyalTester = royalTester;

            server.AddWebSocketService("/", () => factory.CreateScope().ServiceProvider.GetRequiredService<SocketConnection>());

            try
            {
                server.Start();
                logger.LogInformation("Listening for client connections");
            }
            catch (System.Exception e)
            {
                logger.LogError("{Message}", e.Message);
            }

            return Task.CompletedTask;
        }
    }
}