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
