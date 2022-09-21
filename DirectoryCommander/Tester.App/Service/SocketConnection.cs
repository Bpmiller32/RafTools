using System.Text.Json;
using Common.Data;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Tester
{
    public class SocketConnection : WebSocketBehavior
    {
        public static WebSocketServiceManager SocketServer { get; set; }
        public static SmartTester SmartTester { get; set; }
        public static ParaTester ParaTester { get; set; }
        public static RoyalTester RoyalTester { get; set; }

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
                SmartTester.Status = ComponentStatus.Disabled;
                RoyalTester.Status = ComponentStatus.Disabled;

                if (message.Property == "Force")
                {
                    await Task.Run(() => ParaTester.ExecuteAsync());
                }
            }
            if (message.Directory == "Parascript")
            {
                SmartTester.Status = ComponentStatus.Disabled;
                RoyalTester.Status = ComponentStatus.Disabled;

                if (message.Property == "Force")
                {
                    await Task.Run(() => ParaTester.ExecuteAsync());
                }
            }
            if (message.Directory == "RoyalMail")
            {
                SmartTester.Status = ComponentStatus.Disabled;
                RoyalTester.Status = ComponentStatus.Disabled;

                if (message.Property == "Force")
                {
                    await Task.Run(() => ParaTester.ExecuteAsync());
                }
            }
        }

        public static void SendMessage(DirectoryType directoryType)
        {
            Dictionary<ComponentStatus, string> statusMap = new() { { ComponentStatus.Ready, "Ready" }, { ComponentStatus.InProgress, "In Progress" }, { ComponentStatus.Error, "Error" }, { ComponentStatus.Disabled, "Disabled" } };
            string serializedObject = "";

            if (directoryType == DirectoryType.SmartMatch)
            {
                SocketResponse SmartMatch = new()
                {
                    DirectoryStatus = statusMap[SmartTester.Status],
                    Progress = SmartTester.Progress,
                };

                serializedObject = JsonSerializer.Serialize(new { SmartMatch });

                if (SmartTester.Progress > 99 && SmartTester.Status == ComponentStatus.Ready)
                {
                    ParaTester.Status = ComponentStatus.Ready;
                    RoyalTester.Status = ComponentStatus.Ready;
                }
            }
            if (directoryType == DirectoryType.Parascript)
            {
                SocketResponse Parascript = new()
                {
                    DirectoryStatus = statusMap[ParaTester.Status],
                    Progress = ParaTester.Progress,
                };

                serializedObject = JsonSerializer.Serialize(new { Parascript });

                if (ParaTester.Progress > 99 && ParaTester.Status == ComponentStatus.Ready)
                {
                    SmartTester.Status = ComponentStatus.Ready;
                    RoyalTester.Status = ComponentStatus.Ready;
                }
            }
            if (directoryType == DirectoryType.RoyalMail)
            {
                SocketResponse RoyalMail = new()
                {
                    DirectoryStatus = statusMap[RoyalTester.Status],
                    Progress = RoyalTester.Progress,
                };

                serializedObject = JsonSerializer.Serialize(new { RoyalMail });

                if (RoyalTester.Progress > 99 && RoyalTester.Status == ComponentStatus.Ready)
                {
                    SmartTester.Status = ComponentStatus.Ready;
                    ParaTester.Status = ComponentStatus.Ready;
                }
            }

            SocketServer.Broadcast(serializedObject);
        }
    }
}