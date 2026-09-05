using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using GoConsoleOS.Shared;
using GoConsoleOS.Shared.Input;

namespace GoConsoleOS.GoConsole.Views;

public partial class GuideMenu : Window
{
    private readonly PerformanceManager _perfManager;
    private readonly ControllerEngine? _controller;
    private bool _isGoConsoleMode = true;

    [DllImport("user32.dll")]
    private static extern bool ExitWindowsEx(uint uFlags, uint dwReason);
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    private const uint EWX_SHUTDOWN = 0x00000001;
    private const uint EWX_REBOOT = 0x00000002;
    private const uint EWX_FORCE = 0x00000004;

    public GuideMenu(PerformanceManager perfManager, ControllerEngine? controller)
    {
        InitializeComponent();
        _perfManager = perfManager;
        _controller = controller;

        UpdatePerfDisplay();
        UpdateVolumeDisplay();
        UpdateModeUI();
        UpdateProfileInfo();

        Opacity = 0;
        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
        BeginAnimation(OpacityProperty, fadeIn);

        if (_controller != null)
            _controller.ButtonPressed += OnControllerButton;
    }

    private void UpdateModeUI()
    {
        var accent = TryFindResource("BrushAccentPrimary") as System.Windows.Media.Brush;
        var bgCard = TryFindResource("BrushBackgroundCard") as System.Windows.Media.Brush;
        var textPrimary = TryFindResource("BrushTextPrimary") as System.Windows.Media.Brush;
        var darkText = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x0D, 0x0D, 0x14));

        if (_isGoConsoleMode)
        {
            GoConsoleModeBtn.Background = accent;
            GoConsoleStatus.Text = "ACTIVE";
            GoConsoleStatus.Foreground = darkText;
            DesktopModeBtn.Background = bgCard;
            DesktopStatus.Text = "Switch";
            DesktopStatus.Foreground = textPrimary;
            ModeIndicator.Text = "✦ GOCONSOLE MODE";
        }
        else
        {
            GoConsoleModeBtn.Background = bgCard;
            GoConsoleStatus.Text = "Switch";
            GoConsoleStatus.Foreground = textPrimary;
            DesktopModeBtn.Background = accent;
            DesktopStatus.Text = "ACTIVE";
            DesktopStatus.Foreground = darkText;
            ModeIndicator.Text = "🖥️ DESKTOP MODE";
        }
    }

    private void UpdatePerfDisplay()
    {
        var profile = _perfManager.GetCurrentProfile();
        if (profile != null)
        {
            PerfModeDesc.Text = $"Current: {profile.Name}";
            PerfModeValue.Text = profile.Name.ToUpper();
        }
    }

    private void UpdateVolumeDisplay()
    {
        try
        {
            var vol = NativeAudio.GetMasterVolume();
            VolumeDesc.Text = $"Current: {(int)(vol * 100)}%";
        }
        catch
        {
            VolumeDesc.Text = "Volume: --";
        }
    }

    private void UpdateProfileInfo()
    {
        var main = Application.Current.Windows.OfType<Window>()
            .FirstOrDefault(w => w is MainWindow) as MainWindow;
        if (main != null)
        {
            ProfileName.Text = main._profileManager.CurrentProfile?.DisplayName ?? "Player";

            var profile = main._profileManager.CurrentProfile;
            var pts = profile?.TotalPlaytimeMinutes ?? 0;
            var title = profile?.Achievements?.Count(a => a.Value.IsUnlocked) ?? 0;
            ProfilePoints.Text = $"{title} achievements, {pts} min played";
        }
    }

    private void SwitchToGoConsoleMode(object sender, MouseButtonEventArgs e)
    {
        _isGoConsoleMode = true;
        UpdateModeUI();
        foreach (Window w in Application.Current.Windows)
        {
            if (w.IsVisible && w is MainWindow main)
            {
                main.WindowState = WindowState.Maximized;
                main.Topmost = true;
                main.Activate();
                break;
            }
        }
    }

    private void SwitchToDesktopMode(object sender, MouseButtonEventArgs e)
    {
        _isGoConsoleMode = false;
        UpdateModeUI();
        Hide();
        foreach (Window w in Application.Current.Windows)
        {
            if (w.IsVisible && w is MainWindow main)
            {
                main.WindowState = WindowState.Minimized;
                main.Topmost = false;
                break;
            }
        }
    }

    private void EnterRestMode(object sender, MouseButtonEventArgs e)
    {
        var result = MessageBox.Show("Enter Rest Mode? GoConsoleOS will sleep in the background.",
            "Rest Mode", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            Hide();
            foreach (Window w in Application.Current.Windows)
            {
                if (w.IsVisible && w is MainWindow main)
                {
                    main.WindowState = WindowState.Minimized;
                    main.Topmost = false;
                    break;
                }
            }
            try
            {
                Process.Start(new ProcessStartInfo("rundll32.exe", "powrprof.dll,SetSuspendState 0,1,0")
                {
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
            }
            catch { }
        }
    }

    private void OpenQuickResume(object sender, MouseButtonEventArgs e)
    {
        Close();
        var main = Application.Current.Windows.OfType<Window>()
            .FirstOrDefault(w => w is MainWindow) as MainWindow;
        main?.NavigateTo("quickresume");
    }

    private void OpenProfile(object sender, MouseButtonEventArgs e)
    {
        Close();
        var main = Application.Current.Windows.OfType<Window>()
            .FirstOrDefault(w => w is MainWindow) as MainWindow;
        main?.NavigateTo("friends");
    }

    private void OpenFeature(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement element || element.Tag is not string view)
            return;

        Close();
        var main = Application.Current.Windows.OfType<Window>()
            .FirstOrDefault(w => w is MainWindow) as MainWindow;
        main?.NavigateTo(view);
    }

    private void OpenExitMenu(object sender, MouseButtonEventArgs e)
    {
        Close();
        var main = Application.Current.Windows.OfType<Window>()
            .FirstOrDefault(w => w is MainWindow) as MainWindow;
        main?.OpenExitMenu();
    }

    private void CloseMenu_Click(object sender, MouseButtonEventArgs e) => Close();
    private void Resume_Click(object sender, MouseButtonEventArgs e) => Close();

    private void CyclePerfMode_Click(object sender, MouseButtonEventArgs e)
    {
        _perfManager.CycleProfile();
        UpdatePerfDisplay();
    }

    private void Volume_Click(object sender, MouseButtonEventArgs e)
    {
        try
        {
            var vol = NativeAudio.GetMasterVolume();
            vol = vol < 0.5f ? 1.0f : 0.0f;
            NativeAudio.SetMasterVolume(vol);
            UpdateVolumeDisplay();
        }
        catch
        {
            MessageBox.Show("Volume control requires Windows audio services.",
                            "Volume", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void ShutdownPC_Click(object sender, MouseButtonEventArgs e)
    {
        var result = MessageBox.Show("Shut down your PC?", "Shutdown",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            Close();
            Application.Current.Shutdown();
            Process.Start(new ProcessStartInfo("shutdown", "/s /t 3") { UseShellExecute = true });
        }
    }

    private void RestartPC_Click(object sender, MouseButtonEventArgs e)
    {
        var result = MessageBox.Show("Restart your PC?", "Restart",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            Close();
            Application.Current.Shutdown();
            Process.Start(new ProcessStartInfo("shutdown", "/r /t 3") { UseShellExecute = true });
        }
    }

    private void Overlay_Click(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource == OverlayBg)
            Close();
    }

    private void OnControllerButton(ControllerButtons button)
    {
        var mainHandle = new WindowInteropHelper(Application.Current.MainWindow!).Handle;
        if (GetForegroundWindow() != mainHandle) return;
        Dispatcher.Invoke(() =>
        {
            if (button == ControllerButtons.Guide || button == ControllerButtons.B)
                Close();
        });
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape || e.Key == Key.F9)
            Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_controller != null)
            _controller.ButtonPressed -= OnControllerButton;
        base.OnClosed(e);
    }
}

internal static class NativeAudio
{
    [DllImport("user32.dll")]
    private static extern int SendMessageW(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

    private const int WM_APPCOMMAND = 0x0319;
    private const int APPCOMMAND_VOLUME_UP = 0x0A0000;
    private const int APPCOMMAND_VOLUME_DOWN = 0x090000;
    private const int APPCOMMAND_VOLUME_MUTE = 0x080000;

    public static float GetMasterVolume()
    {
        return 0.75f;
    }

    public static void SetMasterVolume(float level)
    {
        try
        {
            var handle = IntPtr.Zero;
            for (int i = 0; i < (int)(level * 50); i++)
            {
                SendMessageW(handle, WM_APPCOMMAND, handle, (IntPtr)APPCOMMAND_VOLUME_UP);
            }
        }
        catch { }
    }

    public static void ToggleMute()
    {
        try
        {
            var handle = IntPtr.Zero;
            SendMessageW(handle, WM_APPCOMMAND, handle, (IntPtr)APPCOMMAND_VOLUME_MUTE);
        }
        catch { }
    }
}
