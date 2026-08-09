using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace GoConsoleOS.Shared;

public class AchievementDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Icon { get; set; } = "🏆";
    public string Reward { get; set; } = "10 pts";
    public int Threshold { get; set; } = 1;
}

/// <summary>
/// Persistent achievement tracker stored at system\achievements.json.
/// Raises Unlocked when a new achievement is earned so the shell can toast it.
/// </summary>
public static class AchievementManager
{
    private static string _path = "";
    private static HashSet<string> _unlocked = new();
    private static Dictionary<string, int> _counters = new();

    public static event Action<AchievementDefinition>? Unlocked;

    public static List<AchievementDefinition> Definitions { get; } = new()
    {
        new() { Id = "first_launch", Name = "First Launch", Description = "Launch your first game", Icon = "🚀", Reward = "10 pts", Threshold = 1 },
        new() { Id = "screenshot_master", Name = "Screenshot Master", Description = "Take 10 screenshots", Icon = "📷", Reward = "25 pts", Threshold = 10 },
        new() { Id = "explorer", Name = "Explorer", Description = "Visit 8 sections of the console", Icon = "🧭", Reward = "15 pts", Threshold = 8 },
        new() { Id = "social_butterfly", Name = "Social Butterfly", Description = "Send 5 messages", Icon = "🦋", Reward = "20 pts", Threshold = 5 },
        new() { Id = "store_shopper", Name = "Store Shopper", Description = "Wishlist 3 items", Icon = "🛒", Reward = "15 pts", Threshold = 3 },
        new() { Id = "night_owl", Name = "Night Owl", Description = "Enable Night Mode", Icon = "🌙", Reward = "5 pts", Threshold = 1 },
        new() { Id = "network_champion", Name = "Network Champion", Description = "Run a network test", Icon = "🌐", Reward = "10 pts", Threshold = 1 },
        new() { Id = "achievement_hunter", Name = "Achievement Hunter", Description = "View this page", Icon = "🏆", Reward = "5 pts", Threshold = 1 },
    };

    public static void Initialize(string rootPath)
    {
        _path = Path.Combine(rootPath, "system", "achievements.json");
        try
        {
            if (!File.Exists(_path)) return;
            var save = JsonSerializer.Deserialize<AchievementSave>(File.ReadAllText(_path));
            if (save != null)
            {
                _unlocked = save.Unlocked ?? new HashSet<string>();
                _counters = save.Counters ?? new Dictionary<string, int>();
            }
        }
        catch { }
    }

    public static bool IsUnlocked(string id) => _unlocked.Contains(id);
    public static int UnlockedCount => Definitions.Count(d => IsUnlocked(d.Id));
    public static int GetCounter(string key) => _counters.TryGetValue(key, out var v) ? v : 0;

    public static void Unlock(string id)
    {
        if (_unlocked.Contains(id)) return;
        _unlocked.Add(id);
        Save();
        var def = Definitions.FirstOrDefault(d => d.Id == id);
        if (def != null) Unlocked?.Invoke(def);
    }

    public static void AddProgress(string counter, string achievementId)
    {
        _counters[counter] = GetCounter(counter) + 1;
        var def = Definitions.FirstOrDefault(d => d.Id == achievementId);
        if (def != null && _counters[counter] >= def.Threshold)
            Unlock(achievementId);
        Save();
    }

    public static void RecordVisit(string section)
    {
        _counters["visited:" + section] = 1;
        var explorer = Definitions.FirstOrDefault(d => d.Id == "explorer");
        if (explorer != null && _counters.Count(k => k.Key.StartsWith("visited:")) >= explorer.Threshold)
            Unlock("explorer");
        Save();
    }

    private static void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(_path,
                JsonSerializer.Serialize(new AchievementSave { Unlocked = _unlocked, Counters = _counters },
                    new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    private sealed class AchievementSave
    {
        public HashSet<string>? Unlocked { get; set; }
        public Dictionary<string, int>? Counters { get; set; }
    }
}
