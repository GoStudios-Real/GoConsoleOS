using System.Collections.Generic;

namespace GoConsoleOS.Shared.Acc;

/// <summary>A GoConsoleOS account (part of the ACC - Account, Cloud &amp; Community system).</summary>
public class AccUser
{
    public string Id { get; set; } = "";
    public string Username { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? Email { get; set; }
    public string PasswordHash { get; set; } = "";
    public string? Avatar { get; set; }
    public string Bio { get; set; } = "";
    public bool TwoFactorEnabled { get; set; }
    public bool EmailVerified { get; set; }
    public string Locale { get; set; } = "en-US";
    public string Theme { get; set; } = "default";
    public long GoPoints { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public List<string> FriendIds { get; set; } = new();
    public List<AccDevice> Devices { get; set; } = new();
    public List<AccSubscription> Subscriptions { get; set; } = new();
    public List<AccActivity> Activity { get; set; } = new();
}

public class AccSession
{
    public string Token { get; set; } = "";
    public string UserId { get; set; } = "";
    public string DeviceName { get; set; } = "";
    public string IpAddress { get; set; } = "";
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(30);
    public bool IsCurrent { get; set; }
}

public class AccDevice
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "console"; // console | usb | android | web
    public string Os { get; set; } = "GoConsoleOS";
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    public string IpAddress { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string City { get; set; } = "";
    public string Country { get; set; } = "";
}

public class AccSubscription
{
    public string Id { get; set; } = "";
    public string Plan { get; set; } = "free";
    public string Tier { get; set; } = "free";
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? PaymentMethod { get; set; }
    public int DurationDays { get; set; }
    public string Source { get; set; } = "manual"; // manual | giftcard
    public string? GiftCardCode { get; set; }
}

public class GiftCard
{
    public string Code { get; set; } = "";
    public string Tier { get; set; } = "pro";
    public int DurationDays { get; set; } = 30;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRedeemed { get; set; }
    public string? RedeemedBy { get; set; }
    public DateTime? RedeemedAt { get; set; }
}

/// <summary>A GoConsole Game Pass subscription plan (tier).</summary>
public class GamePassPlan
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Emoji { get; set; } = "🎮";
    public string Color { get; set; } = "#00C9DB";
    public string[] Perks { get; set; } = Array.Empty<string>();
}

public static class GamePassCatalog
{
    public static readonly IReadOnlyList<GamePassPlan> Plans = new[]
    {
        new GamePassPlan { Id = "free", Name = "Game Pass Free", Emoji = "🎮", Color = "#4A4F5A",
            Perks = new[] { "Free games rotation", "Community features", "Basic cloud saves" } },
        new GamePassPlan { Id = "pro", Name = "Game Pass Pro", Emoji = "🟢", Color = "#2ECC71",
            Perks = new[] { "Everything in Free", "Play all Game Pass titles", "Exclusive deals & rewards" } },
        new GamePassPlan { Id = "plus", Name = "Game Pass Plus", Emoji = "🔵", Color = "#3D9BFF",
            Perks = new[] { "Everything in Pro", "Early access to new releases", "1,000 Go Points monthly" } },
        new GamePassPlan { Id = "premium", Name = "Game Pass Premium", Emoji = "🟣", Color = "#7C5CFF",
            Perks = new[] { "Everything in Plus", "Cloud streaming & remote play", "Day-one triple-A titles" } },
        new GamePassPlan { Id = "ultimate", Name = "Game Pass Ultimate", Emoji = "👑", Color = "#FFC800",
            Perks = new[] { "Everything in Premium", "All DLC & expansions included", "Controller + GoPoints bonuses", "VIP support & giveaways" } },
    };

    public static GamePassPlan Find(string? id)
        => Plans.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase))
           ?? Plans[0];
}

public class AccActivity
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "info"; // login | purchase | security | achievement | social
    public string Message { get; set; } = "";
    public DateTime At { get; set; } = DateTime.UtcNow;
}

/// <summary>What ACC exposes to the outside world after login.</summary>
public class AccProfileView
{
    public string Id { get; set; } = "";
    public string Username { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? Email { get; set; }
    public string? Avatar { get; set; }
    public string Bio { get; set; } = "";
    public bool TwoFactorEnabled { get; set; }
    public bool EmailVerified { get; set; }
    public string Locale { get; set; } = "en-US";
    public string Theme { get; set; } = "default";
    public long GoPoints { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> FriendIds { get; set; } = new();
    public List<AccDevice> Devices { get; set; } = new();
    public List<AccSubscription> Subscriptions { get; set; } = new();
    public List<AccActivity> Activity { get; set; } = new();
}
