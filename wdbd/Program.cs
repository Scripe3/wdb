using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;
using System.Linq;
using System.Collections.Concurrent;

class Program
{
    static bool dangerMode = false;
    static string? pairingCode = null;
    static bool requirePairing = false;

    static ConcurrentDictionary<string, bool> allowedIps = new();

    [DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    const int SW_HIDE = 0;

    static void Main(string[] args)
    {
        if (args.Contains("--danger"))
            dangerMode = true;

        bool hideWindow = args.Contains("--hide-window");

        if (args.Contains("create-new-pairing-code"))
        {
            var rnd = new Random();
            pairingCode = rnd.Next(100000, 999999).ToString();
            requirePairing = true;
            Console.WriteLine("Pairing code: " + pairingCode);
        }

        if (dangerMode)
        {
            Console.WriteLine("Dangerous Stage");
            Console.WriteLine("Enabled dangerous mode.");
            Console.WriteLine("WDBD is running in 10 sec...");
            Thread.Sleep(10000);
        }

        if (hideWindow && !dangerMode)
        {
            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_HIDE);
        }

        // localhost her zaman izinli
        allowedIps["127.0.0.1"] = true;

        // UDP pairing thread
        Thread pairingThread = new Thread(PairingListener);
        pairingThread.Start();

        TcpListener server = new TcpListener(IPAddress.Any, 5050);
        server.Start();

        Console.WriteLine("WDB server running");

        while (true)
        {
            TcpClient client = server.AcceptTcpClient();
            Thread t = new Thread(() => Handle(client));
            t.Start();
        }
    }

    static void PairingListener()
    {
        UdpClient udp = new UdpClient(5051);

        while (true)
        {
            try
            {
                var ep = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udp.Receive(ref ep);
                string msg = Encoding.UTF8.GetString(data);

                if (!msg.StartsWith("PAIR:"))
                    continue;

                string code = msg.Substring(5).Trim();

                if (!requirePairing || code == pairingCode)
                {
                    allowedIps[ep.Address.ToString()] = true;

                    byte[] ok = Encoding.UTF8.GetBytes("OK");
                    udp.Send(ok, ok.Length, ep);

                    Console.WriteLine("Paired: " + ep.Address);
                }
                else
                {
                    byte[] fail = Encoding.UTF8.GetBytes("NO");
                    udp.Send(fail, fail.Length, ep);
                }
            }
            catch { }
        }
    }

    static void Handle(TcpClient client)
    {
        try
        {
            var ep = client.Client.RemoteEndPoint as IPEndPoint;
            if (ep == null)
            {
                client.Close();
                return;
            }

            string remoteIp = ep.Address.ToString();

            if (!allowedIps.ContainsKey(remoteIp))
            {
                client.Close();
                return;
            }

            var stream = client.GetStream();
            byte[] buffer = new byte[8192];

            int bytes = stream.Read(buffer, 0, buffer.Length);
            if (bytes <= 0)
            {
                client.Close();
                return;
            }

            string cmd = Encoding.UTF8.GetString(buffer, 0, bytes).Trim();
            Console.WriteLine("CMD: " + cmd);

            string output = Execute(cmd);

            byte[] send = Encoding.UTF8.GetBytes(output);
            stream.Write(send, 0, send.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        client.Close();
    }

    static string Execute(string cmd)
    {
        if (cmd == "devices")
            return "localhost:5050\n";

        if (cmd == "reboot")
        {
            if (dangerMode)
            {
                Console.WriteLine("REBOOT REQUESTED");
                Console.Write("Type y to allow reboot: ");
                string? confirm = Console.ReadLine();

                if (!string.Equals(confirm?.Trim(), "y", StringComparison.OrdinalIgnoreCase))
                    return "Cancelled\n";
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = "shutdown",
                Arguments = "/r /t 0",
                UseShellExecute = true
            });

            return "Rebooting...\n";
        }

        string[] dangerous =
        {
            "diskpart","format","sysprep","systemreset","shutdown","bcdedit","clean"
        };

        bool isDanger = dangerous.Any(d => cmd.ToLower().Contains(d));

        if (isDanger)
        {
            if (!dangerMode)
                return "Danger mode disabled\n";

            Console.WriteLine($"DANGEROUS COMMAND: {cmd}");
            Console.Write("Type y to allow: ");
            string? confirm = Console.ReadLine();

            if (!string.Equals(confirm?.Trim(), "y", StringComparison.OrdinalIgnoreCase))
                return "Cancelled\n";

            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c " + cmd,
                UseShellExecute = true
            });

            return "Launched dangerous command\n";
        }

        try
        {
            Process p = new Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.Arguments = "/c " + cmd;
            p.StartInfo.RedirectStandardInput = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;

            p.Start();

            string output = p.StandardOutput.ReadToEnd();
            string err = p.StandardError.ReadToEnd();

            p.WaitForExit();

            Console.WriteLine("EXEC: " + cmd);

            return output + err;
        }
        catch (Exception ex)
        {
            return ex.Message + "\n";
        }
    }
}