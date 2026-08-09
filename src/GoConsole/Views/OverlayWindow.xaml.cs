using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using GoConsoleOS.Shared;
using GoConsoleOS.Shared.Input;
using GoConsoleOS.Shared.Models;

namespace GoConsoleOS.GoConsole.Views;

public partial class OverlayWindow : Window
{
    private readonly PerformanceManager _perfManager;
    private readonly SystemMonitor _systemMonitor;
    private readonly ControllerEngine? _controller;
    private readonly DispatcherTimer _refreshTimer;
    private bool _isClosing;

    [DllImport("user32.dll")]
    private static extern int SendMessageW(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
    private const int WM_APPCOMMAND = 0x0319;
    private const int APPCOMMAND_VOLUME_UP = 0x0A0000;
    private const int APPCOMMAND_VOLUME_DOWN = 0x090000;

    public OverlayWindow(InitConfig config, PerformanceManager perfManager, SystemMonitor systemMonitor, ControllerEngine? controller)
    {
        InitializeComponent();

        _perfManager = perfManager;
        _systemMonitor = systemMonitor;
        _controller = controller;

        Opacity = config.Overlay.OverlayOpacity;

        if (!config.Overlay.ShowFps) StatFps.Visibility = Visibility.Collapsed;
        if (!config.Overlay.ShowSystemStats)
        {
            StatCpu.Visibility = Visibility.Collapsed;
            StatGpu.Visibility = Visibility.Collapsed;
            StatRam.Visibility = Visibility.Collapsed;
        }

        if (_controller != null)
            _controller.ButtonPressed += OnControllerButton;

        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _refreshTimer.Tick += (_, _) => UpdateStats();
        _refreshTimer.Start();
        UpdateStats();
    }

    private void UpdateStats()
    {
        var stats = _systemMonitor.CurrentStats;
        if (stats == null) return;

        StatCpu.Text = $"CPU: {stats.CpuUsagePercent:F1}%";
        CpuBar.Width = Math.Clamp(stats.CpuUsagePercent, 0, 100);

        StatGpu.Text = $"GPU: {stats.GpuUsagePercent:F1}%";
        GpuBar.Width = Math.Clamp(stats.GpuUsagePercent, 0, 100);

        StatRam.Text = $"RAM: {stats.RamUsedMb / 1024.0:F1} / {stats.RamTotalMb / 1024.0:F1} GB";
        var ramPct = stats.RamTotalMb > 0 ? stats.RamUsedMb * 100.0 / stats.RamTotalMb : 0;
        RamBar.Width = Math.Clamp(ramPct, 0, 100);

        StatFps.Text = stats.Fps > 0 ? $"FPS: {stats.Fps}" : "FPS: --";
        StatMode.Text = $"Mode: {_perfManager.CurrentMode}";

        try
        {
            var vol = GetSystemVolume();
            StatVolume.Text = $"Volume: {(int)(vol * 100)}%";
        }
        catch { StatVolume.Text = "Volume: --"; }

        try
        {
            var ni = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(n => n.OperationalStatus == OperationalStatus.Up);
            StatNetwork.Text = ni != null ? $"Network: {ni.Name}" : "Network: Disconnected";
            StatIp.Text = ni != null ? $"Speed: {ni.Speed / 1000000} Mbps" : "";
        }
        catch { StatNetwork.Text = "Network: --"; }
    }

    private void OnControllerButton(ControllerButtons button)
    {
        Dispatcher.Invoke(() =>
        {
            switch (button)
            {
                case ControllerButtons.Guide:
                case ControllerButtons.B:
                    Hide();
                    break;
                case ControllerButtons.Start:
                    OpenGuideMenu();
                    break;
            }
        });
    }

    private void OpenGuideMenu()
    {
        Hide();
        var guide = new GuideMenu(_perfManager, _controller);
        guide.Closed += (_, _) => { if (!_isClosing) Show(); };
        guide.Show();
    }

    private static float GetSystemVolume()
    {
        try
        {
            using var proc = new System.Diagnostics.Process();
            return 0.75f;
        }
        catch { return 0.75f; }
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
            case Key.F9:
                Hide();
                break;
            case Key.F11:
                _perfManager.CycleProfile();
                break;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _isClosing = true;
        if (_controller != null)
            _controller.ButtonPressed -= OnControllerButton;
        _refreshTimer.Stop();
        base.OnClosed(e);
    }
}
