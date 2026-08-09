namespace GoConsoleOS.Shared.Models;

public class InitConfig
{
    public GeneralConfig General { get; set; } = new();
    public BootConfig Boot { get; set; } = new();
    public PerformanceConfig Performance { get; set; } = new();
    public PathConfig Paths { get; set; } = new();
    public ServiceConfig Services { get; set; } = new();
    public DisplayConfig Display { get; set; } = new();
    public OverlayConfig Overlay { get; set; } = new();
    public NetworkConfig Network { get; set; } = new();
    public LoggingConfig Logging { get; set; } = new();
    public SoundConfig Sound { get; set; } = new();
    public ThemeConfig Theme { get; set; } = new();
    public MusicConfig Music { get; set; } = new();
}

public class ThemeConfig
{
    public string? AccentPrimary { get; set; }
    public string? AccentSecondary { get; set; }
    public string? AccentTertiary { get; set; }
    public string? BackgroundDark { get; set; }
    public string? BackgroundMedium { get; set; }
    public string? BackgroundLight { get; set; }
    public string? BackgroundCard { get; set; }
}

public class MusicConfig
{
    public string Genre { get; set; } = "";
    public string Folder { get; set; } = "system\\music";
}

public class SoundConfig
{
    public bool Enabled { get; set; } = true;
    public int Volume { get; set; } = 75;
}

public class GeneralConfig
{
    public string OsName { get; set; } = "GoConsoleOS";
    public string Version { get; set; } = "1.8.0";
    public bool AutoDetectDrive { get; set; } = true;
    public int SplashDurationMs { get; set; } = 5000;
    public bool VerboseLogging { get; set; }
}

public class BootConfig
{
    public string BootMode { get; set; } = "ask";
    public bool AutoLaunchShell { get; set; } = true;
    public int ShellLaunchDelayMs { get; set; } = 1000;
    public string ShellPath { get; set; } = "launcher\\GoConsole.exe";
    public string SplashPath { get; set; } = "boot\\splash.png";
    public int MinRamMb { get; set; } = 4096;
    public int MinUsbSpaceMb { get; set; } = 512;
}

public class PerformanceConfig
{
    public string DefaultMode { get; set; } = "balanced";
    public bool IntegratePowerPlans { get; set; } = true;
    public string BalancedPlanGuid { get; set; } = "381b4222-f694-41f0-9685-ff5bb260df2f";
    public string TurboPlanGuid { get; set; } = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";
    public string QuietPlanGuid { get; set; } = "a1841308-3541-4fab-bc81-f71556f20b4a";
}

public class PathConfig
{
    public string Profiles { get; set; } = "profiles";
    public string Library { get; set; } = "launcher\\library";
    public string Plugins { get; set; } = "plugins";
    public string Assets { get; set; } = "assets";
    public string Logs { get; set; } = "system\\logs";
    public string Cache { get; set; } = "system\\cache";
    public string AppData { get; set; } = "apps";
}

public class ServiceConfig
{
    public string Enabled { get; set; } = "controller,performance_monitor,system_watchdog";
    public int ControllerPollRate { get; set; } = 60;
    public bool XinputEnabled { get; set; } = true;
    public bool MouseEmulation { get; set; } = true;
    public int PerfMonitorIntervalMs { get; set; } = 1000;
}

public class DisplayConfig
{
    public string Resolution { get; set; } = "auto";
    public bool Fullscreen { get; set; } = true;
    public string RefreshRate { get; set; } = "auto";
    public bool HardwareAcceleration { get; set; } = true;
    public double UiScale { get; set; } = 1.0;
    public string Wallpaper { get; set; } = "default";
}

public class OverlayConfig
{
    public bool Enabled { get; set; } = true;
    public string GuideCombo { get; set; } = "Guide";
    public bool ShowFps { get; set; } = true;
    public bool ShowSystemStats { get; set; } = true;
    public double OverlayOpacity { get; set; } = 0.85;
}

public class NetworkConfig
{
    public bool EnableNetworking { get; set; } = true;
    public bool CheckUpdates { get; set; } = true;
    public string UpdateUrl { get; set; } = "https://updates.goconsoleos.com";
}

public class LoggingConfig
{
    public string LogLevel { get; set; } = "info";
    public int MaxLogSizeMb { get; set; } = 10;
    public int LogRetentionDays { get; set; } = 30;
}
