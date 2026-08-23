using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using GoConsoleOS.Shared;

namespace GoConsoleOS.GoConsole;

public partial class BootScreen : Window
{
    private readonly DispatcherTimer _progressTimer;
    private bool _finished;
    private double _progress;
    private readonly string[] _bootStatuses =
    {
        "Initializing GoConsoleOS...",
        "Detecting hardware...",
        "Starting controller service...",
        "Scanning game libraries...",
        "Loading cloud saves...",
        "Preparing console shell...",
        "Almost ready..."
    };
    private int _statusIndex;

    public BootScreen()
    {
        InitializeComponent();
        var config = ConfigReader.ReadInitConfig();
        VersionText.Text = $"v{config.General.Version}";
        Title = config.General.OsName;
        LogoText.Text = config.General.OsName;
        TrademarkText.Text = $"{config.General.OsName}™ is a trademark of GoStudios Corporation. © 2026 GoStudios Corporation.";
        ApplyVariantColors(config);

        _bootStatuses[0] = $"Initializing {config.General.OsName}...";

        LogoPanel.Visibility = Visibility.Collapsed;
        BrandScreen.Visibility = Visibility.Visible;
        BeginBrandAnimation();

        _progressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(90) };
        _progressTimer.Tick += OnProgressTick;
        _progressTimer.Start();

        Opacity = 0;
        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(400));
        BeginAnimation(OpacityProperty, fadeIn);
    }

    private void ApplyVariantColors(GoConsoleOS.Shared.Models.InitConfig config)
    {
        var theme = config.Theme;
        try
        {
            if (!string.IsNullOrEmpty(theme.BackgroundDark))
                RootBorder.Background = ParseBrush(theme.BackgroundDark);
            var accent = !string.IsNullOrEmpty(theme.AccentPrimary) ? theme.AccentPrimary : "#00C9DB";
            var accentBrush = ParseBrush(accent);
            BrandBadge.Background = accentBrush;
            BrandText.Foreground = accentBrush;
            LogoBadge.Background = accentBrush;
            LogoText.Foreground = accentBrush;
            ProgressBar.Background = accentBrush;
        }
        catch { }
    }

    private static Brush ParseBrush(string hex)
    {
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
    }

    private void BeginBrandAnimation()
    {
        BrandScreen.Opacity = 0;
        var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(350));
        BrandScreen.BeginAnimation(OpacityProperty, fade);

        var scale = new DoubleAnimation(0.5, 1.0, TimeSpan.FromMilliseconds(650));
        scale.EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 };
        BrandScreen.RenderTransform = new ScaleTransform(0.5, 0.5);
        BrandScreen.RenderTransformOrigin = new Point(0.5, 0.5);
        BrandScreen.BeginAnimation(ScaleTransform.ScaleXProperty, scale);
        BrandScreen.BeginAnimation(ScaleTransform.ScaleYProperty, scale);
    }

    private void ShowLogoPanel()
    {
        if (LogoPanel.Visibility == Visibility.Visible) return;
        BrandScreen.Visibility = Visibility.Collapsed;
        LogoPanel.Visibility = Visibility.Visible;
        LogoPanel.Opacity = 0;
        var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(350));
        LogoPanel.BeginAnimation(OpacityProperty, fade);

        var logoScale = new DoubleAnimation(0.6, 1.0, TimeSpan.FromMilliseconds(600));
        logoScale.EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.4 };
        LogoScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, logoScale);
        LogoScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, logoScale);
    }

    private void OnProgressTick(object? sender, EventArgs e)
    {
        _progress += 0.014;
        if (_progress > 1.0) _progress = 1.0;

        ProgressBar.Width = 360 * _progress;

        if (_progress > 0.30)
            ShowLogoPanel();

        if (_statusIndex < _bootStatuses.Length && _progress > (_statusIndex + 1) * (1.0 / _bootStatuses.Length))
        {
            StatusText.Text = _bootStatuses[_statusIndex];
            _statusIndex++;
        }

        if (_progress >= 1.0)
        {
            _progressTimer.Stop();
            StatusText.Text = "Ready!";
        }
    }

    public void Finish()
    {
        if (_finished) return;
        _finished = true;
        _progressTimer.Stop();
        StatusText.Text = "Ready!";

        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(400));
        fadeOut.Completed += (_, _) => Close();
        BeginAnimation(OpacityProperty, fadeOut);
    }
}
