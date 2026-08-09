using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GoConsoleOS.GoConsole;

public static class WallpaperManager
{
    private static string? _dir;

    private static string WallpaperDir(string rootPath)
    {
        _dir ??= Path.Combine(rootPath, "system", "wallpapers");
        Directory.CreateDirectory(_dir);
        return _dir;
    }

    public static string GetCurrentPath(string rootPath)
    {
        var dir = WallpaperDir(rootPath);
        EnsurePresets(dir);

        var cfgPath = Path.Combine(dir, "current.txt");
        var selected = "default";
        if (File.Exists(cfgPath))
        {
            var raw = File.ReadAllText(cfgPath).Trim();
            if (!string.IsNullOrEmpty(raw)) selected = raw;
        }

        if (File.Exists(selected)) return selected;

        var preset = Path.Combine(dir, selected + ".png");
        if (File.Exists(preset)) return preset;

        return Path.Combine(dir, "default.png");
    }

    public static void SetCurrent(string rootPath, string selection)
    {
        var dir = WallpaperDir(rootPath);
        File.WriteAllText(Path.Combine(dir, "current.txt"), selection.Trim());
    }

    private static void EnsurePresets(string dir)
    {
        if (!File.Exists(Path.Combine(dir, "default.png")))
            Render(Path.Combine(dir, "default.png"), Preset.Midnight);
        if (!File.Exists(Path.Combine(dir, "aurora.png")))
            Render(Path.Combine(dir, "aurora.png"), Preset.Aurora);
        if (!File.Exists(Path.Combine(dir, "minimal.png")))
            Render(Path.Combine(dir, "minimal.png"), Preset.Minimal);
    }

    private enum Preset { Midnight, Aurora, Minimal }

    private static void Render(string path, Preset preset)
    {
        const int w = 2560;
        const int h = 1440;

        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            var baseBrush = new LinearGradientBrush(
                Color.FromRgb(0x0E, 0x0E, 0x1C),
                Color.FromRgb(0x2A, 0x2A, 0x48),
                new Point(0, 0), new Point(1, 1));
            dc.DrawRectangle(baseBrush, null, new Rect(0, 0, w, h));

            switch (preset)
            {
                case Preset.Midnight:
                    Glow(dc, w, h, 0.18 * w, 0.20 * h, 0.85 * w, Color.FromArgb(170, 0x00, 0xC9, 0xDB));
                    Glow(dc, w, h, 0.85 * w, 0.85 * h, 0.75 * w, Color.FromArgb(160, 0x7B, 0x2D, 0xFF));
                    Glow(dc, w, h, 0.15 * w, 0.90 * h, 0.45 * w, Color.FromArgb(90, 0xFF, 0x4D, 0x8C));
                    break;
                case Preset.Aurora:
                    Glow(dc, w, h, 0.50 * w, 0.30 * h, 1.15 * w, Color.FromArgb(190, 0x00, 0xC9, 0xDB));
                    Glow(dc, w, h, 0.30 * w, 0.85 * h, 0.65 * w, Color.FromArgb(170, 0xFF, 0x4D, 0x8C));
                    Glow(dc, w, h, 0.80 * w, 0.75 * h, 0.65 * w, Color.FromArgb(160, 0x7B, 0x2D, 0xFF));
                    break;
                case Preset.Minimal:
                    Glow(dc, w, h, 0.50 * w, 0.45 * h, 1.35 * w, Color.FromArgb(140, 0x3A, 0x76, 0xD2));
                    Glow(dc, w, h, 0.72 * w, 0.18 * h, 0.45 * w, Color.FromArgb(90, 0x00, 0xC9, 0xDB));
                    break;
            }

            Dots(dc, w, h);

            var text = new FormattedText(
                "GoConsoleOS",
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                110,
                new SolidColorBrush(Color.FromArgb(42, 240, 240, 255)),
                96.0)
            {
                TextAlignment = TextAlignment.Right
            };
            dc.DrawText(text, new Point(w - 90, h - 155));

            var vignette = new RadialGradientBrush
            {
                GradientOrigin = new Point(0.5, 0.5),
                Center = new Point(0.5, 0.5),
                GradientStops =
                {
                    new GradientStop(Color.FromArgb(0, 0, 0, 0), 0.62),
                    new GradientStop(Color.FromArgb(80, 0, 0, 0), 1.0)
                }
            };
            dc.DrawRectangle(vignette, null, new Rect(0, 0, w, h));
        }

        var bmp = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
        bmp.Render(visual);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bmp));
        using (var fs = File.Create(path))
            encoder.Save(fs);
    }

    private static void Glow(DrawingContext dc, int w, int h, double cx, double cy, double radius, Color color)
    {
        var brush = new RadialGradientBrush(color, Colors.Transparent)
        {
            GradientOrigin = new Point(0.5, 0.5)
        };
        dc.DrawEllipse(brush, null, new Point(cx, cy), radius, radius);
    }

    private static void Dots(DrawingContext dc, int w, int h)
    {
        var brush = new SolidColorBrush(Color.FromArgb(34, 255, 255, 255));
        for (int x = 48; x < w; x += 76)
            for (int y = 48; y < h; y += 76)
                dc.DrawEllipse(brush, null, new Point(x, y), 2.1, 2.1);
    }
}
