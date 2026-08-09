using System.Text.Json.Serialization;

namespace GoConsoleOS.Shared.Models;

public class PerformanceProfileData
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.4.0";
    [JsonPropertyName("default_profile")]
    public string DefaultProfile { get; set; } = "balanced";
    [JsonPropertyName("global_profiles")]
    public List<PerformanceProfile> GlobalProfiles { get; set; } = new();
    [JsonPropertyName("per_game_overrides")]
    public List<PerGameOverride> PerGameOverrides { get; set; } = new();
}

public class PerformanceProfile
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string PowerPlanGuid { get; set; } = "";
    public ProfileSettings Settings { get; set; } = new();
}

public class ProfileSettings
{
    [JsonPropertyName("cpu_max_frequency_pct")]
    public int CpuMaxFrequencyPct { get; set; } = 80;
    [JsonPropertyName("gpu_power_limit_pct")]
    public int GpuPowerLimitPct { get; set; } = 80;
    [JsonPropertyName("fan_curve")]
    public string FanCurve { get; set; } = "standard";
    [JsonPropertyName("background_process_priority")]
    public string BackgroundProcessPriority { get; set; } = "normal";
    [JsonPropertyName("game_process_priority")]
    public string GameProcessPriority { get; set; } = "high";
    public bool Vsync { get; set; }
    [JsonPropertyName("frame_rate_limit")]
    public int FrameRateLimit { get; set; }
    [JsonPropertyName("resolution_scale")]
    public double ResolutionScale { get; set; } = 1.0;
}

public class PerGameOverride
{
    [JsonPropertyName("game_id")]
    public string GameId { get; set; } = "";
    [JsonPropertyName("game_name")]
    public string GameName { get; set; } = "";
    [JsonPropertyName("override_profile")]
    public string OverrideProfile { get; set; } = "";
    public string? Notes { get; set; }
}
