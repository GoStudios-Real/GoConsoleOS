using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace GoConsoleOS.Shared;

/// <summary>
/// Persistent key-value settings database stored at system\settings.json.
/// Backs runtime user preferences (sound, display, toggles, accent color, ...).
/// </summary>
public static class SettingsStore
{
    private static readonly object _lock = new();
    private static string _path = "";
    private static Dictionary<string, string> _values = new();

    public static void Initialize(string rootPath)
    {
        lock (_lock)
        {
            _path = Path.Combine(rootPath, "system", "settings.json");
            Load();
        }
    }

    public static string? Get(string key, string? def = null)
    {
        lock (_lock) { return _values.TryGetValue(key, out var v) ? v : def; }
    }

    public static bool GetBool(string key, bool def = false)
        => bool.TryParse(Get(key), out var b) ? b : def;

    public static int GetInt(string key, int def = 0)
        => int.TryParse(Get(key), out var i) ? i : def;

    public static double GetDouble(string key, double def = 0)
        => double.TryParse(Get(key), out var d) ? d : def;

    public static void Set(string key, string value)
    {
        lock (_lock)
        {
            _values[key] = value;
            Save();
        }
    }

    public static void SetBool(string key, bool value) => Set(key, value ? "true" : "false");
    public static void SetInt(string key, int value) => Set(key, value.ToString());
    public static void SetDouble(string key, double value) => Set(key, value.ToString("0.###"));

    private static void Load()
    {
        try
        {
            if (!File.Exists(_path)) return;
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(_path));
            if (dict != null) _values = dict;
        }
        catch { }
    }

    private static void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(_path,
                JsonSerializer.Serialize(_values, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}
