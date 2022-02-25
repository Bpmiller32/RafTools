using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using Ui.Core.Data;

namespace Ui.Core.Services;

public class SocketServer : BackgroundService
{
    private readonly ILogger<SocketServer> logger;
    private readonly NetworkStreams networkStream;

    public SocketServer(ILogger<SocketServer> logger, NetworkStreams networkStream)
    {
        this.logger = logger;
        this.networkStream = networkStream;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Attempt socket connection to Crawler
        try
        {
            TcpClient crawlerClient = new TcpClient();
            crawlerClient.ConnectAsync("127.0.0.1", 10022).Wait(TimeSpan.FromSeconds(5));
            logger.LogInformation("Socket connection established: Crawler");

            networkStream.CrawlerStream = crawlerClient.GetStream();
        }
        catch (System.Exception e)
        {
            logger.LogError("Error connecting to Crawler: " + e.Message);
        }
        
        // Attempt socket connection to Builder
        try
        {
            TcpClient builderClient = new TcpClient();
            builderClient.ConnectAsync("127.0.0.1", 10021).Wait(TimeSpan.FromSeconds(5));
            logger.LogInformation("Socket connection established: Builder");

            networkStream.BuilderStream = builderClient.GetStream();
        }
        catch (System.Exception e)
        {
            logger.LogError("Error connecting to Builder: " + e.Message);
        }

        // Main loop
        try
        {
            while (true)
            {
                if (networkStream.CrawlerStream != null)
                {
                    networkStream.CrawlerResponse = GetCrawlerStatus();
                }
                if (networkStream.BuilderStream != null)
                {
                    networkStream.BuilderResponse = GetBuilderStatus();                
                }

                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }
        catch (System.Exception e)
        {
            logger.LogError(e.Message);    
        }
    }

    private SocketResponseBundle GetBuilderStatus()
    {
        // Send message
        SocketMessage message = new SocketMessage()
        {
            CheckStatus = true
        };

        string messageSerialized = JsonConvert.SerializeObject(message);
        byte[] messageBytes = Encoding.UTF8.GetBytes(messageSerialized);

        networkStream.BuilderStream.Write(messageBytes);


        // Recieve response
        byte[] buffer = new byte[256];

        int responseBytes = networkStream.BuilderStream.Read(buffer, 0, buffer.Length);
        string responseSerialized = Encoding.UTF8.GetString(buffer, 0, responseBytes);

        SocketResponseBundle bundle = JsonConvert.DeserializeObject<SocketResponseBundle>(responseSerialized);

        return bundle;
    }

    private SocketResponse GetCrawlerStatus()
    {
        // Send message
        byte[] messageBytes = Encoding.UTF8.GetBytes("Status");

        networkStream.CrawlerStream.Write(messageBytes);


        // Recieve response
        byte[] buffer = new byte[256];

        int responseBytes = networkStream.CrawlerStream.Read(buffer, 0, buffer.Length);
        string responseSerialized = Encoding.UTF8.GetString(buffer, 0, responseBytes);

        SocketResponse response = JsonConvert.DeserializeObject<SocketResponse>(responseSerialized);

        return response;
    }
}
