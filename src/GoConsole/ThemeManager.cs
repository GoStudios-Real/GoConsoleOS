using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using GoConsoleOS.Shared;

namespace GoConsoleOS.GoConsole;

public class ThemeDefinition
{
    public string Name { get; set; } = "Custom";
    public string BackgroundDark { get; set; } = "#0D0D14";
    public string BackgroundMedium { get; set; } = "#14141F";
    public string BackgroundLight { get; set; } = "#141A2E";
    public string BackgroundCard { get; set; } = "#182040";
    public string AccentPrimary { get; set; } = "#0066FF";
    public string AccentSecondary { get; set; } = "#7B2DFF";
    public string AccentTertiary { get; set; } = "#FF4D8C";
    public string TextPrimary { get; set; } = "#F0F0FF";
    public string TextSecondary { get; set; } = "#8888AA";
    public string TextMuted { get; set; } = "#555577";
    public string BorderColor { get; set; } = "#1E2A55";
    public string FocusGlow { get; set; } = "#0066FF";
    public string SuccessColor { get; set; } = "#00E676";
    public string WarningColor { get; set; } = "#FFD600";
    public string ErrorColor { get; set; } = "#FF5252";
}

public static class ThemeManager
{
    private static readonly string ThemesDir;

    static ThemeManager()
    {
        ThemesDir = ConfigReader.ResolvePath("system\\themes");
        Directory.CreateDirectory(ThemesDir);
    }

    public static List<ThemeDefinition> PresetThemes { get; } = new()
    {
        new ThemeDefinition
        {
            Name = "GoConsole Dark",
            BackgroundDark = "#0D0D14", BackgroundMedium = "#14141F",
            BackgroundLight = "#141A2E", BackgroundCard = "#182040",
            AccentPrimary = "#0066FF", AccentSecondary = "#7B2DFF",
            AccentTertiary = "#FF4D8C", TextPrimary = "#F0F0FF",
            TextSecondary = "#8888AA", TextMuted = "#555577",
            BorderColor = "#1E2A55"
        },
        new ThemeDefinition
        {
            Name = "Amber Glow",
            BackgroundDark = "#0D0D08", BackgroundMedium = "#1A1A0E",
            BackgroundLight = "#2A2A14", BackgroundCard = "#222212",
            AccentPrimary = "#FFB900", AccentSecondary = "#FF8C00",
            AccentTertiary = "#FF6B35", TextPrimary = "#FFF8E7",
            TextSecondary = "#BBAA88", TextMuted = "#887766",
            BorderColor = "#3A3A1A"
        },
        new ThemeDefinition
        {
            Name = "Cyber Purple",
            BackgroundDark = "#0D0A1A", BackgroundMedium = "#14102E",
            BackgroundLight = "#1A1540", BackgroundCard = "#1E1848",
            AccentPrimary = "#B388FF", AccentSecondary = "#7C4DFF",
            AccentTertiary = "#E040FB", TextPrimary = "#F0EAFF",
            TextSecondary = "#9990CC", TextMuted = "#6655AA",
            BorderColor = "#2A2250"
        },
        new ThemeDefinition
        {
            Name = "Crimson",
            BackgroundDark = "#140A0A", BackgroundMedium = "#1E1010",
            BackgroundLight = "#2E1818", BackgroundCard = "#261414",
            AccentPrimary = "#FF5252", AccentSecondary = "#D32F2F",
            AccentTertiary = "#FF8A80", TextPrimary = "#FFF0F0",
            TextSecondary = "#BB8888", TextMuted = "#885555",
            BorderColor = "#3A2020"
        },
        new ThemeDefinition
        {
            Name = "Forest Green",
            BackgroundDark = "#0A140A", BackgroundMedium = "#102010",
            BackgroundLight = "#18301A", BackgroundCard = "#142814",
            AccentPrimary = "#00E676", AccentSecondary = "#00C853",
            AccentTertiary = "#69F0AE", TextPrimary = "#F0FFF0",
            TextSecondary = "#88BB88", TextMuted = "#558855",
            BorderColor = "#1A3A1A"
        },
        new ThemeDefinition
        {
            Name = "Ocean Blue",
            BackgroundDark = "#0A0D18", BackgroundMedium = "#0E1428",
            BackgroundLight = "#141E3A", BackgroundCard = "#121A30",
            AccentPrimary = "#448AFF", AccentSecondary = "#2979FF",
            AccentTertiary = "#82B1FF", TextPrimary = "#ECF0FF",
            TextSecondary = "#8899CC", TextMuted = "#5566AA",
            BorderColor = "#1E2850"
        },
    };

    public static string? CurrentThemeName { get; set; } = "GoConsole Dark";

    public static void ApplyTheme(ThemeDefinition theme)
    {
        try
        {
            var app = System.Windows.Application.Current;
            SetBrush(app, "BrushBackgroundDark", theme.BackgroundDark);
            SetBrush(app, "BrushBackgroundMedium", theme.BackgroundMedium);
            SetBrush(app, "BrushBackgroundLight", theme.BackgroundLight);
            SetBrush(app, "BrushBackgroundCard", theme.BackgroundCard);
            SetBrush(app, "BrushBackgroundCardHover", Lighten(theme.BackgroundCard, 0.12));
            SetBrush(app, "BrushAccentPrimary", theme.AccentPrimary);
            SetBrush(app, "BrushAccentSecondary", theme.AccentSecondary);
            SetBrush(app, "BrushAccentTertiary", theme.AccentTertiary);
            SetBrush(app, "BrushTextPrimary", theme.TextPrimary);
            SetBrush(app, "BrushTextSecondary", theme.TextSecondary);
            SetBrush(app, "BrushTextMuted", theme.TextMuted);
            SetBrush(app, "BrushBorder", theme.BorderColor);
            SetBrush(app, "BrushFocusGlow", theme.FocusGlow);
            SetBrush(app, "BrushSuccess", theme.SuccessColor);
            SetBrush(app, "BrushWarning", theme.WarningColor);
            SetBrush(app, "BrushError", theme.ErrorColor);
        }
        catch { }
    }

    private static void SetBrush(System.Windows.Application app, string key, string hex)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            app.Resources[key] = new SolidColorBrush(color);
        }
        catch { }
    }

    private static string Lighten(string hex, double factor)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            var r = (byte)Math.Min(255, color.R + (255 - color.R) * factor);
            var g = (byte)Math.Min(255, color.G + (255 - color.G) * factor);
            var b = (byte)Math.Min(255, color.B + (255 - color.B) * factor);
            return $"#{r:X2}{g:X2}{b:X2}";
        }
        catch { return hex; }
    }

    public static void SaveCustomTheme(ThemeDefinition theme)
    {
        var path = Path.Combine(ThemesDir, $"{SanitizeName(theme.Name)}.json");
        var json = System.Text.Json.JsonSerializer.Serialize(theme, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
        CurrentThemeName = theme.Name;
    }

    public static List<ThemeDefinition> LoadCustomThemes()
    {
        if (!Directory.Exists(ThemesDir)) return new List<ThemeDefinition>();
        var list = new List<ThemeDefinition>();
        foreach (var file in Directory.GetFiles(ThemesDir, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var theme = System.Text.Json.JsonSerializer.Deserialize<ThemeDefinition>(json);
                if (theme != null) list.Add(theme);
            }
            catch { }
        }
        return list;
    }

    public static List<ThemeDefinition> GetAllThemes()
    {
        var all = new List<ThemeDefinition>(PresetThemes);
        all.AddRange(LoadCustomThemes());
        return all;
    }

    private static string SanitizeName(string name)
    {
        foreach (var c in System.IO.Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }
}
