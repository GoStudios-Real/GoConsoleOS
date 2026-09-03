using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace GoConsoleWatcher;

public partial class App : Application
{
    private NotifyIcon? _trayIcon;
    private UsbWatcher? _usbWatcher;
    private Window? _hiddenWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        StartupManager.EnsureRegistered();

        CreateHiddenWindow();
        CreateTrayIcon();

        _usbWatcher = new UsbWatcher(Log, ShowBalloon);
        _usbWatcher.Attach(_hiddenWindow!);

        _trayIcon!.Visible = true;
    }

    private void CreateHiddenWindow()
    {
        _hiddenWindow = new Window
        {
            WindowStyle = WindowStyle.None,
            ShowInTaskbar = false,
            Width = 0,
            Height = 0,
            Visibility = Visibility.Hidden
        };

        // Force the window handle to be created
        var helper = new System.Windows.Interop.WindowInteropHelper(_hiddenWindow);
        _ = helper.Handle;
    }

    private void CreateTrayIcon()
    {
        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Launch GoConsoleOS", null, OnLaunchGoConsoleOS);
        contextMenu.Items.Add("Check for USB", null, OnCheckForUsb);
        contextMenu.Items.Add("-");
        contextMenu.Items.Add("Exit", null, OnExit);

        var icon = SystemIcons.Application;

        try
        {
            var iconPath = Path.Combine(AppContext.BaseDirectory, "GoConsoleWatcher.ico");
            if (File.Exists(iconPath))
                icon = new Icon(iconPath);
        }
        catch
        {
            // use default
        }

        _trayIcon = new NotifyIcon
        {
            Text = "GoConsoleOS USB Watcher",
            Icon = icon,
            ContextMenuStrip = contextMenu,
            Visible = true
        };

        _trayIcon.DoubleClick += (s, ev) => OnLaunchGoConsoleOS(s, ev);
    }

    private void OnLaunchGoConsoleOS(object? sender, EventArgs e)
    {
        try
        {
            // Look for GoConsoleOS.exe in common locations
            var baseDir = AppContext.BaseDirectory;
            var candidates = new[]
            {
                Path.Combine(baseDir, "..", "..", "..", "..", "GoConsole", "bin", "x64", "Release", "net8.0-windows", "GoConsoleOS.exe"),
                Path.Combine(baseDir, "GoConsoleOS.exe"),
            };

            foreach (var candidate in candidates)
            {
                var fullPath = Path.GetFullPath(candidate);
                if (File.Exists(fullPath))
                {
                    Log($"Launching GoConsoleOS from {fullPath}");
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = fullPath,
                        UseShellExecute = true
                    });
                    return;
                }
            }

            Log("GoConsoleOS.exe not found");
        }
        catch (Exception ex)
        {
            Log($"Error launching GoConsoleOS: {ex.Message}");
        }
    }

    private void OnCheckForUsb(object? sender, EventArgs e)
    {
        Log("Manual USB check requested - insert USB drive to trigger auto-detection");
        ShowBalloon("Insert a USB drive with GoConsoleOS to auto-launch");
    }

    private void OnExit(object? sender, EventArgs e)
    {
        _usbWatcher?.Detach();
        _usbWatcher?.Dispose();

        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }

        _hiddenWindow?.Close();
        Shutdown();
    }

    private void Log(string message)
    {
        Debug.WriteLine($"[GoConsoleWatcher] {message}");
    }

    private void ShowBalloon(string message)
    {
        _trayIcon?.ShowBalloonTip(3000, "GoConsoleOS Watcher", message, ToolTipIcon.Info);
    }
}
