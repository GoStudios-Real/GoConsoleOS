using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using GoConsoleOS.Shared;

namespace GoConsoleOS.GoConsole.Views;

public partial class DynamicBackgroundView : UserControl
{
    private string _currentBg = "static";
    private readonly Color[] _animColors = {
        Color.FromRgb(0x00, 0xC9, 0xDB),
        Color.FromRgb(0x7B, 0x2D, 0xFF),
        Color.FromRgb(0xFF, 0x4D, 0x8C),
        Color.FromRgb(0x00, 0xE6, 0x76),
        Color.FromRgb(0xFF, 0xB9, 0x40),
    };
    private int _animIndex;

    public DynamicBackgroundView()
    {
        InitializeComponent();
    }

    private void SelectBg(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string mode)
        {
            _currentBg = mode;
            var active = FindResource("BrushAccentPrimary") as Brush ?? Brushes.Cyan;
            var inactive = FindResource("BrushBackgroundCard") as Brush;

            BgStatic.BorderBrush = mode == "static" ? active : inactive;
            BgAnimated.BorderBrush = mode == "animated" ? active : inactive;
            BgCapture.BorderBrush = mode == "capture" ? active : inactive;
            BgAccent.BorderBrush = mode == "accent" ? active : inactive;

            ApplyBackground(mode);
        }
    }

    private void ApplyBackground(string mode)
    {
        var main = Window.GetWindow(this) as MainWindow;
        if (main == null) return;

        switch (mode)
        {
            case "static":
                main.Background = new SolidColorBrush(Color.FromRgb(0x0D, 0x0D, 0x14));
                break;

            case "animated":
                StartColorAnimation(main);
                break;

            case "capture":
                SetCaptureBackground(main);
                break;

            case "accent":
                var accent = Application.Current.Resources["BrushAccentPrimary"] as SolidColorBrush;
                if (accent != null)
                {
                    var c = accent.Color;
                    main.Background = new SolidColorBrush(Color.FromRgb(
                        (byte)(c.R / 4), (byte)(c.G / 4), (byte)(c.B / 4)));
                }
                break;
        }
    }

    private void StartColorAnimation(MainWindow main)
    {
        var colorAnim = new ColorAnimation
        {
            From = _animColors[_animIndex % _animColors.Length],
            To = _animColors[(_animIndex + 1) % _animColors.Length],
            Duration = TimeSpan.FromSeconds(11 - SpeedSlider.Value),
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever
        };

        _animIndex++;
        var brush = new SolidColorBrush(_animColors[0]);
        main.Background = brush;
        brush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnim);
    }

    private void SetCaptureBackground(MainWindow main)
    {
        var dir = Path.Combine(ConfigReader.RootPath ?? "", "system", "screenshots");
        if (Directory.Exists(dir))
        {
            var files = Directory.GetFiles(dir, "*.png");
            if (files.Length > 0)
            {
                    var latest = files.OrderByDescending(f => File.GetLastWriteTime(f)).First();
                try
                {
                    var img = new BitmapImage();
                    img.BeginInit();
                    img.UriSource = new Uri(latest);
                    img.CacheOption = BitmapCacheOption.OnLoad;
                    img.EndInit();
                    var ib = new ImageBrush(img) { Stretch = Stretch.UniformToFill, Opacity = 0.5 };
                    main.Background = ib;
                    return;
                }
                catch { }
            }
        }
        main.Background = new SolidColorBrush(Color.FromRgb(0x0D, 0x0D, 0x14));
    }

    private void SpeedChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        SpeedLabel.Text = $"Speed: {(int)e.NewValue}";
    }
}
