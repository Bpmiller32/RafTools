using System.Text.Json;
using Common.Data;
using WebSocketSharp.NetCore;
using WebSocketSharp.NetCore.Server;

namespace Tester;

public class SocketConnection : WebSocketBehavior
{
    private readonly ILogger<SocketConnection> logger;

    private static SmartTester smartTester;
    private static ParaTester paraTester;
    private static RoyalTester royalTester;
    private static ZipTester zipTester;

    private static WebSocketSessionManager server;
    private System.Net.IPAddress ipAddress;

    public SocketConnection(ILogger<SocketConnection> logger, SmartTester smartTester, ParaTester paraTester, RoyalTester royalTester, ZipTester zipTester)
    {
        this.logger = logger;

        SocketConnection.smartTester = smartTester;
        SocketConnection.paraTester = paraTester;
        SocketConnection.royalTester = royalTester;
        SocketConnection.zipTester = zipTester;
    }

    protected override void OnOpen()
    {
        server = Sessions;

        ipAddress = Context.UserEndPoint.Address;
        logger.LogInformation("Connected to client: {ipAddress}, Total clients: {Count}", ipAddress, Sessions.Count);

        SendMessage();
    }

    protected override void OnClose(CloseEventArgs e)
    {
        logger.LogInformation("Connection closed: {ipAddress}, Total clients: {Count}", ipAddress, Sessions.Count);
    }

    protected override async void OnMessage(MessageEventArgs e)
    {
        SocketMessage message = JsonSerializer.Deserialize<SocketMessage>(e.Data);

        if (message.Directory == "SmartMatch")
        {
            paraTester.Status = ComponentStatus.Disabled;
            royalTester.Status = ComponentStatus.Disabled;
            zipTester.Status = ComponentStatus.Disabled;

            if (message.Property == "Force")
            {
                await Task.Run(() => smartTester.ExecuteAsync());
            }
        }
        if (message.Directory == "Parascript")
        {
            smartTester.Status = ComponentStatus.Disabled;
            royalTester.Status = ComponentStatus.Disabled;
            zipTester.Status = ComponentStatus.Disabled;

            if (message.Property == "Force")
            {
                await Task.Run(() => paraTester.ExecuteAsync());
            }
        }
        if (message.Directory == "RoyalMail")
        {
            smartTester.Status = ComponentStatus.Disabled;
            paraTester.Status = ComponentStatus.Disabled;
            zipTester.Status = ComponentStatus.Disabled;

            if (message.Property == "Force")
            {
                await Task.Run(() => royalTester.ExecuteAsync());
            }
        }
        if (message.Directory == "Zip4")
        {
            smartTester.Status = ComponentStatus.Disabled;
            paraTester.Status = ComponentStatus.Disabled;
            royalTester.Status = ComponentStatus.Disabled;

            if (message.Property == "Force")
            {
                await Task.Run(() => zipTester.Execute());
            }
        }
    }

    public static void SendMessage()
    {
        Dictionary<ComponentStatus, string> statusMap = new() { { ComponentStatus.Ready, "Ready" }, { ComponentStatus.InProgress, "In Progress" }, { ComponentStatus.Error, "Error" }, { ComponentStatus.Disabled, "Disabled" } };

        if (smartTester.Progress > 5 && smartTester.Status == ComponentStatus.Ready)
        {
            paraTester.Status = ComponentStatus.Ready;
            royalTester.Status = ComponentStatus.Ready;
            zipTester.Status = ComponentStatus.Ready;
        }
        if (paraTester.Progress > 5 && paraTester.Status == ComponentStatus.Ready)
        {
            smartTester.Status = ComponentStatus.Ready;
            royalTester.Status = ComponentStatus.Ready;
            zipTester.Status = ComponentStatus.Ready;
        }
        if (royalTester.Progress > 5 && royalTester.Status == ComponentStatus.Ready)
        {
            smartTester.Status = ComponentStatus.Ready;
            paraTester.Status = ComponentStatus.Ready;
            zipTester.Status = ComponentStatus.Ready;
        }
        if (zipTester.Progress > 5 && zipTester.Status == ComponentStatus.Ready)
        {
            smartTester.Status = ComponentStatus.Ready;
            paraTester.Status = ComponentStatus.Ready;
            royalTester.Status = ComponentStatus.Ready;
        }

        SocketResponse SmartMatch = new()
        {
            DirectoryStatus = statusMap[smartTester.Status],
            Progress = smartTester.Progress,
        };
        SocketResponse Parascript = new()
        {
            DirectoryStatus = statusMap[paraTester.Status],
            Progress = paraTester.Progress,
        };
        SocketResponse RoyalMail = new()
        {
            DirectoryStatus = statusMap[royalTester.Status],
            Progress = royalTester.Progress,
        };
        SocketResponse Zip4 = new()
        {
            DirectoryStatus = statusMap[zipTester.Status],
            Progress = zipTester.Progress,
        };

        string serializedObject = JsonSerializer.Serialize(new { SmartMatch, Parascript, RoyalMail, Zip4 });
        server.Broadcast(serializedObject);
    }
}