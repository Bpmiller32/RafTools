using System.Net;
using System.Net.Sockets;
using System.Text;

#pragma warning disable CS4014

class Program
{
    static async Task Main()
    {
        Console.WriteLine("Hello, World!");

        IPEndPoint endPoint = new(IPAddress.Parse("127.0.0.1"), 1069);
        Socket client = new(SocketType.Stream, ProtocolType.Tcp);

        client.Connect(endPoint);

        Task.Run(async () =>
       {
           // AP Warmup
           await Task.Delay(TimeSpan.FromSeconds(2));

           System.Console.WriteLine("*** Requesting directory info ***");
           byte[] message = RequestDirectoryInfo();
           client.Send(message);

           await Task.Delay(TimeSpan.FromSeconds(10));

           System.Console.WriteLine("*** Requesting status alerts ***");
           message = RequestStatusAlerts();
           client.Send(message);

           await Task.Delay(TimeSpan.FromSeconds(10));

           System.Console.WriteLine("*** Requesting config change ***");
           message = RequestConfigChange("yookay");
           client.Send(message);
       });

        try
        {
            while (true)
            {
                if (client.Available <= 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    continue;
                }

                SocketMessage socketMessage = new(client);
                await socketMessage.ReadMessageHeader();

                // 6011: Info, 6010 PosAck for directory info request, 6015 PosAck for everything else?, 6016 NegAck
                System.Console.WriteLine($"MessageType: {socketMessage.MessageType}");
                await socketMessage.ReadAllDataSections();

                foreach (var dataSection in socketMessage.DataSections)
                {
                    System.Console.WriteLine($"Section: {dataSection.Key}");
                    System.Console.WriteLine($"Value: {dataSection.Value}");
                    System.Console.WriteLine(" ");
                }
            }
        }
        catch (Exception e)
        {
            System.Console.WriteLine(e.Message);
            client.Close();
        }
    }

    static byte[] RequestConfigChange(string data)
    {
        byte[] headerBytes = new byte[38] { 0, 0, 0, 0, 65, 80, 67, 84, 76, 65, 0, 0, 19, 165, 0, 0, 0, 0, 0, 0, 0, 8, 0, 0, 0, 0, 0, 0, 17, 213, 0, 0, 0, 1, 0, 0, 0, 0 };
        byte[] dataBytes = Encoding.ASCII.GetBytes(data);

        byte[] socketMessage = new byte[headerBytes.Length + dataBytes.Length];

        for (int i = 0; i < headerBytes.Length; i++)
        {
            if (i == 3 || i == 37)
            {
                // Set special length bytes
                socketMessage[3] = (byte)(socketMessage.Length - 4);
                socketMessage[37] = (byte)dataBytes.Length;
                continue;
            }

            socketMessage[i] = headerBytes[i];
        }

        for (int i = 0; i < dataBytes.Length; i++)
        {
            socketMessage[headerBytes.Length + i] = dataBytes[i];
        }

        return socketMessage;
    }

    static byte[] RequestDirectoryInfo()
    {
        byte[] socketMessage = new byte[42] { 0, 0, 0, 38, 65, 80, 67, 84, 76, 65, 0, 0, 19, 154, 0, 0, 0, 0, 0, 0, 0, 8, 0, 0, 0, 0, 0, 0, 0, 43, 0, 0, 0, 1, 0, 0, 0, 4, 0, 0, 0, 1 };
        return socketMessage;
    }

    static byte[] RequestStatusAlerts()
    {
        byte[] socketMessage = new byte[42] { 0, 0, 0, 38, 65, 80, 67, 84, 76, 65, 0, 0, 19, 155, 0, 0, 0, 0, 0, 0, 0, 8, 0, 0, 0, 0, 0, 0, 3, 153, 0, 0, 0, 1, 0, 0, 0, 4, 0, 0, 0, 50 };
        return socketMessage;
    }
}

class SocketMessage
{
    public int MessageType { get; set; }
    public Dictionary<int, string> DataSections { get; set; } = new();

    private readonly Socket socket;
    private int remainingBytes;

    private int dataSectionType;
    private int dataSectionSize;

    public SocketMessage(Socket socket)
    {
        this.socket = socket;
    }

    public async Task ReadMessageHeader()
    {
        byte[] sizeBytes = new byte[4];
        byte[] signatureBytes = new byte[6];
        byte[] typeBytes = new byte[4];

        await RecieveFromSocket(sizeBytes);
        await RecieveFromSocket(signatureBytes);
        await RecieveFromSocket(typeBytes);

        MessageType = ConvertIntBytes(typeBytes);

        // Subtract the above header reading (6 from signature + 4 from type, messageSize excluded itself)
        remainingBytes = ConvertIntBytes(sizeBytes) - 10;
    }

    public async Task ReadAllDataSections()
    {
        while (remainingBytes > 0)
        {
            await ReadSection();
        }
    }

    private async Task ReadSection()
    {
        // Read section header
        byte[] typeBytes = new byte[4];
        byte[] sizeBytes = new byte[4];

        await RecieveFromSocket(typeBytes);
        await RecieveFromSocket(sizeBytes);

        dataSectionType = ConvertIntBytes(typeBytes);
        dataSectionSize = ConvertIntBytes(sizeBytes);

        // Read section value
        byte[] valueBytes = new byte[dataSectionSize];
        await RecieveFromSocket(valueBytes);
        DataSections.Add(dataSectionType, Encoding.UTF8.GetString(valueBytes));
    }

    private async Task RecieveFromSocket(byte[] bytes)
    {
        await socket.ReceiveAsync(bytes, SocketFlags.None);
        remainingBytes -= bytes.Length;
    }

    private static int ConvertIntBytes(byte[] bytes)
    {
        Array.Reverse(bytes);
        uint value = BitConverter.ToUInt32(bytes);

        return Convert.ToInt32(value);
    }
}