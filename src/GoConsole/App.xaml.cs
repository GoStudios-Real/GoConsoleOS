using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using GoConsoleOS.Shared;

namespace GoConsoleOS.GoConsole;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var rootPath = DetectRootPath();
        ConfigReader.SetRootPath(rootPath);

        var config = ConfigReader.ReadInitConfig();
        var logDir = ConfigReader.ResolvePath("system\\logs");
        Logger.Initialize(logDir, config.Logging.LogLevel, config.Logging.MaxLogSizeMb);

        Logger.Info("GoConsole shell starting");

        this.DispatcherUnhandledException += (_, args) =>
        {
            Logger.Error($"Unhandled exception: {args.Exception}");
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            Logger.Error($"AppDomain unhandled: {args.ExceptionObject}");

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Logger.Error($"Unobserved task exception: {args.Exception}");
            args.SetObserved();
        };

        KillOrphanedWebView2();

        // Boot sequence: show the boot screen, then launch the main shell
        var boot = new BootScreen();
        boot.Show();

        var splashMs = Math.Clamp(config.General.SplashDurationMs, 1000, 8000);
        var bootTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(splashMs) };
        bootTimer.Tick += (_, _) =>
        {
            bootTimer.Stop();
            try
            {
                var main = new MainWindow();
                MainWindow = main;
                main.Show();
                boot.Finish();
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to launch shell: {ex}");
                Shutdown();
            }
        };
        bootTimer.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        KillOrphanedWebView2();
        base.OnExit(e);
    }

    private static void KillOrphanedWebView2()
    {
        try
        {
            var dataDir = Path.Combine(ConfigReader.RootPath ?? "", "system", "webview2");
            Logger.Info($"WebView2 cleanup check, data dir: {dataDir}");
            using var searcher = new ManagementObjectSearcher(
                "SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name = 'msedgewebview2.exe'");
            var total = 0;
            var matched = 0;
            var killed = 0;
            foreach (var o in searcher.Get())
            {
                total++;
                var cmd = o["CommandLine"]?.ToString() ?? "";
                if (cmd.IndexOf(dataDir, StringComparison.OrdinalIgnoreCase) < 0) continue;
                matched++;
                if (int.TryParse(o["ProcessId"]?.ToString(), out var pid))
                {
                    try
                    {
                        Process.GetProcessById(pid)?.Kill();
                        killed++;
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"WebView2 kill {pid}: {ex.Message}");
                    }
                }
                else
                {
                    Logger.Warn($"WebView2 pid parse failed: '{o["ProcessId"]}'");
                }
            }
            if (total > 0 || killed > 0)
                Logger.Info($"WebView2 cleanup: {total} total, {matched} matched, {killed} killed");
        }
        catch (Exception ex)
        {
            Logger.Warn($"WebView2 cleanup: {ex}");
        }
    }

    private static string DetectRootPath()
    {
        var exeDir = Path.GetDirectoryName(Environment.ProcessPath);
        if (exeDir == null) exeDir = Directory.GetCurrentDirectory();

        var check = exeDir;
        while (check != null)
        {
            if (File.Exists(Path.Combine(check, "boot", "init.cfg")))
                return check;
            check = Directory.GetParent(check)?.FullName;
        }

        return exeDir;
    }
}
