using System.Collections;
using System.Net;
using System.Net.Sockets;
using Common.Data;

namespace Tester;

public class BackEndSockC
{
    public int ImageCount { get; set; }
    public int FinalCount { get; set; }

    private readonly Socket client;

    public BackEndSockC(string ipAddress)
    {
        IPEndPoint endPoint = new(IPAddress.Parse(ipAddress), 3011);
        client = new(SocketType.Stream, ProtocolType.Tcp);

        client.Connect(endPoint);
    }

    public async Task ExecuteAsync(CancellationTokenSource stoppingTokenSource)
    {
        CancellationToken stoppingToken = stoppingTokenSource.Token;

        try
        {
            while (true)
            {
                // Pull first 4 bytes to determine message length
                byte[] lengthBytes = new byte[4];
                await client.ReceiveAsync(lengthBytes, SocketFlags.None, stoppingToken);
                int messageLength = Utils.ConvertIntBytes(lengthBytes);

                // Define new buffer based on message size
                byte[] messageBytes = new byte[messageLength];
                await client.ReceiveAsync(messageBytes, SocketFlags.None, stoppingToken);
                int messageType = Utils.ConvertIntBytes(messageBytes[5..9]);

                if (messageType == 4001)
                {
                    ImageCount++;
                    if (GetFinal(messageBytes))
                    {
                        FinalCount++;
                    }
                }
            }
        }
        catch
        {
            client.Close();
        }
    }

    private static bool GetFinal(byte[] messageBytes)
    {
        int startIndex = 9;
        while (startIndex < messageBytes.Length)
        {
            DataSection dataSection = new(messageBytes, startIndex);
            startIndex += 8 + dataSection.SectionSize;

            if (dataSection.SectionNumber == 12)
            {
                BitArray bits = Utils.ConvertBitBytes(dataSection.SectionData);
                return bits[5];
            }
        }

        return false;
    }
}