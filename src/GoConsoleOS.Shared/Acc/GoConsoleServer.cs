using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GoConsoleOS.Shared.Ai;

namespace GoConsoleOS.Shared.Acc;

/// <summary>
/// The GoConsoleOS server. Serves the ACC REST API (<c>/api/acc/*</c>), the GoAI
/// chat endpoint (<c>/api/goai</c>), discovery (<c>/api/info</c>) and the account
/// website that is also published to GitHub Pages. Runs inside the USB console
/// and can be mirrored on-device by the Android companion.
///
/// Built on a plain <see cref="TcpListener"/> with a tiny HTTP parser so it works
/// on any Windows account without admin URL ACL reservations - ideal for a
/// portable USB console that must run on whatever machine it is plugged into.
/// </summary>
public sealed class GoConsoleServer : IDisposable
{
    public const int DefaultPort = 39210;

    private readonly AccStore _store;
    private readonly GoAiEngine _ai;
    private readonly CancellationTokenSource _cts = new();
    private readonly GeoLocator _geo = new();
    private string? _webRoot;
    private int _port = DefaultPort;
    private TcpListener? _listener;
    private Thread? _acceptThread;
    private (double Lat, double Lng, string City, string Country)? _selfLocation;

    public event Action<string, string>? OnLogin;

    public GoConsoleServer(AccStore store, GoAiEngine ai)
    {
        _store = store;
        _ai = ai;
        _webRoot = store.AccountWebRoot;
    }

    public bool IsRunning { get; private set; }
    public int Port => _port;

