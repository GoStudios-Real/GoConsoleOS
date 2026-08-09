using System;
using System.Collections.Generic;

namespace GoConsoleOS.GoConsole;

public static class ActivityFeed
{
    private static readonly List<ActivityEntry> _entries = new();

    public static IReadOnlyList<ActivityEntry> Entries => _entries.AsReadOnly();

    public static void AddGameLaunch(string gameTitle, string platform)
    {
        _entries.Insert(0, new ActivityEntry
        {
            Type = "game_launch",
            Title = $"Played {gameTitle}",
            Description = $"Launched on {platform}",
            Timestamp = DateTime.Now
        });
        Trim();
    }

    public static void AddAchievement(string gameTitle, string achievement)
    {
        _entries.Insert(0, new ActivityEntry
        {
            Type = "achievement",
            Title = $"Achievement: {achievement}",
            Description = $"Unlocked in {gameTitle}",
            Timestamp = DateTime.Now
        });
        Trim();
    }

    public static void AddScreenshot(string name)
    {
        _entries.Insert(0, new ActivityEntry
        {
            Type = "screenshot",
            Title = "Screenshot captured",
            Description = name,
            Timestamp = DateTime.Now
        });
        Trim();
    }

    public static void AddCustom(string title, string description)
    {
        _entries.Insert(0, new ActivityEntry
        {
            Type = "custom",
            Title = title,
            Description = description,
            Timestamp = DateTime.Now
        });
        Trim();
    }

    private static void Trim()
    {
        while (_entries.Count > 50)
            _entries.RemoveAt(_entries.Count - 1);
    }
}

public class ActivityEntry
{
    public string Type { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string TimeAgo
    {
        get
        {
            var span = DateTime.Now - Timestamp;
            if (span.TotalMinutes < 1) return "just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
            return $"{(int)span.TotalDays}d ago";
        }
    }
}
