using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GoConsoleOS.Shared;

/// <summary>A remote tool the host can run, advertised to companions.</summary>
public sealed class ToolInfo
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Desc { get; set; } = "";
}

/// <summary>Result of a USB health probe, matching the Android UsbDeviceInfo model.</summary>
public sealed class UsbHealthRecord
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public string Vendor { get; set; } = "";
    public string Product { get; set; } = "";
    public string Serial { get; set; } = "";
    public string Health { get; set; } = "unknown";
    public int HealthScore { get; set; }
    public long Total { get; set; }
    public long Free { get; set; }
    public string Interface { get; set; } = "";
    public string Issue { get; set; } = "";
    public bool Mounted { get; set; }
}

/// <summary>
/// Windows host for the GoConsoleOS Android companion apps.
/// Implements the transport documented in the GoConsoleOS-Android repo:
/// UDP :39100 discovery + TCP :39101 control/media stream.
/// </summary>
public sealed class LinkServer : IDisposable
{
    public const int DiscoveryPort = 39100;
    public const int LinkPort = 39101;

    private readonly object _gate = new();
    private readonly List<TcpClient> _clients = new();

    private readonly Func<IEnumerable<string>> _gamesProvider;
    private readonly Func<IEnumerable<UsbHealthRecord>> _usbProvider;
    private readonly Action<string> _launchAction;
    private readonly Action _openInstallerAction;
    private readonly Func<IEnumerable<ToolInfo>>? _toolsProvider;
    private readonly Action<string>? _toolAction;
    private readonly Action<byte[]> _castFrame;

    private UdpClient? _discovery;
    private TcpListener? _listener;
    private CancellationTokenSource _cts = new();
    private bool _running;

    public LinkServer(
        Func<IEnumerable<string>>? gamesProvider = null,
        Func<IEnumerable<UsbHealthRecord>>? usbProvider = null,
        Action<string>? launchAction = null,
        Action? openInstallerAction = null,
        Func<IEnumerable<ToolInfo>>? toolsProvider = null,
        Action<string>? toolAction = null,
        Action<byte[]>? castFrame = null)
    {
        _gamesProvider = gamesProvider ?? (() => new List<string>());
        _usbProvider = usbProvider ?? (() => new List<UsbHealthRecord>());
        _launchAction = launchAction ?? (_ => { });
        _openInstallerAction = openInstallerAction ?? (() => { });
        _toolsProvider = toolsProvider;
        _toolAction = toolAction;
        _castFrame = castFrame ?? (_ => { });
    }

    public bool IsRunning => _running;

    public void Start()
    {
        if (_running) return;
        _running = true;
        _cts = new CancellationTokenSource();
        try
        {
            _discovery = new UdpClient(new IPEndPoint(IPAddress.Any, DiscoveryPort)) { EnableBroadcast = true };
            _discovery.Client.ReceiveTimeout = 1000;
            _ = Task.Run(() => DiscoveryLoop(_cts.Token));
        }
        catch (Exception ex)
        {
            Logger.Warn($"LinkServer discovery failed: {ex.Message}");
        }

        try
        {
            _listener = new TcpListener(IPAddress.Any, LinkPort);
            _listener.Start();
            _ = Task.Run(() => AcceptLoop(_cts.Token));
            Logger.Info($"LinkServer listening on :{LinkPort}");
        }
        catch (Exception ex)
        {
            Logger.Warn($"LinkServer tcp failed: {ex.Message}");
        }
    }

