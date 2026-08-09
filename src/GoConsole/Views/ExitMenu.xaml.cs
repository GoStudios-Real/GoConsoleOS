using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using GoConsoleOS.Shared;
using GoConsoleOS.Shared.Input;

namespace GoConsoleOS.GoConsole.Views;

public partial class ExitMenu : Window
{
    private readonly PerformanceManager? _perfManager;
    private readonly ControllerEngine? _controller;
    private bool _isExiting;

    public ExitMenu(PerformanceManager? perfManager = null, ControllerEngine? controller = null)
    {
        InitializeComponent();
        _perfManager = perfManager;
        _controller = controller;

        Opacity = 0;
        var fadeIn = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180));
        BeginAnimation(OpacityProperty, fadeIn);

        if (_controller != null)
            _controller.ButtonPressed += OnControllerButton;
    }

    private void ExitToDesktop_Click(object sender, MouseButtonEventArgs e)
    {
        if (_isExiting) return;
        _isExiting = true;

        var main = Application.Current.Windows.OfType<Window>()
            .FirstOrDefault(w => w is MainWindow) as MainWindow;
        main?.ExitToDesktop();
        Close();
    }

    private void Shutdown_Click(object sender, MouseButtonEventArgs e)
    {
        if (_isExiting) return;
        _isExiting = true;

        var result = MessageBox.Show("Turn off GoConsoleOS and shut down the PC?",
            "Turn Off Console", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            Close();
            var main = Application.Current.Windows.OfType<Window>()
                .FirstOrDefault(w => w is MainWindow) as MainWindow;
            main?.ShutdownGoConsoleOS();
            Application.Current.Shutdown();
            Process.Start(new ProcessStartInfo("shutdown", "/s /t 3") { UseShellExecute = true });
        }
        else
        {
            _isExiting = false;
        }
    }

    private void Restart_Click(object sender, MouseButtonEventArgs e)
    {
        if (_isExiting) return;
        _isExiting = true;

        var result = MessageBox.Show("Restart GoConsoleOS and reboot the PC?",
            "Restart Console", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            Close();
            var main = Application.Current.Windows.OfType<Window>()
                .FirstOrDefault(w => w is MainWindow) as MainWindow;
            main?.ShutdownGoConsoleOS();
            Application.Current.Shutdown();
            Process.Start(new ProcessStartInfo("shutdown", "/r /t 3") { UseShellExecute = true });
        }
        else
        {
            _isExiting = false;
        }
    }

    private void Sleep_Click(object sender, MouseButtonEventArgs e)
    {
        if (_isExiting) return;
        _isExiting = true;

        var result = MessageBox.Show("Put the system into Sleep Mode?",
            "Sleep Mode", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            Close();
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
        else
        {
            _isExiting = false;
        }
    }

    private void Resume_Click(object sender, MouseButtonEventArgs e) => Close();
    private void CloseMenu_Click(object sender, MouseButtonEventArgs e) => Close();

    private void Overlay_Click(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource == OverlayBg)
            Close();
    }

    private void OnControllerButton(ControllerButtons button)
    {
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
