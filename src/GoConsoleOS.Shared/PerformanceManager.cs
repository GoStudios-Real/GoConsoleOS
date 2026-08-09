using System.Diagnostics;
using System.Text.Json;
using GoConsoleOS.Shared.Models;

namespace GoConsoleOS.Shared;

public class PerformanceManager
{
    private PerformanceProfileData _data;
    private string _currentProfileId;
    private readonly string _configPath;
    public string CurrentMode => _currentProfileId;
    public event Action<string>? ProfileChanged;

    public PerformanceManager(string rootPath)
    {
        _configPath = ConfigReader.ResolvePath("system\\performance\\profiles.json");
        _data = LoadProfiles();
        _currentProfileId = _data.DefaultProfile;
    }

    private PerformanceProfileData LoadProfiles()
    {
        if (!File.Exists(_configPath))
        {
            var defaultData = CreateDefaultProfiles();
            SaveProfiles(defaultData);
            return defaultData;
        }

        try
        {
            var json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<PerformanceProfileData>(json) ?? CreateDefaultProfiles();
        }
        catch
        {
            return CreateDefaultProfiles();
        }
    }

    private static PerformanceProfileData CreateDefaultProfiles()
    {
        return new PerformanceProfileData
        {
            Version = "1.4.0",
            DefaultProfile = "balanced",
            GlobalProfiles = new List<PerformanceProfile>
            {
                new()
                {
                    Id = "quiet", Name = "Quiet Mode",
                    Description = "Reduces power consumption and fan noise.",
                    PowerPlanGuid = "a1841308-3541-4fab-bc81-f71556f20b4a",
                    Settings = new ProfileSettings
                    {
                        CpuMaxFrequencyPct = 50, GpuPowerLimitPct = 60,
                        FanCurve = "silent", BackgroundProcessPriority = "idle",
                        GameProcessPriority = "normal", Vsync = true,
                        FrameRateLimit = 30, ResolutionScale = 0.75
                    }
                },
                new()
                {
                    Id = "balanced", Name = "Balanced",
                    Description = "Default mode balancing performance and power.",
                    PowerPlanGuid = "381b4222-f694-41f0-9685-ff5bb260df2f",
                    Settings = new ProfileSettings
                    {
                        CpuMaxFrequencyPct = 80, GpuPowerLimitPct = 80,
                        FanCurve = "standard", BackgroundProcessPriority = "normal",
                        GameProcessPriority = "high", Vsync = false,
                        FrameRateLimit = 60, ResolutionScale = 1.0
                    }
                },
                new()
                {
                    Id = "turbo", Name = "Turbo Mode",
                    Description = "Maximum performance for demanding titles.",
                    PowerPlanGuid = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c",
                    Settings = new ProfileSettings
                    {
                        CpuMaxFrequencyPct = 100, GpuPowerLimitPct = 100,
                        FanCurve = "aggressive", BackgroundProcessPriority = "belowNormal",
                        GameProcessPriority = "realtime", Vsync = false,
                        FrameRateLimit = 0, ResolutionScale = 1.0
                    }
                }
            }
        };
    }

    private void SaveProfiles(PerformanceProfileData data)
    {
        try
        {
            var dir = Path.GetDirectoryName(_configPath);
            if (dir != null) Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configPath, json);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to save performance profiles: {ex.Message}");
        }
    }

    public PerformanceProfile? GetProfile(string id)
    {
        return _data.GlobalProfiles.FirstOrDefault(p => p.Id == id);
    }

    public PerformanceProfile? GetCurrentProfile()
    {
        return GetProfile(_currentProfileId);
    }

    public List<PerformanceProfile> GetAllProfiles()
    {
        return _data.GlobalProfiles.ToList();
    }

    public string? GetProfileForGame(string gameId)
    {
        var ovr = _data.PerGameOverrides.FirstOrDefault(o => o.GameId == gameId);
        return ovr?.OverrideProfile;
    }

    public bool SetProfile(string profileId)
    {
        if (GetProfile(profileId) == null) return false;

        _currentProfileId = profileId;
        ApplyProfile(profileId);
        ProfileChanged?.Invoke(profileId);
        Logger.Info($"Performance mode changed to: {profileId}");
        return true;
    }

    private void ApplyProfile(string profileId)
    {
        var profile = GetProfile(profileId);
        if (profile == null) return;

        try
        {
            if (!string.IsNullOrEmpty(profile.PowerPlanGuid))
            {
                var startInfo = new ProcessStartInfo("powercfg")
                {
                    Arguments = $"/setactive {profile.PowerPlanGuid}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = "runas"
                };

                try
                {
                    using var proc = Process.Start(startInfo);
                    proc?.WaitForExit(3000);
                }
                catch
                {
                    Logger.Warn("Could not set power plan (admin rights may be needed)");
                }
            }

            using var currentProcess = Process.GetCurrentProcess();
            var priority = profile.Settings.GameProcessPriority.ToLowerInvariant() switch
            {
                "idle" => ProcessPriorityClass.Idle,
                "belowNormal" => ProcessPriorityClass.BelowNormal,
                "normal" => ProcessPriorityClass.Normal,
                "high" => ProcessPriorityClass.High,
                "realtime" => ProcessPriorityClass.RealTime,
                _ => ProcessPriorityClass.Normal
            };
            currentProcess.PriorityClass = priority;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to apply performance profile: {ex.Message}");
        }
    }

    public bool CycleProfile()
    {
        var profiles = _data.GlobalProfiles;
        var idx = profiles.FindIndex(p => p.Id == _currentProfileId);
        idx = (idx + 1) % profiles.Count;
        return SetProfile(profiles[idx].Id);
    }
}
