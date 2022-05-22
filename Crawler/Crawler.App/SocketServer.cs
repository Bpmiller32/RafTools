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
        private readonly DatabaseContext context;
        private readonly IServiceScopeFactory factory;
        private readonly EmailCrawler emailCrawler;
        private readonly SmartmatchCrawler smartmatchCrawler;
        private readonly ParascriptCrawler parascriptCrawler;
        private readonly RoyalCrawler royalCrawler;

        public SocketServer(ILogger<SocketServer> logger, DatabaseContext context, IServiceScopeFactory factory, EmailCrawler emailCrawler, SmartmatchCrawler smartmatchCrawler, ParascriptCrawler parascriptCrawler, RoyalCrawler royalCrawler)
        {
            this.logger = logger;
            this.context = context;
            this.factory = factory;

            this.emailCrawler = emailCrawler;
            this.smartmatchCrawler = smartmatchCrawler;
            this.parascriptCrawler = parascriptCrawler;
            this.royalCrawler = royalCrawler;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            context.Database.EnsureCreated();

            WebSocketServer server = new WebSocketServer(10022);
            server.Log.Output = (logdata, str) => { logger.LogError(logdata.Message); };

            SocketConnection.SocketServer = server.WebSocketServices;
            SocketConnection.EmailCrawler = emailCrawler;
            SocketConnection.SmartMatchCrawler = smartmatchCrawler;
            SocketConnection.ParascriptCrawler = parascriptCrawler;
            SocketConnection.RoyalCrawler = royalCrawler;

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
}
