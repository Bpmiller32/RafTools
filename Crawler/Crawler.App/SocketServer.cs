using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Crawler.App
{
    public class SocketServer : BackgroundService
    {
        private readonly ILogger<SocketServer> logger;
        private readonly IConfiguration config;
        private readonly CrawlTask tasks;
        private readonly DatabaseContext context;

        public SocketServer(ILogger<SocketServer> logger, IConfiguration config, IServiceScopeFactory factory, CrawlTask tasks)
        {
            this.logger = logger;
            this.config = config;
            this.tasks = tasks;
            this.context = factory.CreateScope().ServiceProvider.GetRequiredService<DatabaseContext>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            context.Database.EnsureCreated();
            TcpListener server = new TcpListener(IPAddress.Parse("127.0.0.1"), 10022);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    server.Start();

                    TcpClient connection = await server.AcceptTcpClientAsync();
                    logger.LogInformation("Socket connection established");
                    NetworkStream stream = connection.GetStream();

                    // Report status every minute unless error
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        GetMessage(stream);
                        ReportStatus(stream);
                    }
                }
                catch (System.Exception e)
                {
                    logger.LogError(e.Message);
                }
                finally
                {
                    server.Stop();
                }
            }
        }

        private void GetMessage(NetworkStream stream)
        {
            byte[] buffer = new byte[256];

            // Blocking while stream.Read == null, will always be set to the length of the message if < buffer
            int bytesToRead = stream.Read(buffer, 0, buffer.Length);
            string data = Encoding.UTF8.GetString(buffer, 0, bytesToRead);

            SocketMessage message = JsonConvert.DeserializeObject<SocketMessage>(data);

            if (data == null)
            {
                throw new Exception("Empty message recieved from server/connection stream was closed");
            }
        }

        private void ReportStatus(NetworkStream stream)
        {
            List<List<string>> buildBundle = GetAvailableBuilds();

            List<UspsBundle> uspsBundles = context.UspsBundles.Where(x => (x.IsReadyForBuild == true)).ToList();
            List<ParaBundle> paraBundles = context.ParaBundles.Where(x => (x.IsReadyForBuild == true)).ToList();
            List<RoyalBundle> royalBundles = context.RoyalBundles.Where(x => (x.IsReadyForBuild == true)).ToList();

            SocketResponse SmartMatch = new SocketResponse() { Status = tasks.SmartMatch, AvailableBuilds = buildBundle[0] };
            SocketResponse Parascript = new SocketResponse() { Status = tasks.Parascript, AvailableBuilds = buildBundle[1] };
            SocketResponse RoyalMail = new SocketResponse() { Status = tasks.RoyalMail, AvailableBuilds = buildBundle[2] };

            string serializedObject = JsonConvert.SerializeObject(new { SmartMatch, Parascript, RoyalMail });
            byte[] data = System.Text.Encoding.UTF8.GetBytes(serializedObject);

            stream.Write(data);
        }

        private List<List<string>> GetAvailableBuilds()
        {
            List<string> smBuilds = new List<string>();
            List<string> psBuilds = new List<string>();
            List<string> rmBuilds = new List<string>();

            List<UspsBundle> uspsBundles = context.UspsBundles.Where(x => (x.IsReadyForBuild == true)).ToList();
            List<ParaBundle> paraBundles = context.ParaBundles.Where(x => (x.IsReadyForBuild == true)).ToList();
            List<RoyalBundle> royalBundles = context.RoyalBundles.Where(x => (x.IsReadyForBuild == true)).ToList();

            foreach (UspsBundle bundle in uspsBundles)
            {
                smBuilds.Add(bundle.DataYearMonth);
            }
            foreach (ParaBundle bundle in paraBundles)
            {
                string dataMonth;
                if (bundle.DataMonth < 10)
                {
                    dataMonth = "0" + bundle.DataMonth;
                }
                else
                {
                    dataMonth = bundle.DataMonth.ToString();
                }

                string dataYearMonth = bundle.DataYear.ToString() + dataMonth;
                psBuilds.Add(dataYearMonth);
            }
            foreach (RoyalBundle bundle in royalBundles)
            {
                string dataMonth;
                if (bundle.DataMonth < 10)
                {
                    dataMonth = "0" + bundle.DataMonth;
                }
                else
                {
                    dataMonth = bundle.DataMonth.ToString();
                }

                string dataYearMonth = bundle.DataYear.ToString() + dataMonth;
                rmBuilds.Add(dataYearMonth);
            }

            List<List<string>> buildBundle = new List<List<string>>();
            buildBundle.Add(smBuilds);
            buildBundle.Add(psBuilds);
            buildBundle.Add(rmBuilds);

            return buildBundle;
        }
    }
}
