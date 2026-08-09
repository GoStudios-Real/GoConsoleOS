using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GoConsoleOS.Shared;

namespace GoConsoleOS.GoConsole.Views;

public partial class QuickAccessPanel : Window
{
    private readonly PerformanceManager _perf;
    private readonly ScreenshotManager? _screenshot;
    private bool _wifiOn = true;
    private bool _bluetoothOn;
    private bool _gameOptimized;
    private bool _nightMode;

    public QuickAccessPanel(PerformanceManager perf, ScreenshotManager? screenshot = null)
    {
        InitializeComponent();
        _perf = perf;
        _screenshot = screenshot;
        CurrentPerfMode.Text = perf.CurrentMode.ToUpper();

        if (_screenshot != null)
            ScreenshotCount.Text = $"{_screenshot.ScreenshotCount} screenshots";

        try
        {
            var screenshotDir = System.IO.Path.Combine(
                GoConsoleOS.Shared.ConfigReader.RootPath ?? Environment.CurrentDirectory,
                "system", "screenshots");
            if (System.IO.Directory.Exists(screenshotDir))
                ScreenshotCount.Text = $"{System.IO.Directory.GetFiles(screenshotDir, "*.png").Length} screenshots";
        }
        catch { }

        var main = Application.Current.Windows.OfType<Window>()
            .FirstOrDefault(w => w.GetType().Name == "MainWindow") as MainWindow;
        if (main != null)
        {
            var notifCount = main._notificationHistory.Count;
            NotificationStatus.Text = notifCount > 0
                ? $"{notifCount} notification{(notifCount == 1 ? "" : "s")}"
                : "No new notifications";
        }

        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
                Close();
        };

