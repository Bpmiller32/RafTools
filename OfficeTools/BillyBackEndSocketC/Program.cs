using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Namespace
{
    internal static class Program
    {
        private static void Main()
        {
            IPEndPoint endPoint = new(IPAddress.Parse("172.27.23.57"), 3011);
            Socket client = new(SocketType.Stream, ProtocolType.Tcp);

            client.Connect(endPoint);

            Console.WriteLine("Connected to Back End Socket C Interface");

            while (true)
            {
                // Pull first 4 bytes to determine message length
                byte[] lengthBytes = new byte[4];
                client.Receive(lengthBytes);
                int messageLength = ByteConverter.ConvertIntBytes(lengthBytes);

                // Define new buffer based on message size
                byte[] messageBytes = new byte[messageLength];
                client.Receive(messageBytes);
                int messageType = ByteConverter.ConvertIntBytes(messageBytes[5..9]);

                // if (messageType == 4000)
                // {
                //     Console.WriteLine("--- Message 4000 ---");
                //     int id = GetId(messageBytes);
                //     System.Console.WriteLine("ID: " + id);
                // }
                if (messageType == 4001)
                {
                    Console.WriteLine("--- Message 4001 ---");
                    bool isFinal = GetFinal(messageBytes);
                    System.Console.WriteLine("Final: " + isFinal);
                }
            }
        }

        private static int GetId(byte[] messageBytes)
        {
            int startIndex = 9;
            while (startIndex < messageBytes.Length)
            {
                DataSection dataSection = new(messageBytes, startIndex);
                startIndex += 8 + dataSection.SectionSize;

                if (dataSection.SectionNumber == 1)
                {
                    return ByteConverter.ConvertIntBytes(dataSection.SectionData);
                }
            }

            return 0;
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
                    BitArray bits = ByteConverter.ConvertBitBytes(dataSection.SectionData);
                    return bits[5];
                }
            }

            return false;
        }
    }
}