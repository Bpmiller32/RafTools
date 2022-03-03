using System.Net.Sockets;

namespace Ui.Core.Data;

public class NetworkStreams
{
    public NetworkStream CrawlerStream { get; set; }
    public NetworkStream BuilderStream { get; set; }

    public SocketResponseBundle CrawlerResponse { get; set; }
    public SocketResponseBundle BuilderResponse { get; set; }
}
