using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using GoConsoleOS.Shared;

namespace GoConsoleOS.GoCore;

public partial class SplashWindow : Window
{
    private readonly DispatcherTimer _progressTimer;
    private double _progress;
    private readonly string[] _bootStatuses =
    {
        "Initializing GoConsoleOS...",
        "Detecting hardware...",
        "Starting controller service...",
        "Scanning game libraries...",
        "Preparing console shell...",
        "Almost ready..."
    };
    private int _statusIndex;

    public SplashWindow()
    {
        InitializeComponent();
        VersionText.Text = $"v{ConfigReader.ReadInitConfig().General.Version}";

        _progressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
        _progressTimer.Tick += OnProgressTick;
        _progressTimer.Start();

        BeginFadeIn();
    }

    private void BeginFadeIn()
    {
        Opacity = 0;
        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(500));
        BeginAnimation(OpacityProperty, fadeIn);
    }

    private void OnProgressTick(object? sender, EventArgs e)
    {
        _progress += 0.012;
        if (_progress > 1.0) _progress = 1.0;

        ProgressBar.Width = 320 * _progress;

        if (_statusIndex < _bootStatuses.Length && _progress > (_statusIndex + 1) * (1.0 / _bootStatuses.Length))
        {
            StatusText.Text = _bootStatuses[_statusIndex];
            _statusIndex++;
        }
    }

    public void SetStatus(string status)
    {
        Dispatcher.Invoke(() => StatusText.Text = status);
    }

    public void Complete()
    {
        _progressTimer.Stop();
        ProgressBar.Width = 320;
        StatusText.Text = "Ready!";

        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(400));
        fadeOut.Completed += (_, _) => Close();
        BeginAnimation(OpacityProperty, fadeOut);
    }
}