    public void Start(int port = DefaultPort)
    {
        if (IsRunning) return;
        _port = port;
        try
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            IsRunning = true;
            Logger.Info($"GoConsoleOS server listening on :{port}");

            _acceptThread = new Thread(AcceptLoop) { IsBackground = true, Name = "go-console-server" };
            _acceptThread.Start();

            TrackSelfLocation();
        }
        catch (Exception ex)
        {
            Logger.Warn($"GoConsoleOS server failed to start: {ex.Message}");
            IsRunning = false;
            _listener = null;
        }
    }

    private void TrackSelfLocation()
    {
        ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                _selfLocation = _geo.Self();
                if (_selfLocation is (_, _, var city, var country))
                    Logger.Info($"Console location: {city}, {country}");
            }
            catch (Exception ex)
            {
                Logger.Warn($"location lookup failed: {ex.Message}");
            }
        });
    }

    private void AcceptLoop()
    {
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                var client = _listener!.AcceptTcpClient();
                ThreadPool.QueueUserWorkItem(_ => HandleConnection(client));
            }
            catch (Exception)
            {
                if (_cts.IsCancellationRequested) break;
                Thread.Sleep(50);
            }
        }
    }

    // ---- HTTP plumbing -------------------------------------------------------

    private void HandleConnection(TcpClient client)
    {
        try
        {
            client.ReceiveTimeout = 15000;
            using var stream = client.GetStream();

            var requestLine = ReadLine(stream);
            if (string.IsNullOrEmpty(requestLine)) return;

            var parts = requestLine!.Split(' ');
            if (parts.Length < 2) return;
            var method = parts[0].ToUpperInvariant();
            var rawPath = parts[1];

            var clientIp = "";
            try
            {
                if (client.Client.RemoteEndPoint is IPEndPoint ep)
                    clientIp = ep.Address.ToString();
            }
            catch { }

            var queryIdx = rawPath.IndexOf('?');
            var path = queryIdx >= 0 ? rawPath[..queryIdx] : rawPath;
            if (path.Length == 0) path = "/";
            var query = queryIdx >= 0 ? rawPath[(queryIdx + 1)..] : "";

            // headers + body
            var contentLength = 0;
            var body = "";
            while (true)
            {
                var line = ReadLine(stream);
                if (string.IsNullOrEmpty(line)) break;
                var idx = line.IndexOf(':');
                if (idx > 0)
                {
                    var key = line[..idx].Trim();
                    var value = line[(idx + 1)..].Trim();
                    if (key.Equals("content-length", StringComparison.OrdinalIgnoreCase))
                        int.TryParse(value, out contentLength);
                }
            }
            if (contentLength > 0)
                body = ReadBody(stream, contentLength);

            if (method == "OPTIONS")
            {
                WriteResponse(stream, 204, "", "application/json; charset=utf-8");
                return;
            }

            if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
            {
                var (status, json) = Route(method, path, body, query, clientIp);
                WriteResponse(stream, status, json, "application/json; charset=utf-8");
            }
            else
            {
                ServeStatic(stream, path);
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"server request error: {ex.Message}");
        }
        finally
        {
            try { client.Close(); } catch { }
        }
    }

    private static string? ReadLine(Stream stream)
    {
        var sb = new StringBuilder();
        while (true)
        {
            var b = stream.ReadByte();
            if (b == -1) return sb.Length == 0 ? null : sb.ToString();
            if (b == '\n') return sb.ToString().TrimEnd('\r');
            sb.Append((char)b);
        }
    }

    private static string ReadBody(Stream stream, int length)
    {
        var buf = new byte[length];
        var offset = 0;
        while (offset < length)
        {
            var n = stream.Read(buf, offset, length - offset);
            if (n <= 0) break;
            offset += n;
        }
        return Encoding.UTF8.GetString(buf, 0, offset);
    }

    private void WriteResponse(Stream stream, int code, string json, string contentType)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        var status = code switch
        {
            200 => "200 OK",
            204 => "204 No Content",
            400 => "400 Bad Request",
            401 => "401 Unauthorized",
            404 => "404 Not Found",
            _ => "500 Internal Server Error",
        };
        var header = $"HTTP/1.1 {status}\r\n" +
                     $"Content-Type: {contentType}\r\n" +
                     (code == 204 ? "" : $"Content-Length: {bytes.Length}\r\n") +
                     "Connection: close\r\n" +
                     "Access-Control-Allow-Origin: *\r\n" +
                     "Access-Control-Allow-Headers: Content-Type, X-GoConsole-Token\r\n" +
                     "Access-Control-Allow-Methods: GET, POST, PUT, PATCH, DELETE, OPTIONS\r\n" +
                     "Cache-Control: no-cache\r\n" +
                     "\r\n";
        var hb = Encoding.ASCII.GetBytes(header);
        stream.Write(hb, 0, hb.Length);
        if (code != 204)
        {
            stream.Write(bytes, 0, bytes.Length);
        }
        stream.Flush();
    }

    // ---- static website ----------------------------------------------------

    private void ServeStatic(Stream stream, string path)
    {
        if (path == "/") path = "/index.html";
        var file = ResolveWebFile(path);
        if (file == null)
        {
            WriteResponse(stream, 404, "GoConsoleOS: not found", "text/plain; charset=utf-8");
            return;
        }
        var bytes = File.ReadAllBytes(file);
        WriteResponse(stream, 200, System.Text.Encoding.UTF8.GetString(bytes), MimeFor(Path.GetExtension(file)));
    }

    private string? ResolveWebFile(string path)
    {
        if (string.IsNullOrEmpty(_webRoot) || !Directory.Exists(_webRoot)) return null;
        var name = path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        if (string.IsNullOrWhiteSpace(name)) name = "index.html";
        // prevent path traversal
        var full = Path.GetFullPath(Path.Combine(_webRoot, name));
        var root = Path.GetFullPath(_webRoot);
        return full.StartsWith(root, StringComparison.OrdinalIgnoreCase) && File.Exists(full) ? full : null;
    }

    private static string MimeFor(string ext) => ext.ToLowerInvariant() switch
    {
        ".html" => "text/html; charset=utf-8",
        ".css" => "text/css; charset=utf-8",
        ".js" => "application/javascript; charset=utf-8",
        ".json" => "application/json; charset=utf-8",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".svg" => "image/svg+xml",
        ".ico" => "image/x-icon",
        _ => "application/octet-stream",
    };

    // ---- REST API -----------------------------------------------------------

    private (int Status, string Json) Route(string method, string path, string body, string query, string clientIp)
    {
        try
        {
            var p = path.ToLowerInvariant();
            if (p == "/api/info" && method == "GET")
                return (200, Json(new
                {
                    id = "GCS",
                    kind = "console.os",
                    name = "GoConsoleOS",
                    os = "GoConsoleOS",
                    version = "1.8.0",
                    server = "go-console-acc",
                    api = "1.0",
                    features = new[] { "acc", "goai", "link", "usb", "cast" },
                    time = DateTime.UtcNow,
                }));

            if (p == "/api/goai" && method == "POST")
                return HandleGoAi(body);

            if (p == "/api/update" && method == "GET")
                return (200, Json(new
                {
                    ok = true,
                    current = "1.8.0",
                    channel = "stable",
                    checkUrl = "https://raw.githubusercontent.com/GoStudios-Real/GoConsoleOS/main/update.json",
                    manifestVersion = 1,
                    serverTime = DateTime.UtcNow,
                }));

            if (p.StartsWith("/api/acc/", StringComparison.Ordinal))
                return HandleAcc(method, p["/api/acc/".Length..], body, query, clientIp);

            return (404, Err("unknown endpoint"));
        }
        catch (Exception ex)
        {
            return (500, Err(ex.Message));
        }
    }

    private (int, string) HandleGoAi(string body)
    {
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
        var input = doc.RootElement.TryGetProperty("message", out var m) ? m.GetString() : "";
        var reply = _ai.Reply(input ?? "");
        return (200, Json(new { reply = reply.Message, suggestions = reply.Suggestions }));
    }

    private (int, string) HandleAcc(string method, string sub, string body, string query, string clientIp)
    {
        return RouteAcc(method, sub, body, QueryValue(query, "token") ?? "", clientIp);
    }

    private (int, string) RouteAcc(string method, string sub, string body, string token, string clientIp)
    {
        var parts = sub.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var endpoint = parts.Length > 0 ? parts[0].ToLowerInvariant() : "";

        if (endpoint == "register" && method == "POST")
        {
            var (ok, msg, user) = DoRegister(body);
            return ok && user != null
                ? (200, Json(new { ok = true, token = _store.CreateSession(user, "console", clientIp), profile = AccStore.ToView(user) }))
                : (400, Json(new { ok = false, error = msg }));
        }

        if (endpoint == "login" && method == "POST")
        {
            var (ok, msg, user) = DoLogin(body);
            return ok && user != null
                ? (200, Json(new { ok = true, token = _store.CreateSession(user, "console", clientIp), profile = AccStore.ToView(user) }))
                : (401, Json(new { ok = false, error = msg }));
        }

        if (endpoint == "logout" && method == "POST")
        {
            var t = ReadToken(body);
            if (string.IsNullOrWhiteSpace(t)) t = token;
            _store.DestroySession(t);
            return (200, Json(new { ok = true }));
        }

        if (endpoint == "profile")
        {
            var user = RequireUser(body, token);
            if (user == null) return (401, Json(new { ok = false, error = "not authenticated" }));
            if (method == "GET")
                return (200, Json(new { ok = true, profile = AccStore.ToView(user) }));
            if (method == "PATCH")
            {
                UpdateProfile(user, body);
                _store.SaveUser(user);
                return (200, Json(new { ok = true, profile = AccStore.ToView(user) }));
            }
        }

            if (endpoint == "devices")
        {
            var user = RequireUser(body, token);
            if (user == null) return (401, Json(new { ok = false, error = "not authenticated" }));
            if (method == "GET")
                return (200, Json(new { ok = true, devices = user.Devices }));
            if (method == "POST")
            {
                var dev = RegisterDevice(user, body, clientIp);
                _store.SaveUser(user);
                return (200, Json(new { ok = true, device = dev }));
            }
            if (method == "DELETE" && parts.Length > 1)
            {
                var id = parts[1];
                user.Devices.RemoveAll(d => d.Id == id);
                _store.SaveUser(user);
                return (200, Json(new { ok = true }));
            }
        }

        if (endpoint == "map")
        {
            var user = RequireUser(body, token);
            if (user == null) return (401, Json(new { ok = false, error = "not authenticated" }));
            return (200, Json(new
            {
                ok = true,
                self = _selfLocation is (var slat, var slng, var scity, var scountry)
                    ? new { lat = slat, lng = slng, city = scity, country = scountry }
                    : null,
                devices = user.Devices,
            }));
        }

        if (endpoint == "subscriptions")
        {
            var user = RequireUser(body, token);
            if (user == null) return (401, Json(new { ok = false, error = "not authenticated" }));
            if (method == "POST")
            {
                AddSubscription(user, body);
                _store.SaveUser(user);
            }
            return (200, Json(new { ok = true, subscriptions = user.Subscriptions }));
        }

        if (endpoint == "activity")
        {
            var user = RequireUser(body, token);
            if (user == null) return (401, Json(new { ok = false, error = "not authenticated" }));
            return (200, Json(new { ok = true, activity = user.Activity }));
        }

        if (endpoint == "wallet")
        {
            var user = RequireUser(body, token);
            if (user == null) return (401, Json(new { ok = false, error = "not authenticated" }));
            if (method == "POST")
            {
                var amt = ExtractLong(body, "points", 0);
                user.GoPoints = Math.Max(0, user.GoPoints + amt);
                _store.SaveUser(user);
            }
            return (200, Json(new { ok = true, points = user.GoPoints }));
        }

        if (endpoint == "friends")
        {
            var user = RequireUser(body, token);
            if (user == null) return (401, Json(new { ok = false, error = "not authenticated" }));
            if (method == "POST")
            {
                var target = ExtractString(body, "username", "");
                var other = _store.FindByUsername(target);
                if (other == null) return (404, Json(new { ok = false, error = "user not found" }));
                if (!user.FriendIds.Contains(other.Id)) { user.FriendIds.Add(other.Id); _store.SaveUser(user); }
                if (!other.FriendIds.Contains(user.Id)) { other.FriendIds.Add(user.Id); _store.SaveUser(other); }
                _store.AddActivity(user, "social", $"Became friends with {other.DisplayName}");
            }
            return (200, Json(new { ok = true, friends = user.FriendIds }));
        }

        return (404, Err("unknown acc endpoint"));
    }

    // ---- helpers -----------------------------------------------------------

    private AccUser? RequireUser(string body, string queryToken = "")
    {
        var t = ReadToken(body);
        if (string.IsNullOrWhiteSpace(t)) t = queryToken;
        return _store.ValidateSession(t);
    }

    private static string ReadToken(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return "";
        try
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.TryGetProperty("token", out var t) ? t.GetString() ?? "" : "";
        }
        catch { return ""; }
    }

    private (bool, string, AccUser?) DoRegister(string body)
    {
        var username = ExtractString(body, "username", "");
        var display = ExtractString(body, "displayName", "");
        var password = ExtractString(body, "password", "");
        var email = ExtractString(body, "email", "");

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return (false, "username and password are required", null);
        if (password.Length < 4)
            return (false, "password must be at least 4 characters", null);
        if (_store.UsernameTaken(username))
            return (false, "username is already taken", null);

        var user = _store.CreateUser(username, display, password, string.IsNullOrWhiteSpace(email) ? null : email);
        return (true, "ok", user);
    }

    private (bool, string, AccUser?) DoLogin(string body)
    {
        var username = ExtractString(body, "username", "");
        var password = ExtractString(body, "password", "");
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return (false, "username and password are required", null);
        var user = _store.FindByUsername(username);
        if (user == null) return (false, "no such account", null);
        if (!AccStore.VerifyPassword(password, user.PasswordHash))
            return (false, "incorrect password", null);
        OnLogin?.Invoke(user.Username, user.DisplayName);
        return (true, "ok", user);
    }

    private void UpdateProfile(AccUser user, string body)
    {
        foreach (var field in new[] { "displayName", "bio", "avatar", "locale", "theme" })
        {
            var v = ExtractString(body, field, null);
            if (v == null) continue;
            switch (field)
            {
                case "displayName": user.DisplayName = v; break;
                case "bio": user.Bio = v; break;
                case "avatar": user.Avatar = v; break;
                case "locale": user.Locale = v; break;
                case "theme": user.Theme = v; break;
            }
        }
        if (ExtractBool(body, "twoFactorEnabled", null) is bool twofa)
            user.TwoFactorEnabled = twofa;
        if (ExtractBool(body, "emailVerified", null) is bool ev)
            user.EmailVerified = ev;
        var email = ExtractString(body, "email", null);
        if (email != null) user.Email = email;
    }

    private AccDevice RegisterDevice(AccUser user, string body, string ip)
    {
        var dev = new AccDevice
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = ExtractString(body, "name", "GoConsoleOS Device"),
            Kind = ExtractString(body, "kind", "console"),
            Os = ExtractString(body, "os", "GoConsoleOS"),
            IpAddress = ip,
            LastSeen = DateTime.UtcNow,
        };
        if (!GeoLocator.IsPrivate(ip) && _geo.ForIp(ip) is (var lat, var lng, var city, var country))
        {
            dev.Latitude = lat;
            dev.Longitude = lng;
            dev.City = city;
            dev.Country = country;
        }
        user.Devices.Add(dev);
        _store.AddActivity(user, "security", $"New device registered: {dev.Name}");
        return dev;
    }

    private void AddSubscription(AccUser user, string body)
    {
        var plan = ExtractString(body, "plan", "free");
        var existing = user.Subscriptions.FirstOrDefault(s => s.IsActive);
        if (existing != null) existing.IsActive = false;
        user.Subscriptions.Add(new AccSubscription
        {
            Id = Guid.NewGuid().ToString("N"),
            Plan = plan,
            Tier = plan,
            StartedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMonths(1),
            IsActive = true,
            PaymentMethod = ExtractString(body, "paymentMethod", null),
        });
        _store.AddActivity(user, "purchase", $"Subscribed to {plan} plan");
    }

    // ---- json utilities -----------------------------------------------------

    private static string? QueryValue(string query, string key)
    {
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = pair.IndexOf('=');
            var k = eq >= 0 ? pair[..eq] : pair;
            if (string.Equals(k, key, StringComparison.OrdinalIgnoreCase))
                return Uri.UnescapeDataString(eq >= 0 ? pair[(eq + 1)..] : "");
        }
        return null;
    }

    private static string ExtractString(string body, string key, string? def)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
            if (doc.RootElement.TryGetProperty(key, out var el))
                return el.ValueKind == JsonValueKind.Null ? def ?? "" : el.GetString() ?? "";
        }
        catch { }
        return def ?? "";
    }

    private static long ExtractLong(string body, string key, long def)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
            if (doc.RootElement.TryGetProperty(key, out var el) && el.TryGetInt64(out var v)) return v;
        }
        catch { }
        return def;
    }

    private static bool? ExtractBool(string body, string key, bool? def)
    {
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
            if (doc.RootElement.TryGetProperty(key, out var el) && el.ValueKind == JsonValueKind.True) return true;
            if (doc.RootElement.TryGetProperty(key, out el) && el.ValueKind == JsonValueKind.False) return false;
        }
        catch { }
        return def;
    }

    private static string Err(string message)
        => Json(new { ok = false, error = message });

    private static string Json(object obj)
        => JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
        });

    public void Dispose()
    {
        _cts.Cancel();
        try { _listener?.Stop(); } catch { }
        _listener = null;
        IsRunning = false;
        Logger.Info("GoConsoleOS server stopped");
    }
}
