using System.Threading;
using System.Threading.Tasks;
using Common.Data;
using Crawler.App.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebSocketSharp.Server;

namespace Crawler.App
{
    public class SocketServer : BackgroundService
    {
        private readonly ILogger<SocketServer> logger;
        private readonly ComponentTask tasks;
        private readonly DatabaseContext context;

        public SocketServer(ILogger<SocketServer> logger, IServiceScopeFactory factory, ComponentTask tasks)
        {
            this.logger = logger;
            this.tasks = tasks;
            this.context = factory.CreateScope().ServiceProvider.GetRequiredService<DatabaseContext>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            context.Database.EnsureCreated();

            // WebSocketServer server = new WebSocketServer(IPAddress.Parse("127.0.0.1"), 10022);
            WebSocketServer server = new WebSocketServer(10022);
            server.Log.Output = (logdata, str) => { logger.LogError(logdata.Message); };

            // server.AddWebSocketService<WsEcho>("/Echo", () => new WsEcho());
            server.AddWebSocketService<SocketConnection>("/", () => new SocketConnection(logger, tasks, context));
            SocketConnection.SocketServer = server.WebSocketServices;

            try
            {
                server.Start();
                logger.LogInformation("Listening for client connection");
            }
            catch (System.Exception e)
            {
                logger.LogError(e.Message);
            }

            return Task.CompletedTask;
        }
    }
}
