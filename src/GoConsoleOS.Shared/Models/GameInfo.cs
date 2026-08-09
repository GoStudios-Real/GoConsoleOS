using System;
using System.Text.Json.Serialization;

namespace GoConsoleOS.Shared.Models;

public class GameInfo
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Platform { get; set; } = "";
    public string ExecutablePath { get; set; } = "";
    public string? WorkingDirectory { get; set; }
    public string? CoverArtPath { get; set; }
    public string? PlatformIconPath { get; set; }
    public List<string> Genres { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public int PlaytimeMinutes { get; set; }
    public DateTime? LastPlayed { get; set; }
    public bool IsFavorite { get; set; }
    public bool IsInstalled { get; set; } = true;
    public string? LaunchArguments { get; set; }
    public string? StoreId { get; set; }

    public string GetStoreUrl()
    {
        if (!string.IsNullOrEmpty(StoreUrl))
            return StoreUrl;

        var slug = Title.ToLowerInvariant()
            .Replace(":", "").Replace(" ", "-")
            .Replace("--", "-").Trim('-');

        return Platform switch
        {
            "Steam" when !string.IsNullOrEmpty(StoreId) => $"https://store.steampowered.com/app/{StoreId}",
            "Steam" => $"https://store.steampowered.com/search/?term={Uri.EscapeDataString(Title)}",
            "Epic Games" => $"https://store.epicgames.com/en-US/p/{slug}",
            "GOG" => $"https://www.gog.com/en/game/{slug}",
            "Xbox" => $"https://www.xbox.com/en-US/games/store/{slug}",
            _ => $"https://www.google.com/search?q={Uri.EscapeDataString(Title + " " + Platform + " game")}"
        };
    }

    public string? StoreUrl { get; set; }
}

public class LibraryData
{
    public string Version { get; set; } = "1.4.0";
    public DateTime LastScanned { get; set; }
    public List<GameInfo> Games { get; set; } = new();
}

public enum GamePlatform
{
    Steam,
    EpicGames,
    Xbox,
    GOG,
    Custom
}