        Loaded += (_, _) =>
        {
            var guideWindows = Application.Current.Windows;
            foreach (Window w in guideWindows)
            {
                if (w is QuickAccessPanel qa && qa != this)
                    qa.Close();
            }
        };
    }

    private void SetPerformance(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string mode)
        {
            _perf.SetProfile(mode);
            CurrentPerfMode.Text = mode.ToUpper();
            var accentBrush = TryFindResource("BrushAccentPrimary") as Brush;
            var bgBrush = TryFindResource("BrushBackgroundCard") as Brush;
            var textBrush = TryFindResource("BrushTextPrimary") as Brush;
            var darkBrush = new SolidColorBrush(Color.FromRgb(0x0D, 0x0D, 0x14));

            PerfQuiet.Background = mode == "quiet" ? accentBrush : bgBrush;
            PerfBalanced.Background = mode == "balanced" ? accentBrush : bgBrush;
            PerfTurbo.Background = mode == "turbo" ? accentBrush : bgBrush;

            foreach (var tb in new[] { (TextBlock)PerfQuiet.Child, (TextBlock)PerfBalanced.Child, (TextBlock)PerfTurbo.Child })
                tb.Foreground = (tb.Parent as Border)?.Tag?.ToString() == mode ? darkBrush : textBrush;
        }
    }

    private void ToggleWifi(object sender, MouseButtonEventArgs e)
    {
        _wifiOn = !_wifiOn;
        WifiText.Text = _wifiOn ? "ON" : "OFF";
        WifiToggle.Background = _wifiOn
            ? TryFindResource("BrushSuccess") as Brush
            : TryFindResource("BrushBackgroundCard") as Brush;
        WifiText.Foreground = _wifiOn
            ? new SolidColorBrush(Color.FromRgb(0x0D, 0x0D, 0x14))
            : TryFindResource("BrushTextPrimary") as Brush;
    }

    private void ToggleBluetooth(object sender, MouseButtonEventArgs e)
    {
        _bluetoothOn = !_bluetoothOn;
        BluetoothText.Text = _bluetoothOn ? "ON" : "OFF";
        BluetoothToggle.Background = _bluetoothOn
            ? TryFindResource("BrushSuccess") as Brush
            : TryFindResource("BrushBackgroundCard") as Brush;
        BluetoothText.Foreground = _bluetoothOn
            ? new SolidColorBrush(Color.FromRgb(0x0D, 0x0D, 0x14))
            : TryFindResource("BrushTextPrimary") as Brush;
    }

    private void ToggleGameOptimized(object sender, MouseButtonEventArgs e)
    {
        _gameOptimized = !_gameOptimized;
        GameOptText.Text = _gameOptimized ? "ON" : "OFF";
        GameOptToggle.Background = _gameOptimized
            ? TryFindResource("BrushWarning") as Brush
            : TryFindResource("BrushBackgroundCard") as Brush;
        GameOptText.Foreground = _gameOptimized
            ? new SolidColorBrush(Color.FromRgb(0x0D, 0x0D, 0x14))
            : TryFindResource("BrushTextPrimary") as Brush;

        if (_gameOptimized)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c powercfg -setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c",
                    UseShellExecute = true,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                });
            }
            catch { }
        }
    }

    private void ToggleNightMode(object sender, MouseButtonEventArgs e)
    {
        _nightMode = !_nightMode;
        NightModeText.Text = _nightMode ? "ON" : "OFF";
        NightModeToggle.Background = _nightMode
            ? TryFindResource("BrushAccentSecondary") as Brush
            : TryFindResource("BrushBackgroundCard") as Brush;
        NightModeText.Foreground = _nightMode
            ? new SolidColorBrush(Color.FromRgb(0x0D, 0x0D, 0x14))
            : TryFindResource("BrushTextPrimary") as Brush;

        var main = Application.Current.Windows.OfType<Window>()
            .FirstOrDefault(w => w.GetType().Name == "MainWindow") as MainWindow;
        main?.SetNightMode(_nightMode);
    }

    private void OpenNotifications(object sender, MouseButtonEventArgs e)
    {
        var main = Application.Current.Windows.OfType<Window>()
            .FirstOrDefault(w => w.GetType().Name == "MainWindow") as MainWindow;
        if (main != null)
        {
            main.ToggleNotificationPanel();
        }
        Close();
    }

    private void OpenScreenshots(object sender, MouseButtonEventArgs e)
    {
        var main = Application.Current.Windows.OfType<Window>()
            .FirstOrDefault(w => w.GetType().Name == "MainWindow") as MainWindow;
        main?.NavigateTo("captures");
        Close();
    }

    private void SwitchToGoConsoleMode(object sender, MouseButtonEventArgs e)
    {
        var main = Application.Current.Windows.OfType<Window>()
            .FirstOrDefault(w => w is MainWindow) as MainWindow;
        if (main != null)
        {
            main.WindowState = WindowState.Maximized;
            main.Topmost = true;
            main.Activate();
        }
        GoConsoleModeToggle.Background = TryFindResource("BrushAccentPrimary") as Brush;
        GoConsoleModeText.Foreground = new SolidColorBrush(Color.FromRgb(0x0D, 0x0D, 0x14));
        DesktopModeToggle.Background = TryFindResource("BrushBackgroundCard") as Brush;
        DesktopModeText.Foreground = TryFindResource("BrushTextPrimary") as Brush;
        Close();
    }

    private void SwitchToDesktopMode(object sender, MouseButtonEventArgs e)
    {
        var main = Application.Current.Windows.OfType<Window>()
            .FirstOrDefault(w => w is MainWindow) as MainWindow;
        if (main != null)
        {
            main.WindowState = WindowState.Minimized;
            main.Topmost = false;
            main.ExitToDesktop();
        }
        DesktopModeToggle.Background = TryFindResource("BrushAccentPrimary") as Brush;
        DesktopModeText.Foreground = new SolidColorBrush(Color.FromRgb(0x0D, 0x0D, 0x14));
        GoConsoleModeToggle.Background = TryFindResource("BrushBackgroundCard") as Brush;
        GoConsoleModeText.Foreground = TryFindResource("BrushTextPrimary") as Brush;
        Close();
    }

    private void ShutdownSystem(object sender, MouseButtonEventArgs e)
    {
        var result = MessageBox.Show("Shutdown GoConsoleOS and power off?", "Shutdown",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            var main = Application.Current.Windows.OfType<Window>()
                .FirstOrDefault(w => w.GetType().Name == "MainWindow") as MainWindow;
            if (main != null)
            {
                main.ShutdownGoConsoleOS();
                Close();
            }
        }
    }
}
