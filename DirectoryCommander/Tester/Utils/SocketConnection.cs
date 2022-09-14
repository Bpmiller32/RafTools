using WebSocketSharp.Server;

public class SocketConnection : WebSocketBehavior
{
    public static WebSocketServiceManager SocketServer { get; set; }

    private readonly ILogger<SocketConnection> logger;

    private System.Net.IPAddress ipAddress;

    public SocketConnection(ILogger<SocketConnection> logger)
    {
        this.logger = logger;
    }
}
