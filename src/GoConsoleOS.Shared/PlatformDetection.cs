using Microsoft.Win32;

namespace GoConsoleOS.Shared;

public static class PlatformDetection
{
    public static bool IsWindows11()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (key?.GetValue("CurrentBuild") is string build && int.TryParse(build, out var b))
                return b >= 22000;
        }
        catch { }
        return false;
    }

    public static bool IsSteamInstalled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam");
            return key?.GetValue("SteamPath") != null;
        }
        catch { return false; }
    }

    public static string? GetSteamPath()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam");
            return key?.GetValue("SteamPath")?.ToString();
        }
        catch { return null; }
    }

    public static bool IsEpicInstalled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\EpicGames\EpicGamesLauncher");
            return key != null;
        }
        catch { return false; }
    }

    public static string? GetEpicPath()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\EpicGames\EpicGamesLauncher");
            return key?.GetValue("AppDataPath")?.ToString();
        }
        catch { return null; }
    }

    public static bool IsXboxInstalled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\GameBar");
            return key != null;
        }
        catch { return false; }
    }

    public static bool IsGogInstalled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\GOG.com\GalaxyClient");
            return key != null;
        }
        catch
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\GOG.com\GalaxyClient");
                return key != null;
            }
            catch { return false; }
        }
    }

    public static string? GetGogPath()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\GOG.com\GalaxyClient\paths");
            return key?.GetValue("GOG Galaxy")?.ToString();
        }
        catch
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\GOG.com\GalaxyClient\paths");
                return key?.GetValue("GOG Galaxy")?.ToString();
            }
            catch { return null; }
        }
    }

    public static bool IsBattlenetInstalled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Blizzard Entertainment\Battle.net");
            return key != null;
        }
        catch
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Blizzard Entertainment\Battle.net");
                return key != null;
            }
            catch { return false; }
        }
    }

    public static bool IsEaAppInstalled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Electronic Arts\EA Desktop");
            return key != null;
        }
        catch
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Electronic Arts\EA Desktop");
                return key != null;
            }
            catch { return false; }
        }
    }

    public static bool IsUbisoftInstalled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Ubisoft\Launcher");
            return key != null;
        }
        catch
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Ubisoft\Launcher");
                return key != null;
            }
            catch { return false; }
        }
    }

    public static Dictionary<string, bool> GetInstalledPlatforms()
    {
        return new()
        {
            ["Steam"] = IsSteamInstalled(),
            ["Epic Games"] = IsEpicInstalled(),
            ["Xbox"] = IsXboxInstalled(),
            ["GOG"] = IsGogInstalled(),
            ["Battle.net"] = IsBattlenetInstalled(),
            ["EA App"] = IsEaAppInstalled(),
            ["Ubisoft Connect"] = IsUbisoftInstalled()
        };
    }

    public static string GetCpuName()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
            return key?.GetValue("ProcessorNameString")?.ToString()?.Trim() ?? "Unknown CPU";
        }
        catch { return "Unknown CPU"; }
    }

    public static string GetCpuTier()
    {
        var name = GetCpuName();
        if (name.Contains("i9", StringComparison.OrdinalIgnoreCase)) return "Intel Core i9";
        if (name.Contains("i7", StringComparison.OrdinalIgnoreCase)) return "Intel Core i7";
        if (name.Contains("i5", StringComparison.OrdinalIgnoreCase)) return "Intel Core i5";
        if (name.Contains("Ryzen 9", StringComparison.OrdinalIgnoreCase)) return "AMD Ryzen 9";
        if (name.Contains("Ryzen 7", StringComparison.OrdinalIgnoreCase)) return "AMD Ryzen 7";
        if (name.Contains("Ryzen 5", StringComparison.OrdinalIgnoreCase)) return "AMD Ryzen 5";
        if (name.Contains("Ryzen 3", StringComparison.OrdinalIgnoreCase)) return "AMD Ryzen 3";
        if (name.Contains("AMD", StringComparison.OrdinalIgnoreCase)) return "AMD CPU";
        if (name.Contains("Intel", StringComparison.OrdinalIgnoreCase)) return "Intel CPU";
        return "Unsupported CPU";
    }

    public static bool IsCpuSupported()
    {
        var tier = GetCpuTier();
        return tier != "Unsupported CPU";
    }

    public static string GetCpuRecommendation()
    {
        return GetCpuTier() switch
        {
            "Intel Core i9" => "Excellent - max settings, 4K-ready",
            "Intel Core i7" => "Great - high settings, smooth 1440p",
            "Intel Core i5" => "Good - medium/high settings at 1080p",
            "AMD Ryzen 9" => "Excellent - max settings, 4K-ready",
            "AMD Ryzen 7" => "Great - high settings, smooth 1440p",
            "AMD Ryzen 5" => "Good - medium/high settings at 1080p",
            "AMD Ryzen 3" => "Entry - light gaming and apps",
            "AMD CPU" => "Good - general gaming ready",
            "Intel CPU" => "Good - general gaming ready",
            _ => "May not be supported - basic mode only"
        };
    }
}
