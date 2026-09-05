using System.Text;
using GoConsoleOS.Shared.Models;

namespace GoConsoleOS.Shared;

public class ConfigReader
{
    public static string? RootPath { get; private set; }

    public static void SetRootPath(string path)
    {
        RootPath = path;
    }

    public static string ResolvePath(string relativePath)
    {
        if (string.IsNullOrEmpty(RootPath))
            return Path.GetFullPath(relativePath);
        return Path.GetFullPath(Path.Combine(RootPath, relativePath));
    }

    public static InitConfig ReadInitConfig(string? path = null)
    {
        var cfg = new InitConfig();
        path ??= ResolvePath("boot\\init.cfg");

        if (!File.Exists(path))
        {
            Logger.Warn($"init.cfg not found at {path}, using defaults");
            return cfg;
        }

        try
        {
            var lines = File.ReadAllLines(path, Encoding.UTF8);
            string currentSection = "";

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith(';') || line.StartsWith('#'))
                    continue;

                if (line.StartsWith('[') && line.EndsWith(']'))
                {
                    currentSection = line[1..^1].ToLowerInvariant();
                    continue;
                }

                var eqIdx = line.IndexOf('=');
                if (eqIdx < 0) continue;

                var key = line[..eqIdx].Trim().ToLowerInvariant();
                var value = line[(eqIdx + 1)..].Trim();

                SetConfigValue(cfg, currentSection, key, value);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to read init.cfg: {ex.Message}");
        }

        return cfg;
    }

    private static void SetConfigValue(InitConfig cfg, string section, string key, string value)
    {
        try
        {
            switch (section)
            {
                case "general":
                    switch (key)
                    {
                        case "os_name": cfg.General.OsName = value; break;
                        case "version": cfg.General.Version = value; break;
                        case "auto_detect_drive": cfg.General.AutoDetectDrive = bool.Parse(value); break;
                        case "splash_duration_ms": cfg.General.SplashDurationMs = int.Parse(value); break;
                        case "verbose_logging": cfg.General.VerboseLogging = bool.Parse(value); break;
                    }
                    break;
                case "boot":
                    switch (key)
                    {
                        case "boot_mode": cfg.Boot.BootMode = value; break;
                        case "auto_launch_shell": cfg.Boot.AutoLaunchShell = bool.Parse(value); break;
                        case "shell_launch_delay_ms": cfg.Boot.ShellLaunchDelayMs = int.Parse(value); break;
                        case "shell_path": cfg.Boot.ShellPath = value; break;
                        case "splash_path": cfg.Boot.SplashPath = value; break;
                        case "min_ram_mb": cfg.Boot.MinRamMb = int.Parse(value); break;
                        case "min_usb_space_mb": cfg.Boot.MinUsbSpaceMb = int.Parse(value); break;
                    }
                    break;
                case "performance":
                    switch (key)
                    {
                        case "default_mode": cfg.Performance.DefaultMode = value; break;
                        case "integrate_power_plans": cfg.Performance.IntegratePowerPlans = bool.Parse(value); break;
                        case "balanced_plan_guid": cfg.Performance.BalancedPlanGuid = value; break;
                        case "turbo_plan_guid": cfg.Performance.TurboPlanGuid = value; break;
                        case "quiet_plan_guid": cfg.Performance.QuietPlanGuid = value; break;
                    }
                    break;
                case "paths":
                    switch (key)
                    {
                        case "profiles": cfg.Paths.Profiles = value; break;
                        case "library": cfg.Paths.Library = value; break;
                        case "plugins": cfg.Paths.Plugins = value; break;
                        case "assets": cfg.Paths.Assets = value; break;
                        case "logs": cfg.Paths.Logs = value; break;
                        case "cache": cfg.Paths.Cache = value; break;
                        case "app_data": cfg.Paths.AppData = value; break;
                    }
                    break;
                case "services":
                    switch (key)
                    {
                        case "enabled": cfg.Services.Enabled = value; break;
                        case "controller_poll_rate": cfg.Services.ControllerPollRate = int.Parse(value); break;
                        case "xinput_enabled": cfg.Services.XinputEnabled = bool.Parse(value); break;
                        case "perf_monitor_interval_ms": cfg.Services.PerfMonitorIntervalMs = int.Parse(value); break;
                    }
                    break;
                case "display":
                    switch (key)
                    {
                        case "resolution": cfg.Display.Resolution = value; break;
                        case "fullscreen": cfg.Display.Fullscreen = bool.Parse(value); break;
                        case "refresh_rate": cfg.Display.RefreshRate = value; break;
                        case "hardware_acceleration": cfg.Display.HardwareAcceleration = bool.Parse(value); break;
                        case "ui_scale": cfg.Display.UiScale = double.Parse(value); break;
                        case "wallpaper": cfg.Display.Wallpaper = value; break;
                    }
                    break;
                case "overlay":
                    switch (key)
                    {
                        case "enabled": cfg.Overlay.Enabled = bool.Parse(value); break;
                        case "guide_combo": cfg.Overlay.GuideCombo = value; break;
                        case "show_fps": cfg.Overlay.ShowFps = bool.Parse(value); break;
                        case "show_system_stats": cfg.Overlay.ShowSystemStats = bool.Parse(value); break;
                        case "overlay_opacity": cfg.Overlay.OverlayOpacity = double.Parse(value); break;
                    }
                    break;
                case "network":
                    switch (key)
                    {
                        case "enable_networking": cfg.Network.EnableNetworking = bool.Parse(value); break;
                        case "check_updates": cfg.Network.CheckUpdates = bool.Parse(value); break;
                        case "update_url": cfg.Network.UpdateUrl = value; break;
                    }
                    break;
                case "logging":
                    switch (key)
                    {
                        case "log_level": cfg.Logging.LogLevel = value; break;
                        case "max_log_size_mb": cfg.Logging.MaxLogSizeMb = int.Parse(value); break;
                        case "log_retention_days": cfg.Logging.LogRetentionDays = int.Parse(value); break;
                    }
                    break;
                case "sound":
                    switch (key)
                    {
                        case "enabled": cfg.Sound.Enabled = bool.Parse(value); break;
                        case "volume": cfg.Sound.Volume = int.Parse(value); break;
                    }
                    break;
                case "theme":
                    switch (key)
                    {
                        case "accent_primary": cfg.Theme.AccentPrimary = value; break;
                        case "accent_secondary": cfg.Theme.AccentSecondary = value; break;
                        case "accent_tertiary": cfg.Theme.AccentTertiary = value; break;
                        case "background_dark": cfg.Theme.BackgroundDark = value; break;
                        case "background_medium": cfg.Theme.BackgroundMedium = value; break;
                        case "background_light": cfg.Theme.BackgroundLight = value; break;
                        case "background_card": cfg.Theme.BackgroundCard = value; break;
                    }
                    break;
                case "music":
                    switch (key)
                    {
                        case "genre": cfg.Music.Genre = value; break;
                        case "folder": cfg.Music.Folder = value; break;
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"Failed to parse config [{section}] {key}={value}: {ex.Message}");
        }
    }
}
