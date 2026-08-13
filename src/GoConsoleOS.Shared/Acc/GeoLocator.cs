using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace GoConsoleOS.Shared.Acc;

/// <summary>
/// Resolves public (WAN) IP addresses to approximate geographic coordinates so
/// the ACC portal can show "where is my USB console" on a map. Uses the free
/// ip-api.com endpoint (no API key) and caches results per IP.
/// </summary>
public sealed class GeoLocator
{
    private readonly HttpClient _http = new() { Timeout = System.TimeSpan.FromSeconds(8) };
    private readonly object _gate = new();
    private readonly Dictionary<string, (double Lat, double Lng, string City, string Country)> _cache = new();

    /// <summary>Latitude / longitude of the machine itself (from its WAN IP).</summary>
    public (double Lat, double Lng, string City, string Country)? Self()
    {
        try
        {
            var ip = _http.GetStringAsync("https://api.ipify.org").GetAwaiter().GetResult().Trim();
            return ForIp(ip);
        }
        catch
        {
            return null; // offline - no location to report
        }
    }

    /// <summary>Geo-locate an IP address. Returns null for private/loopback/unknown.</summary>
    public (double Lat, double Lng, string City, string Country)? ForIp(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip) || IsPrivate(ip)) return null;

        lock (_gate)
        {
            if (_cache.TryGetValue(ip, out var hit)) return hit;
        }

        try
        {
            var json = _http.GetStringAsync("http://ip-api.com/json/" + WebUtility.UrlEncode(ip)).GetAwaiter().GetResult();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("status", out var st) || st.GetString() != "success") return null;
            var result = (
                Lat: root.GetProperty("lat").GetDouble(),
                Lng: root.GetProperty("lon").GetDouble(),
                City: root.TryGetProperty("city", out var c) ? c.GetString() ?? "" : "",
                Country: root.TryGetProperty("country", out var co) ? co.GetString() ?? "" : "");
            lock (_gate) _cache[ip] = result;
            return result;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>10.x / 172.16-31.x / 192.168.x / 127.x / 169.254.x / ::1 etc.</summary>
    public static bool IsPrivate(string ip)
    {
        if (ip.Equals("localhost", System.StringComparison.OrdinalIgnoreCase)) return true;
        if (IPAddress.TryParse(ip, out var addr))
        {
            var bytes = addr.GetAddressBytes();
            if (addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                if (IPAddress.IsLoopback(addr) || ip.StartsWith("::ffff:0:0:0:0:0:0:")) return true;
                if (bytes.Take(10).All(b => b == 0) && bytes[10] == 0xFF && bytes[11] == 0xFF)
                    return IsPrivate(addr.MapToIPv4().ToString());
                return false;
            }
            if (bytes[0] == 10) return true;
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
            if (bytes[0] == 192 && bytes[1] == 168) return true;
            if (bytes[0] == 127) return true;
            if (bytes[0] == 169 && bytes[1] == 254) return true;
        }
        return false;
    }
}
