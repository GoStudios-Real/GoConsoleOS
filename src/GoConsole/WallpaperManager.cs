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
        if (!File.Exists(Path.Combine(dir, "bluewave.png")))
            RenderBlueWave(Path.Combine(dir, "bluewave.png"));
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
                    Glow(dc, w, h, 0.18 * w, 0.20 * h, 0.85 * w, Color.FromArgb(170, 0x00, 0x66, 0xFF));
                    Glow(dc, w, h, 0.85 * w, 0.85 * h, 0.75 * w, Color.FromArgb(160, 0x7B, 0x2D, 0xFF));
                    Glow(dc, w, h, 0.15 * w, 0.90 * h, 0.45 * w, Color.FromArgb(90, 0xFF, 0x4D, 0x8C));
                    break;
                case Preset.Aurora:
                    Glow(dc, w, h, 0.50 * w, 0.30 * h, 1.15 * w, Color.FromArgb(190, 0x00, 0x66, 0xFF));
                    Glow(dc, w, h, 0.30 * w, 0.85 * h, 0.65 * w, Color.FromArgb(170, 0xFF, 0x4D, 0x8C));
                    Glow(dc, w, h, 0.80 * w, 0.75 * h, 0.65 * w, Color.FromArgb(160, 0x7B, 0x2D, 0xFF));
                    break;
                case Preset.Minimal:
                    Glow(dc, w, h, 0.50 * w, 0.45 * h, 1.35 * w, Color.FromArgb(140, 0x3A, 0x76, 0xD2));
                    Glow(dc, w, h, 0.72 * w, 0.18 * h, 0.45 * w, Color.FromArgb(90, 0x00, 0x66, 0xFF));
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

    private static void RenderBlueWave(string path)
    {
        const int w = 2560;
        const int h = 1440;

        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            // Deep dark blue background gradient
            var bg = new LinearGradientBrush(
                Color.FromRgb(0x05, 0x0A, 0x18),
                Color.FromRgb(0x0A, 0x12, 0x28),
                new Point(0, 0), new Point(0.5, 1));
            dc.DrawRectangle(bg, null, new Rect(0, 0, w, h));

            // Secondary dark gradient for depth
            var bg2 = new LinearGradientBrush(
                Color.FromArgb(60, 0x05, 0x10, 0x30),
                Color.FromArgb(0, 0x05, 0x10, 0x30),
                new Point(0, 1), new Point(0, 0));
            dc.DrawRectangle(bg2, null, new Rect(0, 0, w, h));

            // Wave layers - multiple overlapping sine waves with blue glow
            var waveColors = new[]
            {
                Color.FromArgb(100, 0x00, 0x55, 0xFF),
                Color.FromArgb(140, 0x00, 0x77, 0xFF),
                Color.FromArgb(180, 0x00, 0x99, 0xFF),
                Color.FromArgb(220, 0x00, 0xBB, 0xFF),
                Color.FromArgb(255, 0x00, 0xDD, 0xFF),
            };

            for (int layer = 0; layer < 5; layer++)
            {
                var color = waveColors[layer];
                var pen = new Pen(new SolidColorBrush(color), 3 + layer * 0.5f);

                var segments = 200;
                var pathFig = new PathFigure { StartPoint = new Point(0, h * 0.5 + layer * 40) };

                for (int i = 0; i <= segments; i++)
                {
                    var x = (double)i / segments * w;
                    var yBase = h * 0.45 + layer * 35;
                    var amplitude = 80 + layer * 25;
                    var frequency = 0.003 + layer * 0.0005;
                    var phase = layer * 0.8;
                    var y = yBase + Math.Sin(x * frequency + phase) * amplitude
                                + Math.Sin(x * frequency * 1.5 + phase * 0.7) * amplitude * 0.4
                                + Math.Sin(x * frequency * 2.5 + phase * 1.3) * amplitude * 0.15;

                    if (i == 0)
                        pathFig.StartPoint = new Point(x, y);
                    else
                        pathFig.Segments.Add(new LineSegment(new Point(x, y), true));
                }

                var pathGeom = new PathGeometry();
                pathGeom.Figures.Add(pathFig);
                dc.DrawGeometry(null, pen, pathGeom);

                // Glow effect for each wave
                var glowPen = new Pen(new SolidColorBrush(Color.FromArgb((byte)(color.A / 3), color.R, color.G, color.B)), 8 + layer * 2);
                dc.DrawGeometry(null, glowPen, pathGeom);
            }

            // Particle dots scattered along waves
            var rng = new Random(42);
            var dotBrush = new SolidColorBrush(Color.FromArgb(180, 0x44, 0x99, 0xFF));
            var dotBrushBright = new SolidColorBrush(Color.FromArgb(240, 0x88, 0xCC, 0xFF));
            for (int i = 0; i < 600; i++)
            {
                var x = rng.NextDouble() * w;
                var layer = rng.Next(5);
                var yBase = h * 0.45 + layer * 35;
                var amplitude = 80 + layer * 25;
                var frequency = 0.003 + layer * 0.0005;
                var phase = layer * 0.8;
                var y = yBase + Math.Sin(x * frequency + phase) * amplitude
                            + Math.Sin(x * frequency * 1.5 + phase * 0.7) * amplitude * 0.4
                            + Math.Sin(x * frequency * 2.5 + phase * 1.3) * amplitude * 0.15;
                y += (rng.NextDouble() - 0.5) * 40;
                var size = 0.8 + rng.NextDouble() * 2.5;
                var brush = rng.NextDouble() > 0.7 ? dotBrushBright : dotBrush;
                dc.DrawEllipse(brush, null, new Point(x, y), size, size);
            }

            // Top glow - bright blue area
            var topGlow = new RadialGradientBrush(
                Color.FromArgb(80, 0x00, 0x66, 0xFF),
                Color.FromArgb(0, 0x00, 0x00, 0x00))
            {
                GradientOrigin = new Point(0.5, 0.3),
                Center = new Point(0.5, 0.25)
            };
            dc.DrawRectangle(topGlow, null, new Rect(0, 0, w, h));

            // Bottom subtle glow
            var bottomGlow = new RadialGradientBrush(
                Color.FromArgb(40, 0x00, 0x44, 0xCC),
                Color.FromArgb(0, 0x00, 0x00, 0x00))
            {
                GradientOrigin = new Point(0.5, 0.8),
                Center = new Point(0.5, 0.9)
            };
            dc.DrawRectangle(bottomGlow, null, new Rect(0, 0, w, h));

            // Vignette
            var vignette = new RadialGradientBrush
            {
                GradientOrigin = new Point(0.5, 0.5),
                Center = new Point(0.5, 0.5),
                GradientStops =
                {
                    new GradientStop(Color.FromArgb(0, 0, 0, 0), 0.55),
                    new GradientStop(Color.FromArgb(120, 0, 0, 0), 1.0)
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
}
