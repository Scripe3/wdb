using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

class Program
{
    const int udpPort = 5051;
    const int tcpPort = 5050;
    static string configFile = Path.Combine(AppContext.BaseDirectory, "wdb.config");

    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("usage:");
            Console.WriteLine(" wdb <command>");
            Console.WriteLine("commands:");
            Console.WriteLine(" wdb pair <pairingCode>");
            Console.WriteLine(" wdb disconnect");
            Console.WriteLine(" wdb devices");
            Console.WriteLine(" wdb <cmdCommand>");
            return;
        }

        switch (args[0])
        {
            case "pair":
                if (args.Length < 2)
                {
                    Console.WriteLine("Warning: Pair code required.");
                    return;
                }
                Pair(args[1]);
                return;

            case "disconnect":
                Disconnect();
                return;

            case "devices":
                Devices();
                return;

            default:
                SendCommand(string.Join(" ", args));
                return;
        }
    }

    static void Devices()
    {
        if (!File.Exists(configFile))
        {
            Console.WriteLine("List of paired devices");
            Console.WriteLine();
            return;
        }

        string ip = File.ReadAllText(configFile).Trim();

        Console.WriteLine("List of paired devices");
        Console.WriteLine(ip + ":5050\tdevice");
    }

    static void Pair(string code)
    {
        Console.WriteLine("wdb: Device searching. Please wait a few seconds...");

        using UdpClient udp = new UdpClient();
        udp.EnableBroadcast = true;

        byte[] data = Encoding.UTF8.GetBytes("PAIR:" + code);
        udp.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, udpPort));

        var remote = new IPEndPoint(IPAddress.Any, 0);
        udp.Client.ReceiveTimeout = 5000;

        try
        {
            byte[] resp = udp.Receive(ref remote);
            string txt = Encoding.UTF8.GetString(resp);

            if (txt == "OK")
            {
                string ip = remote.Address.ToString();
                File.WriteAllText(configFile, ip);
                Console.WriteLine("Paired with: " + ip);
            }
            else
            {
                Console.WriteLine("Error: Pair rejected.");
            }
        }
        catch
        {
            Console.WriteLine("Error: Pair failed.");
        }
    }

    static void Disconnect()
    {
        if (File.Exists(configFile))
        {
            File.Delete(configFile);
            Console.WriteLine("Disconnected from device.");
        }
        else
        {
            Console.WriteLine("Error: Not already connected.");
        }
    }

    static void SendCommand(string cmd)
    {
        if (!File.Exists(configFile))
        {
            Console.WriteLine("Error: Pair required.");
            return;
        }

        string ip = File.ReadAllText(configFile).Trim();

        try
        {
            using TcpClient client = new TcpClient();
            client.Connect(ip, tcpPort);

            var stream = client.GetStream();
            byte[] data = Encoding.UTF8.GetBytes(cmd);
            stream.Write(data, 0, data.Length);

            byte[] buffer = new byte[8192];
            int read = stream.Read(buffer, 0, buffer.Length);

            Console.WriteLine(Encoding.UTF8.GetString(buffer, 0, read));
        }
        catch (Exception ex)
        {
            Console.WriteLine("Connection Error: " + ex.Message);
        }
    }
}