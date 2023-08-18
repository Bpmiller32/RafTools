using System.Net;
using System.Net.Sockets;

namespace Server.Tester;

public class ControlPortMessage
{

}

public class ControlPort
{
    private readonly Socket client;

    public ControlPort(string ipAddress, int port)
    {
        IPEndPoint endPoint = new(IPAddress.Parse(ipAddress), port);
        client = new(SocketType.Stream, ProtocolType.Tcp);

        client.Connect(endPoint);
    }

    public void SendMessage()
    {
        byte[] bytes = new byte[2]
        {
            0,
            0
        };
        client.Send(bytes);
    }
}
