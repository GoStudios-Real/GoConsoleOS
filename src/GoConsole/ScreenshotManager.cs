using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GoConsoleOS.Shared;

namespace GoConsoleOS.GoConsole;

public class ScreenshotManager
{
    private readonly string _screenshotDir;
    private int _screenshotCount;

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdc, int x, int y, int cx, int cy, IntPtr hdcSrc, int x1, int y1, int rop);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int cx, int cy);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    public ScreenshotManager()
    {
        var root = ConfigReader.RootPath ?? Directory.GetCurrentDirectory();
        _screenshotDir = Path.Combine(root, "system", "screenshots");
        Directory.CreateDirectory(_screenshotDir);
        _screenshotCount = Directory.GetFiles(_screenshotDir, "screenshot_*.png").Length;
    }

    public string? CaptureScreenshot()
    {
        try
        {
            _screenshotCount++;
            var filename = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}_{_screenshotCount}.png";
            var path = Path.Combine(_screenshotDir, filename);

            var desktopHwnd = GetDesktopWindow();
            var desktopDc = GetWindowDC(desktopHwnd);
            var memDc = CreateCompatibleDC(desktopDc);

            var width = (int)SystemParameters.VirtualScreenWidth;
            var height = (int)SystemParameters.VirtualScreenHeight;

            var bitmap = CreateCompatibleBitmap(desktopDc, width, height);
            var oldBitmap = SelectObject(memDc, bitmap);

            BitBlt(memDc, 0, 0, width, height, desktopDc, 0, 0, 0x00CC0020);

            var bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                bitmap, IntPtr.Zero, Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bs));
                encoder.Save(fs);
            }

            SelectObject(memDc, oldBitmap);
            DeleteObject(bitmap);
            DeleteDC(memDc);
            ReleaseDC(desktopHwnd, desktopDc);

            Logger.Info($"Screenshot saved: {path}");
            return path;
        }
        catch (Exception ex)
        {
            Logger.Error($"Screenshot failed: {ex.Message}");
            return null;
        }
    }

    public int ScreenshotCount => _screenshotCount;
}
