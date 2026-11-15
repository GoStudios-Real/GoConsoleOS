using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace GoConsoleOS.Shared.Acc;

/// <summary>
/// One-time pairing code used by the GoConsoleOS Cloud Gaming app (Android /
/// Google TV Streamer) to connect to this console. The code is shown on the
/// console's Remote Play screen (as text and inside a QR code) and expires
/// after <see cref="Lifetime"/>. The Android app submits it to
/// <c>POST /api/pair/verify</c>.
/// </summary>
public static class PairingService
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no I/O/0/1
    private static readonly object Gate = new();
    private static string _code = "";
    private static DateTime _expiresUtc = DateTime.MinValue;
    private static readonly Random Rng = new();

    /// <summary>Current pairing code (generates/rotates automatically).</summary>
    public static string CurrentCode
    {
        get { lock (Gate) { Ensure(); return _code; } }
    }

    /// <summary>UTC time the current code expires.</summary>
    public static DateTime ExpiresUtc
    {
        get { lock (Gate) { Ensure(); return _expiresUtc; } }
    }

    /// <summary>Force a new code (e.g. the user pressed "New code").</summary>
    public static string Regenerate()
    {
        lock (Gate)
        {
            _code = NewCode();
            _expiresUtc = DateTime.UtcNow + Lifetime;
            return _code;
        }
    }

    public static bool Verify(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        lock (Gate)
        {
            Ensure();
            return DateTime.UtcNow < _expiresUtc &&
                   string.Equals(code.Trim(), _code, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>Best-effort first private IPv4 of this machine (LAN address).</summary>
    public static string? DetectLanIPv4()
    {
        try
        {
            using var probe = new UdpClient();
            probe.Connect("8.8.8.8", 80);
            if (probe.Client.LocalEndPoint is IPEndPoint ep) return ep.Address.ToString();
        }
        catch { }

        try
        {
            var addr = Dns.GetHostAddresses(Dns.GetHostName())
                .FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork &&
                                     !IPAddress.IsLoopback(a));
            if (addr != null) return addr.ToString();
        }
        catch { }

        return null;
    }

    /// <summary>Deep link encoded in the QR code shown on the console.</summary>
    public static string BuildDeepLink(int httpPort = GoConsoleServer.DefaultPort)
    {
        var host = DetectLanIPv4() ?? "127.0.0.1";
        var code = CurrentCode;
        return $"goconsoleos://connect?host={host}&http={httpPort}&link={LinkServer.LinkPort}&code={code}";
    }

    private static void Ensure()
    {
        if (_code.Length == 0 || DateTime.UtcNow >= _expiresUtc)
        {
            _code = NewCode();
            _expiresUtc = DateTime.UtcNow + Lifetime;
        }
    }

    private static string NewCode()
    {
        var sb = new StringBuilder(6);
        for (var i = 0; i < 6; i++) sb.Append(Alphabet[Rng.Next(Alphabet.Length)]);
        return sb.ToString();
    }
}
