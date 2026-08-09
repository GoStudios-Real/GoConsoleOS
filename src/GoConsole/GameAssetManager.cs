using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GoConsoleOS.Shared;
using GoConsoleOS.Shared.Models;

namespace GoConsoleOS.GoConsole;

public class GameAssetManager
{
    private readonly string _bannerDir;
    private readonly string _iconDir;
    private readonly string _heroDir;
    private readonly string _logoDir;

    public GameAssetManager()
    {
        var root = ConfigReader.RootPath ?? Directory.GetCurrentDirectory();
        _bannerDir = Path.Combine(root, "system", "ui", "banners");
        _iconDir = Path.Combine(root, "system", "ui", "icons");
        _heroDir = Path.Combine(root, "system", "ui", "heroes");
        _logoDir = Path.Combine(root, "system", "ui", "logos");
        Directory.CreateDirectory(_bannerDir);
        Directory.CreateDirectory(_iconDir);
        Directory.CreateDirectory(_heroDir);
        Directory.CreateDirectory(_logoDir);
    }

    public string GetBannerPath(GameInfo game)
    {
        var path = Path.Combine(_bannerDir, $"{Sanitize(game.Id)}_banner.png");
        if (!File.Exists(path)) GenerateBanner(game, path);
        return path;
    }

    public string GetHeroPath(GameInfo game)
    {
        var path = Path.Combine(_heroDir, $"{Sanitize(game.Id)}_hero.png");
        if (!File.Exists(path)) GenerateHero(game, path);
        return path;
    }

    public string GetIconPath(GameInfo game)
    {
        var path = Path.Combine(_iconDir, $"{Sanitize(game.Id)}_icon.png");
        if (!File.Exists(path)) GenerateIcon(game, path);
        return path;
    }

    public string GetLogoPath() => Path.Combine(_logoDir, "goconsoleos_logo.png");

    public string GenerateGoConsoleOSLogo()
    {
        var path = GetLogoPath();
        if (File.Exists(path)) return path;
        var bmp = new RenderTargetBitmap(256, 64, 96, 96, PixelFormats.Pbgra32);
        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            var accentColor = Color.FromRgb(0x00, 0xC9, 0xDB);
            var textColor = Color.FromRgb(0xF0, 0xF0, 0xFF);
            dc.DrawText(new FormattedText("GoConsoleOS",
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new         Typeface("Segoe UI"), 36, new SolidColorBrush(textColor), 96.0)
            { TextAlignment = TextAlignment.Left }, new Point(0, 0));
            dc.DrawRectangle(new SolidColorBrush(accentColor), null, new Rect(0, 52, 180, 4));
        }
        bmp.Render(dv);
        using var fs = new FileStream(path, FileMode.Create);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bmp));
        encoder.Save(fs);
        return path;
    }

    private void GenerateBanner(GameInfo game, string path)
    {
        var seed = game.Title.GetHashCode();
        var rng = new Random(seed);
        var bmp = new RenderTargetBitmap(460, 215, 96, 96, PixelFormats.Pbgra32);
        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            var bg = Color.FromRgb((byte)rng.Next(20, 60), (byte)rng.Next(20, 50), (byte)rng.Next(40, 80));
            dc.DrawRectangle(new SolidColorBrush(bg), null, new Rect(0, 0, 460, 215));
            var accent = Color.FromRgb((byte)rng.Next(100, 255), (byte)rng.Next(100, 200), (byte)rng.Next(100, 255));
            for (int i = 0; i < 5; i++)
                dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(30, accent.R, accent.G, accent.B)), null,
                    new Rect(0, 40 + i * 30, 460, 2));
            dc.DrawText(new FormattedText(game.Title,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"), 28, new SolidColorBrush(Colors.White), 96.0)
            { TextAlignment = TextAlignment.Center }, new Point(230, 80));
        }
        bmp.Render(dv);
        using var fs = new FileStream(path, FileMode.Create);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bmp));
        encoder.Save(fs);
    }

    private void GenerateHero(GameInfo game, string path)
    {
        var seed = game.Title.GetHashCode();
        var rng = new Random(seed);
        var bmp = new RenderTargetBitmap(1920, 400, 96, 96, PixelFormats.Pbgra32);
        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            var bg = Color.FromRgb((byte)rng.Next(10, 40), (byte)rng.Next(10, 30), (byte)rng.Next(15, 50));
            dc.DrawRectangle(new SolidColorBrush(bg), null, new Rect(0, 0, 1920, 400));
            var accent = Color.FromRgb(0x00, 0xC9, 0xDB);
            for (int i = 0; i < 8; i++)
                dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(20, accent.R, accent.G, accent.B)), null,
                    new Rect(i * 240, 0, 2, 400));
            dc.DrawText(new FormattedText(game.Title,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"), 72, new SolidColorBrush(Colors.White), 96.0)
            { TextAlignment = TextAlignment.Left }, new Point(60, 120));
            dc.DrawText(new FormattedText(game.Platform,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"), 24, new SolidColorBrush(Color.FromRgb(0x00, 0xC9, 0xDB)), 96.0)
            { TextAlignment = TextAlignment.Left }, new Point(60, 210));
        }
        bmp.Render(dv);
        using var fs = new FileStream(path, FileMode.Create);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bmp));
        encoder.Save(fs);
    }

    private void GenerateIcon(GameInfo game, string path)
    {
        var seed = game.Title.GetHashCode();
        var rng = new Random(seed);
        var bmp = new RenderTargetBitmap(128, 128, 96, 96, PixelFormats.Pbgra32);
        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            var bg = Color.FromRgb((byte)rng.Next(30, 80), (byte)rng.Next(20, 60), (byte)rng.Next(40, 90));
            dc.DrawRectangle(new SolidColorBrush(bg), null, new Rect(0, 0, 128, 128));
            dc.DrawText(new FormattedText(game.Title.Length > 0 ? game.Title[0].ToString() : "?",
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"), 56, new SolidColorBrush(Colors.White), 96.0)
            { TextAlignment = TextAlignment.Center }, new Point(40, 30));
        }
        bmp.Render(dv);
        using var fs = new FileStream(path, FileMode.Create);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bmp));
        encoder.Save(fs);
    }

    private static string Sanitize(string id) =>
        string.Join("_", id.Split(Path.GetInvalidFileNameChars()));
}
