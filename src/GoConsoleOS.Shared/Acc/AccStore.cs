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
    private readonly List<GiftCard> _giftCards = new();

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
            var giftCardsPath = Path.Combine(_dataDir, "giftcards.json");
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
            if (File.Exists(giftCardsPath))
            {
                try
                {
                    var giftCards = JsonSerializer.Deserialize<List<GiftCard>>(File.ReadAllText(giftCardsPath));
                    if (giftCards != null) _giftCards.AddRange(giftCards);
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

    private void SaveGiftCards()
    {
        try
        {
            lock (_gate)
            {
                File.WriteAllText(Path.Combine(_dataDir, "giftcards.json"),
                    JsonSerializer.Serialize(_giftCards, new JsonSerializerOptions { WriteIndented = true }));
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

    /// <summary>
    /// The built-in "console" system account that owns the subscription used by
    /// the on-device Game Pass screen. Created lazily so gift cards redeemed on
    /// the console extend this account's Game Pass.
    /// </summary>
    public AccUser GetOrCreateConsoleAccount()
    {
        var existing = FindByUsername("console");
        if (existing != null) return existing;

        var console = new AccUser
        {
            Id = Guid.NewGuid().ToString("N"),
            Username = "console",
            DisplayName = "GoConsoleOS Console",
            PasswordHash = "",
            CreatedAt = DateTime.UtcNow,
        };
        console.Devices.Add(new AccDevice { Id = Guid.NewGuid().ToString("N"), Name = "GoConsoleOS Console", Kind = "console", Os = "GoConsoleOS" });
        console.Subscriptions.Add(new AccSubscription { Id = Guid.NewGuid().ToString("N"), Plan = "free", Tier = "free" });

        lock (_gate) _users["console"] = console;
        SaveUsers();
        return console;
    }    public AccUser CreateUser(string username, string displayName, string password, string? email = null)
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

    // ---- gift cards ---------------------------------------------------------

    private const string GiftCardAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    /// <summary>Generate a batch of redeemable Game Pass gift card codes.</summary>
    public IReadOnlyList<GiftCard> GenerateGiftCards(string tier, int durationDays, int count = 1)
    {
        var plan = GamePassCatalog.Find(tier);
        var created = new List<GiftCard>();
        lock (_gate)
        {
            for (var i = 0; i < count; i++)
            {
                var card = new GiftCard
                {
                    Code = GenerateGiftCode(),
                    Tier = plan.Id,
                    DurationDays = Math.Max(1, durationDays),
                    CreatedAt = DateTime.UtcNow,
                };
                _giftCards.Add(card);
                created.Add(card);
            }
            SaveGiftCards();
        }
        Logger.Info($"ACC: generated {count} gift card code(s) for {plan.Id}");
        return created;
    }

    public IReadOnlyList<GiftCard> ListGiftCards()
    {
        lock (_gate) return _giftCards.ToList();
    }

    public string? FindGiftCardTier(string code)
    {
        lock (_gate)
        {
            var card = _giftCards.FirstOrDefault(c =>
                string.Equals(c.Code, code.Trim(), StringComparison.OrdinalIgnoreCase));
            return card == null || card.IsRedeemed ? null : card.Tier;
        }
    }

    /// <summary>Redeem a gift card code, extending/upgrading the user's subscription.</summary>
    public (bool Ok, string Message, AccSubscription? Subscription) RedeemGiftCard(AccUser user, string code)
    {
        code = (code ?? "").Trim();
        if (string.IsNullOrWhiteSpace(code))
            return (false, "Enter a gift card code.", null);

        lock (_gate)
        {
            var card = _giftCards.FirstOrDefault(c =>
                string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase));
            if (card == null) return (false, "That gift card code is invalid.", null);
            if (card.IsRedeemed) return (false, "That gift card code has already been used.", null);

            card.IsRedeemed = true;
            card.RedeemedBy = user.Username;
            card.RedeemedAt = DateTime.UtcNow;
            SaveGiftCards();

            var plan = GamePassCatalog.Find(card.Tier);
            var sub = AddSubscription(user, card.Tier, card.DurationDays, "giftcard", card.Code);
            AddActivity(user, "purchase", $"Redeemed {plan.Name} gift card");
            return (true, $"Redeemed {plan.Name} for {card.DurationDays} day(s)!", sub);
        }
    }

    private static string GenerateGiftCode()
    {
        var rnd = RandomNumberGenerator.GetBytes(12);
        var sb = new StringBuilder();
        for (var i = 0; i < 16; i++)
        {
            if (i > 0 && i % 4 == 0) sb.Append('-');
            sb.Append(GiftCardAlphabet[rnd[i % rnd.Length] % GiftCardAlphabet.Length]);
        }
        return sb.ToString();
    }

    // ---- subscriptions ------------------------------------------------------

    /// <summary>
    /// Add (or extend) a subscription. If the user already has an active
    /// subscription the new duration is stacked on top instead of replacing it,
    /// and the tier is upgraded if the new plan is higher.
    /// </summary>
    public AccSubscription AddSubscription(AccUser user, string tier, int durationDays, string source = "manual", string? giftCardCode = null)
    {
        var plan = GamePassCatalog.Find(tier);
        durationDays = Math.Max(1, durationDays);

        var active = user.Subscriptions.FirstOrDefault(s => s.IsActive && (s.ExpiresAt == null || s.ExpiresAt >= DateTime.UtcNow));

        AccSubscription sub;
        if (active != null)
        {
            // keep whichever tier is higher when stacking
            if (TierRank(plan.Id) >= TierRank(active.Tier))
            {
                active.Plan = plan.Id;
                active.Tier = plan.Id;
                active.Source = source;
                if (giftCardCode != null) active.GiftCardCode = giftCardCode;
            }
            active.DurationDays += durationDays;
            active.ExpiresAt = active.ExpiresAt?.AddDays(durationDays) ?? DateTime.UtcNow.AddDays(durationDays);
            active.IsActive = true;
            sub = active;
        }
        else
        {
            sub = new AccSubscription
            {
                Id = Guid.NewGuid().ToString("N"),
                Plan = plan.Id,
                Tier = plan.Id,
                StartedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(durationDays),
                IsActive = true,
                DurationDays = durationDays,
                Source = source,
                GiftCardCode = giftCardCode,
            };
            user.Subscriptions.Add(sub);
        }

        SaveUser(user);
        Logger.Info($"ACC: subscription {plan.Id} for {user.Username} +{durationDays}d");
        return sub;
    }

    private static int TierRank(string? tier)
    {
        for (var i = 0; i < GamePassCatalog.Plans.Count; i++)
        {
            if (string.Equals(GamePassCatalog.Plans[i].Id, tier, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return 0;
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
