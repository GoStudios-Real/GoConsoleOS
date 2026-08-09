using System.Text.Json.Serialization;

namespace GoConsoleOS.Shared.Models;

public class UserProfile
{
    public string Username { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? AvatarPath { get; set; }
    public string? PasswordHash { get; set; }
    public string? Email { get; set; }
    public string Theme { get; set; } = "default";
    public string PerformanceMode { get; set; } = "balanced";
    public string? ControllerPreset { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }
    public int TotalPlaytimeMinutes { get; set; }
    public bool IsGuest { get; set; }
    public List<string> FavoriteGames { get; set; } = new();
    public Dictionary<string, UserAchievement> Achievements { get; set; } = new();
    public UserSettings Settings { get; set; } = new();
}

public class UserAchievement
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsUnlocked { get; set; }
    public DateTime? UnlockedAt { get; set; }
    public string? IconPath { get; set; }
}

public class UserSettings
{
    public bool EnableOverlay { get; set; } = true;
    public bool ShowFps { get; set; } = true;
    public bool VibrateController { get; set; } = true;
    public int ControllerVibrationStrength { get; set; } = 75;
    public string Language { get; set; } = "en-US";
    public bool AutoLaunchOnStartup { get; set; }
}
