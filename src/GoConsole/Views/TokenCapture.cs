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
                    SendHtml(stream, "<h2>Connected!</h2><p>Authorization received. You can close this tab.</p>");
                    var cb = _onToken;
                    Stop();
                    cb?.Invoke(body);
                }
                else
                {
                    var query = path.Contains('?') ? path.Substring(path.IndexOf('?') + 1) : "";
                    var code = ExtractParam(query, "code");
                    var accessToken = ExtractParam(query, "access_token");
                    var value = !string.IsNullOrEmpty(code) ? code : accessToken;

                    if (!string.IsNullOrEmpty(value))
                    {
                        SendHtml(stream, "<h2>Connected!</h2><p>Authorization received. You can close this tab.</p>");
                        var cb = _onToken;
                        Stop();
                        cb?.Invoke(value);
                    }
                    else
                    {
                        var js = "<h2>GoConsoleOS Token Creator</h2>" +
                                 "<p id='s'>Receiving authorization...</p>" +
                                 "<script>" +
                                 "function send(v){fetch('/capture',{method:'POST',body:v}).then(function(){" +
                                 "document.getElementById('s').textContent='Done! You can close this tab.'})}" +
                                 "var u=new URLSearchParams(location.search);var h=new URLSearchParams(location.hash.substring(1));" +
                                 "var c=u.get('code')||h.get('access_token');" +
                                 "if(c){send(c)}" +
                                 "else{setTimeout(function(){var u2=new URLSearchParams(location.search);" +
                                 "var h2=new URLSearchParams(location.hash.substring(1));" +
                                 "var c2=u2.get('code')||h2.get('access_token');" +
                                 "if(c2){send(c2)}else{" +
                                 "document.getElementById('s').textContent='No authorization found.'}},1500)}" +
                                 "</script>";
                        SendHtml(stream, js);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"TokenCapture client: {ex.Message}");
        }
    }

    private static void ServeAuthPage(NetworkStream stream)
    {
        var js = "<h2>GoConsoleOS Token Creator</h2>" +
                 "<p id='s'>Receiving authorization...</p>" +
                 "<script>" +
                 "var u=new URLSearchParams(location.search);var h=new URLSearchParams(location.hash.substring(1));" +
                 "var c=u.get('code')||h.get('access_token');" +
                 "if(c){fetch('/capture',{method:'POST',body:c});" +
                 "document.getElementById('s').textContent='Done! You can close this tab.'}" +
                 "else{document.getElementById('s').textContent='No authorization found.'}" +
                 "</script>";
        SendHtml(stream, js);
    }

    private static string ExtractParam(string query, string name)
    {
        foreach (var part in query.Split('&'))
        {
            var kv = part.Split('=');
            if (kv.Length == 2 && kv[0] == name)
                return Uri.UnescapeDataString(kv[1]);
        }
        return "";
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

    private static void OnCode(string code)
    {
        var cb = _onToken;
        Stop();
        if (!string.IsNullOrEmpty(code))
            cb?.Invoke(code);
    }
}