    private void DiscoveryLoop(CancellationToken token)
    {
        var beacon = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["id"] = "GCS",
            ["kind"] = "console.os",
            ["name"] = "GoConsoleOS",
            ["port"] = LinkPort,
            ["apiPort"] = 39210,
            ["version"] = "2.2.0",
            ["features"] = new[] { "link", "usb", "cast", "api", "games", "remote" },
            ["os"] = "GoConsoleOS",
        });
        var payload = Encoding.UTF8.GetBytes(beacon);
        try
        {
            while (!token.IsCancellationRequested)
            {
                var result = _discovery!.ReceiveAsync().GetAwaiter().GetResult();
                var msg = Encoding.UTF8.GetString(result.Buffer);
                if (msg.Contains("\"kind\":\"hello\"", StringComparison.OrdinalIgnoreCase) ||
                    msg.Contains("\"id\":\"GCS\"", StringComparison.OrdinalIgnoreCase))
                {
                    _discovery.Send(payload, payload.Length, result.RemoteEndPoint);
                }
            }
        }
        catch (SocketException) { } // receive timeout / socket closed
        catch (ObjectDisposedException) { }
        catch (OperationCanceledException) { }
        catch (Exception ex) { Logger.Warn($"LinkServer discovery: {ex.Message}"); }
    }

    private void AcceptLoop(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                var client = _listener!.AcceptTcpClient();
                lock (_gate) _clients.Add(client);
                _ = Task.Run(() => ClientLoop(client, token));
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { Logger.Warn($"LinkServer accept: {ex.Message}"); }
    }

    private void ClientLoop(TcpClient client, CancellationToken token)
    {
        try
        {
            var stream = client.GetStream();
            var reader = new BinaryReader(stream);
            var writer = new BinaryWriter(stream);

            // mixed stream: {json}\n control lines + [type:1][len:4][payload] frames
            while (!token.IsCancellationRequested)
            {
                var header = reader.ReadByte();
                if (header == (byte)'{')
                {
                    var line = ReadLine(reader, alreadyRead: '{');
                    if (line == null) break;
                    HandleControl(line, client, writer);
                }
                else
                {
                    // binary frame: 4-byte length + payload
                    var len = ReadInt(reader);
                    if (len < 0 || len > 32 * 1024 * 1024) break;
                    var payload = reader.ReadBytes(len);
                    if (payload.Length != len) break;
                    if (header == 5) HandleInput(payload);
                    if (header == 3) _castFrame(payload);
                    if (header == 4) _castFrame(payload);
                }
            }
        }
        catch (Exception ex)
        {
            if (!token.IsCancellationRequested)
                Logger.Warn($"LinkServer client: {ex.Message}");
        }
        finally
        {
            lock (_gate) _clients.Remove(client);
            client.Dispose();
        }
    }

    private void HandleControl(string line, TcpClient client, BinaryWriter writer)
    {
        try
        {
            using var doc = JsonDocument.Parse(line);
            var type = doc.RootElement.TryGetProperty("type", out var t) ? t.GetString() : null;
            switch (type)
            {
                case "hello":
                    SendControl(writer, "hello", ("ok", true), ("server", "GoConsoleOS"));
                    break;
                case "games.list":
                {
                    var games = _gamesProvider().ToList();
                    SendControl(writer, "games.list", ("games", games));
                    break;
                }
                case "games.launch":
                {
                    var title = doc.RootElement.TryGetProperty("title", out var tt) ? tt.GetString() : null;
                    if (!string.IsNullOrWhiteSpace(title)) _launchAction(title);
                    break;
                }
                case "usb.list":
                {
                    var devices = _usbProvider().Select(d => new Dictionary<string, object?>
                    {
                        ["id"] = d.Id, ["label"] = d.Label, ["vendor"] = d.Vendor,
                        ["product"] = d.Product, ["serial"] = d.Serial, ["health"] = d.Health,
                        ["healthScore"] = d.HealthScore, ["total"] = d.Total, ["free"] = d.Free,
                        ["interface"] = d.Interface, ["issue"] = d.Issue, ["mounted"] = d.Mounted,
                    }).ToList();
                    SendControl(writer, "usb.list", ("devices", devices));
                    break;
                }
                case "pair":
                {
                    var action = doc.RootElement.TryGetProperty("action", out var a) ? a.GetString() : null;
                    if (action == "open-usb-installer") _openInstallerAction();
                    SendControl(writer, "pair", ("ok", true));
                    break;
                }
                case "tools.list":
                {
                    var tools = _toolsProvider?.Invoke()
                        .Select(t => new Dictionary<string, object?>
                        {
                            ["id"] = t.Id, ["name"] = t.Name, ["desc"] = t.Desc,
                        }).ToList() ?? new List<Dictionary<string, object?>>();
                    SendControl(writer, "tools.list", ("tools", tools));
                    break;
                }
                case "tools.run":
                {
                    var tool = doc.RootElement.TryGetProperty("tool", out var tt) ? tt.GetString() : null;
                    if (!string.IsNullOrWhiteSpace(tool)) _toolAction?.Invoke(tool);
                    SendControl(writer, "tools.run", ("ok", true), ("tool", tool ?? ""));
                    break;
                }
                case "cast.start":
                    SendControl(writer, "cast.start", ("ok", true));
                    break;
                case "cast.stop":
                    SendControl(writer, "cast.stop", ("ok", true));
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"LinkServer control parse: {ex.Message}");
        }
    }

    private void HandleInput(byte[] payload)
    {
        // 4-byte big-endian button bitmask; wire to the shell's controller engine
        if (payload.Length >= 4)
        {
            uint mask = ((uint)payload[0] << 24) | ((uint)payload[1] << 16) |
                        ((uint)payload[2] << 8) | payload[3];
            InputReceived?.Invoke(mask);
        }
    }

    /// <summary>Raised when the phone sends a 4-byte button bitmask (type-5 frame).</summary>
    public event Action<uint>? InputReceived;

    private static string? ReadLine(BinaryReader reader, char alreadyRead = '\0')
    {
        var sb = new StringBuilder();
        if (alreadyRead != '\0') sb.Append(alreadyRead);
        while (true)
        {
            int c;
            try { c = reader.Read(); } catch { return null; }
            if (c < 0 || c == '\n') break;
            sb.Append((char)c);
        }
        return sb.ToString();
    }

    private static int ReadInt(BinaryReader reader)
    {
        var b = reader.ReadBytes(4);
        if (b.Length < 4) return -1;
        return (b[0] << 24) | (b[1] << 16) | (b[2] << 8) | b[3];
    }

    private static void SendControl(BinaryWriter writer, string type, params (string Key, object? Value)[] fields)
    {
        var dict = new Dictionary<string, object?> { ["type"] = type };
        foreach (var (key, value) in fields)
            dict[key] = value;
        var line = JsonSerializer.Serialize(dict) + "\n";
        writer.Write(Encoding.UTF8.GetBytes(line));
        writer.Flush();
    }

    public void Dispose()
    {
        _running = false;
        _cts.Cancel();
        lock (_gate)
        {
            foreach (var c in _clients) c.Dispose();
            _clients.Clear();
        }
        _discovery?.Dispose();
        _listener?.Stop();
        Logger.Info("LinkServer stopped");
    }
}