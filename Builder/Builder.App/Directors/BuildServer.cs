using System.Net;
using System.Net.Sockets;
using System.Security.Principal;
using System.Text;
using Builder.App.Builders;
using Builder.App.Utils;
using Microsoft.Extensions.Options;

namespace Builder.App;

public class BuildServer : BackgroundService
{
    private readonly ILogger<BuildServer> logger;
    private readonly BuildManager buildManager;

    public BuildServer(ILogger<BuildServer> logger, BuildManager buildManager)
    {
        this.logger = logger;
        this.buildManager = buildManager;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            string data = "";
            data = await SocketServer(stoppingToken);
            logger.LogInformation("Message recieved: " + data);
            
            buildManager.RunTask(data);
        }
    }

    private async Task<string> SocketServer(CancellationToken stoppingToken)
    {
        TcpListener server = new TcpListener(IPAddress.Parse("127.0.0.1"), 11000);
        string data = "";

        // Listen for and pull a packet of data
        using (stoppingToken.Register(() => server.Stop()))
        {
            try
            {
                server.Start();

                logger.LogDebug("Waiting for new connection...");
                using (TcpClient connection = await server.AcceptTcpClientAsync())
                {
                    NetworkStream stream = connection.GetStream();

                    while (true)
                    {
                        byte[] buffersize = new byte[100];
                        int numberOfBytesRecieved = stream.Read(buffersize, 0, buffersize.Length);
                        data += Encoding.UTF8.GetString(buffersize, 0, numberOfBytesRecieved);

                        if (data.Contains("<EOF>"))
                        {
                            break;
                        }
                    }
                }
            }
            catch (System.Exception)
            {
                // Either cancellationToken was canceled before accepting (InvalidOperationExample), or after starting accepting (ObjectDisposedException)
                logger.LogInformation("Server shut down while waiting for new connection");
            }
            finally
            {
                server.Stop();
            }
        }

        if (string.IsNullOrEmpty(data))
        {
            throw new Exception("Empty message recieved from server");
        }

        return data;
    }
}