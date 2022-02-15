using System.Net;
using System.Net.Sockets;
using System.Text;
using Builder.App.Utils;
using Newtonsoft.Json;

namespace Builder.App;

public class ServerManager : BackgroundService
{
    private readonly ILogger<ServerManager> logger;
    private readonly BuildManager buildManager;
    private readonly CacheManager cacheManager;

    public ServerManager(ILogger<ServerManager> logger, BuildManager buildManager, CacheManager cacheManager)
    {
        this.logger = logger;
        this.buildManager = buildManager;
        this.cacheManager = cacheManager;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Task.Run(cacheManager.RunTask);

        TcpListener server = new TcpListener(IPAddress.Parse("127.0.0.1"), 11000);
        server.Start();

        try
        {
            // Inside the try because the stop call will throw an error to catch
            using (stoppingToken.Register(() => server.Stop()))
            {
                // Blocking, wait for connection, could put in a using but not because server stop will handle it albeit less gracefully, thoughts: creating a TcpClient which you could do whatever with when it isn't == null a 2-way socket is technically established
                TcpClient connection = await server.AcceptTcpClientAsync();
                // Connection aquired, can start main loop, no reason to let go of this connection until app ends. Will change if more than one process needs to control Builder
                NetworkStream stream = connection.GetStream();

                while (true)
                {
                    SocketMessage message = GetMessage(stream);

                    if (message.CheckStatus)
                    {
                        SendMessage(stream);
                        continue;
                    }

                    buildManager.RunTask(message);
                }
            }
        }
        catch (System.Exception e)
        {
            // If caused by cancellationToken either cancellationToken was canceled before accepting (InvalidOperationExample), or after starting accepting (ObjectDisposedException)
            // Otherwise general error
            logger.LogError(e.Message);
        }
        finally
        {
            server.Stop();
            // Don't want to kill the application over the socket being closed?, a build may be in progress
        }
    }


    private SocketMessage GetMessage(NetworkStream stream)
    {
        byte[] buffer = new byte[256];

        // Blocking while stream.Read == null, will always be set to the length of the message if < buffer
        int bytesToRead = stream.Read(buffer, 0, buffer.Length);
        string data = Encoding.UTF8.GetString(buffer, 0, bytesToRead);

        SocketMessage message = JsonConvert.DeserializeObject<SocketMessage>(data);

        if (message == null)
        {
            throw new Exception("Empty message recieved from server/connection stream was closed");
        }

        return message;
    }

    private void SendMessage(NetworkStream stream)
    {
        // Create data message here
        StatusBundle SmartMatch = new StatusBundle()
        {
            Status = buildManager.SmBuild.Status,
            Progress = buildManager.SmBuild.Progress,
            AvailableBuilds = cacheManager.smBuilds
        };
        StatusBundle Parascript = new StatusBundle()
        {
            Status = buildManager.PsBuild.Status,
            Progress = buildManager.PsBuild.Progress,
            AvailableBuilds = cacheManager.psBuilds
        };
        StatusBundle RoyalMail = new StatusBundle()
        {
            Status = buildManager.PsBuild.Status,
            Progress = buildManager.PsBuild.Progress,
            AvailableBuilds = cacheManager.rmBuilds
        };

        string serializedObject = JsonConvert.SerializeObject(new {SmartMatch, Parascript, RoyalMail});
        byte[] data = System.Text.Encoding.UTF8.GetBytes(serializedObject);

        stream.Write(data);
    }
}