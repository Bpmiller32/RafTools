using System.Net;
using System.Net.Sockets;
using System.Text;
using Builder.App.Utils;
using Common.Data;
using Newtonsoft.Json;

namespace Builder.App;

public class ServerManager : BackgroundService
{
    private readonly ILogger<ServerManager> logger;
    private readonly BuildManager buildManager;
    private readonly DatabaseContext context;

    public ServerManager(ILogger<ServerManager> logger, BuildManager buildManager, IServiceScopeFactory factory)
    {
        this.logger = logger;
        this.buildManager = buildManager;
        this.context = factory.CreateScope().ServiceProvider.GetRequiredService<DatabaseContext>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TcpListener server = new TcpListener(IPAddress.Parse("127.0.0.1"), 10021);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                server.Start();

                // Inside the try because the stop call will throw an error to catch
                using (stoppingToken.Register(() => server.Stop()))
                {
                    // Blocking, wait for connection, could put in a using but not because server stop will handle it albeit less gracefully, thoughts: creating a TcpClient which you could do whatever with when it isn't == null a 2-way socket is technically established
                    TcpClient connection = await server.AcceptTcpClientAsync();
                    logger.LogInformation("Socket connection established");

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
        List<List<string>> buildBundle = GetAvailableBuilds();

        // Create data message here
        SocketResponse SmartMatch = new SocketResponse()
        {
            Status = buildManager.SmBuild.Status,
            Progress = buildManager.SmBuild.Progress,
            AvailableBuilds = buildBundle[0],
            CurrentBuild = buildManager.SmBuild.CurrentBuild
        };
        SocketResponse Parascript = new SocketResponse()
        {
            Status = buildManager.PsBuild.Status,
            Progress = buildManager.PsBuild.Progress,
            AvailableBuilds = buildBundle[1],
            CurrentBuild = buildManager.PsBuild.CurrentBuild
        };
        SocketResponse RoyalMail = new SocketResponse()
        {
            Status = buildManager.RmBuild.Status,
            Progress = buildManager.RmBuild.Progress,
            AvailableBuilds = buildBundle[2],
            CurrentBuild = buildManager.RmBuild.CurrentBuild
        };

        string serializedObject = JsonConvert.SerializeObject(new { SmartMatch, Parascript, RoyalMail });
        byte[] data = System.Text.Encoding.UTF8.GetBytes(serializedObject);

        stream.Write(data);
    }

    private List<List<string>> GetAvailableBuilds()
    {
        List<string> smBuilds = new List<string>();
        List<string> psBuilds = new List<string>();
        List<string> rmBuilds = new List<string>();

        List<UspsBundle> uspsBundles = context.UspsBundles.Where(x => (x.IsBuildComplete == true)).ToList();
        List<ParaBundle> paraBundles = context.ParaBundles.Where(x => (x.IsBuildComplete == true)).ToList();
        List<RoyalBundle> royalBundles = context.RoyalBundles.Where(x => (x.IsBuildComplete == true)).ToList();

        foreach (UspsBundle bundle in uspsBundles)
        {
            smBuilds.Add(bundle.DataYearMonth);
        }
        foreach (ParaBundle bundle in paraBundles)
        {
            psBuilds.Add(bundle.DataYearMonth);
        }
        foreach (RoyalBundle bundle in royalBundles)
        {
            rmBuilds.Add(bundle.DataYearMonth);
        }

        List<List<string>> buildBundle = new List<List<string>>();
        buildBundle.Add(smBuilds);
        buildBundle.Add(psBuilds);
        buildBundle.Add(rmBuilds);

        return buildBundle;
    }
}