using Common.Data;
using WebSocketSharp;
using WebSocketSharp.Server;

public class SocketConnection : WebSocketBehavior
{
    public static WebSocketServiceManager SocketServer { get; set; }
    public static ParaTester ParaTester { get; set; }

    private readonly ILogger<SocketConnection> logger;

    private System.Net.IPAddress ipAddress;

    public SocketConnection(ILogger<SocketConnection> logger)
    {
        this.logger = logger;
    }

    protected override void OnOpen()
    {
        ipAddress = Context.UserEndPoint.Address;
        logger.LogInformation("Connected to client: {ipAddress}, Total clients: {Count}", ipAddress, Sessions.Count);

        Task.Run(() => ParaTester.ExecuteAsync());
    }

    protected override void OnClose(CloseEventArgs e)
    {
        logger.LogInformation("Connection closed: {ipAddress}, Total clients: {Count}", ipAddress, Sessions.Count);
    }

    public void SendMessage(DirectoryType directoryType)
    {
    }
}
