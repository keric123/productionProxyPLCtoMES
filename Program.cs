using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Program
{
    // The port where the proxy listens.
    private const int ProxyPort = 0000; //this port plc targets, must be configured.

    // The real GHP ComCell endpoint, Please note that all of the parameters must be changed to suit your needs.
    // You must change GHP config to listen on a specific port you need. 
    private const string GhpHost = "INSERT PC IP HERE";
    private const int GhpPort = 0000;

    // Log file name
    private static readonly string LogFile = "proxy_log.txt";

    static async Task Main()
    {
        Log("=== Proxy started ===");
        Log($"Listening on port {ProxyPort}, forwarding to {GhpHost}:{GhpPort}");

        // Start TCP listener for PLC
        var listener = new TcpListener(IPAddress.Any, ProxyPort);
        listener.Start();

        while (true)
        {
            // Accept PLC connection
            var client = await listener.AcceptTcpClientAsync();
            _ = HandleClientAsync(client);
        }
    }

    private static async Task HandleClientAsync(TcpClient plcClient)
    {
        Log("PLC connected.");

        using (plcClient)
        using (var ghpClient = new TcpClient())
        {
            try
            {
                // Connect to GHP on its new port
                await ghpClient.ConnectAsync(GhpHost, GhpPort);
                Log("Connected to GHP.");

                var plcStream = plcClient.GetStream();
                var ghpStream = ghpClient.GetStream();

                // Start forwarding in both directions
                var t1 = ForwardAsync(plcStream, ghpStream, fromPlc: true);
                var t2 = ForwardAsync(ghpStream, plcStream, fromPlc: false);

                await Task.WhenAny(t1, t2);
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}");
            }
        }

        Log("Connection closed.");
    }

    private static async Task ForwardAsync(NetworkStream input, NetworkStream output, bool fromPlc)
    {
        var buffer = new byte[4096];

        while (true)
        {
            int bytesRead;

            try
            {
                bytesRead = await input.ReadAsync(buffer, 0, buffer.Length);
            }
            catch
            {
                break;
            }

            if (bytesRead <= 0)
                break;

            var msg = Encoding.ASCII.GetString(buffer, 0, bytesRead);

            if (fromPlc)
            {
                Log($"PLC → Proxy: {Sanitize(msg)}");

                // Validate PLC → GHP messages
                if (!ValidateMessage(msg, out var error))
                {
                    Log($"BLOCKED message: {error}");

                    // Send error back to PLC
                    var err = Encoding.ASCII.GetBytes($"ERROR, text=\"Wrong Message Structure: {error}\"\u0003");
                    await output.WriteAsync(err, 0, err.Length);
                    break;
                }
            }
            else
            {
                Log($"GHP → Proxy: {Sanitize(msg)}");
            }

            // Forward message
            await output.WriteAsync(buffer, 0, bytesRead);
        }
    }

    // Minimal validator to prevent GHP crashes
    private static bool ValidateMessage(string raw, out string error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(raw))
        {
            error = "Empty message.";
            return false;
        }

        // Remove STX/ETX
        string trimmed = raw.Trim('\u0002', '\u0003');

        var tokens = trimmed.Split(',');

        // Your ComCell config uses MaxParams="5"
        if (tokens.Length > 5)
        {
            error = "Too many tokens.";
            return false;
        }

        string command = tokens[0];
        string payload = tokens[^1];

        // Prevent the exact crash you experienced
        if (command == "PING" && payload.Contains("<UnitCheckin"))
        {
            error = "PING message contains unexpected UnitCheckin payload.";
            return false;
        }

        // Basic XML-like validation
        if (payload.Contains("<"))
        {
            if (!payload.Contains("/>"))
            {
                error = "Missing closing '/>' in payload.";
                return false;
            }

            // Detect missing quotes (your crash case)
            if (payload.Contains("=<"))
            {
                error = "Attribute missing quotes.";
                return false;
            }
        }

        return true;
    }

    // Logging helper
    private static void Log(string text)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {text}";
        Console.WriteLine(line);
        File.AppendAllText(LogFile, line + Environment.NewLine);
    }

    // Replace control characters with readable markers
    private static string Sanitize(string msg)
    {
        return msg
            .Replace("\u0002", "<STX>")
            .Replace("\u0003", "<ETX>")
            .Replace("\r", "")
            .Replace("\n", "");
    }
}
