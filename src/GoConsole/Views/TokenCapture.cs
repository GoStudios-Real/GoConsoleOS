using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using GoConsoleOS.Shared;

namespace GoConsoleOS.GoConsole.Views;

public static class TokenCapture
{
    private static TcpListener? _listener;
    private static Thread? _thread;
    private static Timer? _timeout;
    private static Action<string>? _onToken;

    public static bool IsPending { get; private set; }

    public static bool Start(int port, Action<string> onToken)
    {
        Stop();
        try
        {
            var l = new TcpListener(IPAddress.Loopback, port);
            l.Start();
            _listener = l;
            _onToken = onToken;
            IsPending = true;
            _thread = new Thread(() => ListenLoop(l)) { IsBackground = true };
            _thread.Start();
            _timeout = new Timer(_ => Stop(), null, TimeSpan.FromMinutes(10), Timeout.InfiniteTimeSpan);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Warn($"TokenCapture start: {ex.Message}");
            return false;
        }
    }

    public static void Stop()
    {
        IsPending = false;
        _timeout?.Dispose();
        _timeout = null;
        var l = _listener;
        _listener = null;
        _onToken = null;
        try { l?.Stop(); } catch { }
    }

    private static void ListenLoop(TcpListener listener)
    {
        try
        {
            while (listener.Server.IsBound)
            {
                var client = listener.AcceptTcpClient();
                ThreadPool.QueueUserWorkItem(_ => HandleClient(client));
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"TokenCapture listener: {ex.Message}");
        }
    }

    private static void HandleClient(TcpClient client)
    {
        try
        {
            using (client)
            using (var stream = client.GetStream())
            {
                var reader = new StreamReader(stream, Encoding.ASCII, false, 8192, true);
                var requestLine = reader.ReadLine();
                if (string.IsNullOrEmpty(requestLine)) return;

                var parts = requestLine.Split(' ');
                if (parts.Length < 2) return;
                var method = parts[0];
                var path = parts[1];

                var contentLength = 0;
                string? line;
                while (!string.IsNullOrEmpty(line = reader.ReadLine()))
                {
                    if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                        int.TryParse(line.Substring(15).Trim(), out contentLength);
                }

                if (method == "POST" && path.StartsWith("/capture"))
                {
                    var buffer = new byte[contentLength];
                    var read = 0;
                    while (read < contentLength)
                        read += stream.Read(buffer, read, contentLength - read);
                    var body = Encoding.UTF8.GetString(buffer);
                    SendHtml(stream, "<h2>Connected!</h2><p>Token received. You can close this tab.</p>");
                    OnToken(body);
                }
                else
                {
                    var js = "<h2>GoConsoleOS Token Creator</h2>" +
                             "<p id='s'>Receiving token...</p>" +
                             "<script>if(location.hash){fetch('/capture',{method:'POST',body:location.hash.substring(1)});" +
                             "document.getElementById('s').textContent='Done! You can close this tab.'}" +
                             "else{document.getElementById('s').textContent='No token found.'}</script>";
                    SendHtml(stream, js);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"TokenCapture client: {ex.Message}");
        }
    }

    private static void SendHtml(NetworkStream stream, string bodyHtml)
    {
        var html = $"<html><body style=\"font-family:sans-serif;background:#141419;color:#fff;padding:40px\">{bodyHtml}</body></html>";
        var payload = Encoding.UTF8.GetBytes(html);
        var header = Encoding.ASCII.GetBytes(
            "HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: " +
            payload.Length + "\r\nConnection: close\r\n\r\n");
        stream.Write(header, 0, header.Length);
        stream.Write(payload, 0, payload.Length);
        stream.Flush();
    }

    private static void OnToken(string body)
    {
        var token = "";
        foreach (var part in body.Split('&'))
        {
            var kv = part.Split('=');
            if (kv.Length == 2 && kv[0] == "access_token")
                token = Uri.UnescapeDataString(kv[1]);
        }

        var cb = _onToken;
        Stop();
        if (!string.IsNullOrEmpty(token))
            cb?.Invoke(token);
    }
}
