using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using GoConsoleOS.Shared;
using GoConsoleOS.Shared.Models;
using GoConsoleOS.Shared.Input;

namespace GoConsoleOS.GoCore;

public class App : Application
{
    private static SplashWindow? _splash;
    private static ControllerEngine? _controller;
    private static InitConfig? _config;

    [STAThread]
    static void Main(string[] args)
    {
        var rootPath = DetectRootPath();
        ConfigReader.SetRootPath(rootPath);
        _config = ConfigReader.ReadInitConfig();

        var logDir = ConfigReader.ResolvePath("system\\logs");
        Logger.Initialize(logDir, _config.Logging.LogLevel, _config.Logging.MaxLogSizeMb);

        Logger.Info($"=== GoConsoleOS v{_config.General.Version} Boot ===");
        Logger.Info($"Root path: {rootPath}");

        var app = new App();
        app.Startup += (_, _) => BootSequence(args);
        app.Run();
    }

    private static void BootSequence(string[] args)
    {
        _splash = new SplashWindow();
        _splash.Show();

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        var step = 0;

        timer.Tick += async (_, _) =>
        {
            step++;
            switch (step)
            {
                case 1:
                    _splash!.SetStatus("Detecting hardware...");
                    CheckSystemRequirements();
                    break;
                case 2:
                    _splash!.SetStatus("Starting controller service...");
                    if (_config!.Services.XinputEnabled)
                        StartControllerService();
                    break;
                case 3:
                    _splash!.SetStatus("Scanning game libraries...");
                    var platforms = PlatformDetection.GetInstalledPlatforms();
                    foreach (var (name, installed) in platforms)
                        Logger.Info($"  Platform '{name}': {(installed ? "installed" : "not found")}");
                    break;
                case 4:
                    _splash!.SetStatus("Preparing console shell...");
                    break;
                case 5:
                    _splash!.SetStatus("Almost ready...");
                    break;
                case 6:
                    timer.Stop();
                    _splash!.SetStatus("Ready!");

                    await Task.Delay(300);

                    if (_config!.Boot.AutoLaunchShell)
                        LaunchShell();

                    _splash.Complete();
                    _splash = null;
                    break;
            }
        };

        timer.Start();
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

    private static void CheckSystemRequirements()
    {
        try
        {
            Logger.Info($"OS: {Environment.OSVersion.VersionString}");
            Logger.Info($"Windows 11 detected: {PlatformDetection.IsWindows11()}");
        }
        catch (Exception ex)
        {
            Logger.Error($"System check failed: {ex.Message}");
        }
    }

    private static void StartControllerService()
    {
        try
        {
            _controller = new ControllerEngine(0, _config!.Services.ControllerPollRate);
            _controller.ButtonPressed += (button) =>
            {
                if (button == ControllerButtons.Guide)
                    Logger.Debug("Guide button pressed");
            };
            _controller.Start();
            Logger.Info("Controller service started");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to start controller service: {ex.Message}");
        }
    }

    private static void LaunchShell()
    {
        var shellPath = ConfigReader.ResolvePath(_config!.Boot.ShellPath);
        if (!File.Exists(shellPath))
        {
            Logger.Error($"Shell not found at: {shellPath}");
            return;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = shellPath,
                WorkingDirectory = Path.GetDirectoryName(shellPath),
                UseShellExecute = true
            };
            Process.Start(psi);
            Logger.Info($"Launched shell: {shellPath}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to launch shell: {ex.Message}");
        }
    }
}
