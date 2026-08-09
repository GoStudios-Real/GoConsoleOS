using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace GoConsoleOS.Shared.Acc;

/// <summary>
/// Persistent account store for the ACC (Account, Cloud &amp; Community) system.
/// Keeps users, sessions, devices, wallets, subscriptions and activity in
/// <c>system\acc\</c> so the whole thing works offline on a portable USB console.
/// </summary>
public sealed class AccStore
{
    private readonly string _dataDir;
    private readonly object _gate = new();
    private readonly Dictionary<string, AccUser> _users = new();
    private readonly List<AccSession> _sessions = new();

    public string? AccountWebRoot { get; set; }

    public AccStore(string rootPath)
    {
        _dataDir = Path.Combine(rootPath, "system", "acc");
        Directory.CreateDirectory(_dataDir);
        Load();
    }

    public string DataDir => _dataDir;

    // ---- persistence ------------------------------------------------------

    private void Load()
    {
        lock (_gate)
        {
            var usersPath = Path.Combine(_dataDir, "users.json");
            var sessionsPath = Path.Combine(_dataDir, "sessions.json");
            if (File.Exists(usersPath))
            {
                try
                {
                    var users = JsonSerializer.Deserialize<List<AccUser>>(File.ReadAllText(usersPath));
                    if (users != null)
                        foreach (var u in users) _users[u.Username.ToLowerInvariant()] = u;
                }
                catch { }
            }
            if (File.Exists(sessionsPath))
            {
                try
                {
                    var sessions = JsonSerializer.Deserialize<List<AccSession>>(File.ReadAllText(sessionsPath));
                    if (sessions != null) _sessions.AddRange(sessions);
                }
                catch { }
            }
            _sessions.RemoveAll(s => s.ExpiresAt < DateTime.UtcNow);
        }
    }

    private void SaveUsers()
    {
        try
        {
            lock (_gate)
            {
                File.WriteAllText(Path.Combine(_dataDir, "users.json"),
                    JsonSerializer.Serialize(_users.Values.ToList(), new JsonSerializerOptions { WriteIndented = true }));
            }
        }
        catch { }
    }

    private void SaveSessions()
    {
        try
        {
            lock (_gate)
            {
                File.WriteAllText(Path.Combine(_dataDir, "sessions.json"),
                    JsonSerializer.Serialize(_sessions, new JsonSerializerOptions { WriteIndented = true }));
            }
        }
        catch { }
    }

    // ---- users ------------------------------------------------------------

    public AccUser? FindByUsername(string username)
    {
        lock (_gate) return _users.TryGetValue(username.Trim().ToLowerInvariant(), out var u) ? u : null;
    }

    public AccUser? FindById(string id)
    {
        lock (_gate) return _users.Values.FirstOrDefault(u => u.Id == id);
    }

    public IReadOnlyList<AccUser> All()
    {
        lock (_gate) return _users.Values.ToList();
    }

    public bool UsernameTaken(string username)
        => FindByUsername(username) != null;

    public AccUser CreateUser(string username, string displayName, string password, string? email = null)
    {
        var user = new AccUser
        {
            Id = Guid.NewGuid().ToString("N"),
            Username = username.Trim(),
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? username.Trim() : displayName.Trim(),
            Email = email,
            PasswordHash = HashPassword(password),
            CreatedAt = DateTime.UtcNow,
        };
        user.Devices.Add(new AccDevice { Id = Guid.NewGuid().ToString("N"), Name = "GoConsoleOS Console", Kind = "console", Os = "GoConsoleOS" });
        user.Subscriptions.Add(new AccSubscription { Id = Guid.NewGuid().ToString("N"), Plan = "free", Tier = "free" });
        user.Activity.Add(new AccActivity { Id = Guid.NewGuid().ToString("N"), Type = "info", Message = "Account created" });

        lock (_gate) _users[user.Username.ToLowerInvariant()] = user;
        SaveUsers();
        Logger.Info($"ACC: account created for {username}");
        return user;
    }

    public void SaveUser(AccUser user)
    {
        lock (_gate) _users[user.Username.ToLowerInvariant()] = user;
        SaveUsers();
    }

    public void AddActivity(AccUser user, string type, string message)
    {
        user.Activity.Insert(0, new AccActivity { Id = Guid.NewGuid().ToString("N"), Type = type, Message = message });
        if (user.Activity.Count > 200) user.Activity.RemoveRange(200, user.Activity.Count - 200);
        SaveUser(user);
    }

    // ---- sessions ----------------------------------------------------------

    public string CreateSession(AccUser user, string deviceName, string ip)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
            .Replace("+", "").Replace("/", "").Replace("=", "");

        lock (_gate)
        {
            _sessions.RemoveAll(s => s.UserId == user.Id);
            var session = new AccSession
            {
                Token = token,
                UserId = user.Id,
                DeviceName = deviceName,
                IpAddress = ip,
                IssuedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                IsCurrent = true,
            };
            _sessions.Add(session);
        }
        user.LastLoginAt = DateTime.UtcNow;
        AddActivity(user, "login", $"Signed in from {deviceName}");
        SaveSessions();
        return token;
    }

    public AccUser? ValidateSession(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        lock (_gate)
        {
            var session = _sessions.FirstOrDefault(s => s.Token == token && s.ExpiresAt >= DateTime.UtcNow);
            if (session == null) return null;
            return FindById(session.UserId);
        }
    }

    public void DestroySession(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return;
        lock (_gate) _sessions.RemoveAll(s => s.Token == token);
        SaveSessions();
    }

    // ---- password helpers ---------------------------------------------------

    public static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(password + Convert.ToBase64String(salt)));
        return Convert.ToBase64String(salt) + "." + Convert.ToBase64String(hash);
    }

    public static bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(storedHash)) return false;
        var parts = storedHash.Split('.');
        if (parts.Length != 2) return false;
        var salt = parts[0];
        var expected = parts[1];
        var actual = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(password + salt)));
        return expected == actual;
    }

    public static AccProfileView ToView(AccUser user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        DisplayName = user.DisplayName,
        Email = user.Email,
        Avatar = user.Avatar,
        Bio = user.Bio,
        TwoFactorEnabled = user.TwoFactorEnabled,
        EmailVerified = user.EmailVerified,
        Locale = user.Locale,
        Theme = user.Theme,
        GoPoints = user.GoPoints,
        CreatedAt = user.CreatedAt,
        FriendIds = user.FriendIds,
        Devices = user.Devices,
        Subscriptions = user.Subscriptions,
        Activity = user.Activity,
    };
}
