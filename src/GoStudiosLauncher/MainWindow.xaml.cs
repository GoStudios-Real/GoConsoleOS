using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace GoStudiosLauncher;

public partial class MainWindow : Window
{
    private DispatcherTimer _clockTimer;

    public MainWindow()
    {
        InitializeComponent();
        LoadProjects();

        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) =>
        {
            ClockText.Text = DateTime.Now.ToString("HH:mm:ss");
            StatusClockText.Text = DateTime.Now.ToString("ddd, MMM dd  HH:mm:ss");
        };
        _clockTimer.Start();

        UpdateSystemInfo();
    }

    private void LoadProjects()
    {
        var projects = new[]
        {
            new ProjectInfo
            {
                Id = "goconsole",
                Name = "GoConsoleOS",
                Version = "v1.5.0",
                Icon = "🎮",
                Description = "Console Mode for Windows 11. Transform your PC into a Steam Big Picture / Xbox-style gaming console with controller navigation, performance overlays, and unified game library.",
                InstallPath = @"..\..\..\..\boot\GoConsole.exe",
                Status = "Installed"
            },
            new ProjectInfo
            {
                Id = "gocore",
                Name = "GoCore",
                Version = "v1.5.0",
                Icon = "⚙",
                Description = "Core system service that provides performance monitoring, controller input, and system optimization services for the GoStudios Corporation ecosystem.",
                InstallPath = @"..\..\..\..\boot\gocore.exe",
                Status = "Installed"
            },
            new ProjectInfo
            {
                Id = "gobrowser",
                Name = "GoBrowser",
                Version = "v1.5.0",
                Icon = "🌐",
                Description = "A fast, lightweight, game-optimized web browser integrated directly into the GoConsoleOS ecosystem with controller-first navigation.",
                InstallPath = null,
                Status = "Available"
            },
            new ProjectInfo
            {
                Id = "gomedia",
                Name = "GoMedia",
                Version = "v1.5.0",
                Icon = "🎬",
                Description = "Media center for GoConsoleOS. Watch movies, listen to music, and view screenshots — all optimized for controller navigation.",
                InstallPath = null,
                Status = "Available"
            },
            new ProjectInfo
            {
                Id = "gotools",
                Name = "GoTools",
                Version = "v1.5.0",
                Icon = "🔧",
                Description = "System tools and utilities for GoConsoleOS including file manager, performance tuner, and system diagnostics.",
                InstallPath = null,
                Status = "Available"
            },
            new ProjectInfo
            {
                Id = "gostudios",
                Name = "GoStudios Corporation SDK",
                Version = "v1.5.0",
                Icon = "💻",
                Description = "Software Development Kit for building custom GoConsoleOS plugins, themes, overlays, and extensions.",
                InstallPath = null,
                Status = "Available"
            }
        };

        ProjectsList.ItemsSource = projects;
    }

    private void LaunchProject(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string projectId)
        {
            var project = ((System.Collections.IEnumerable)ProjectsList.ItemsSource)
                .Cast<ProjectInfo>()
                .FirstOrDefault(p => p.Id == projectId);

            if (project == null) return;

            // Handle built-in projects
            switch (projectId)
            {
                case "gobrowser":
                    var browser = new BrowserDialog();
                    browser.Owner = this;
                    browser.ShowDialog();
                    return;

                case "gomedia":
                    PlayMediaFiles();
                    return;

                case "gotools":
                    ShowSystemTools();
                    return;
            }

            // Standard executable launch
            if (string.IsNullOrEmpty(project.InstallPath) || !System.IO.File.Exists(project.InstallPath))
            {
                MessageBox.Show(
                    $"{project.Name} is not installed yet.\n\nPlease install it first using the INSTALL button.",
                    "Not Installed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            try
            {
                var fullPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, project.InstallPath));
                Process.Start(new ProcessStartInfo
                {
                    FileName = fullPath,
                    UseShellExecute = true,
                    WorkingDirectory = System.IO.Path.GetDirectoryName(fullPath)
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to launch {project.Name}: {ex.Message}",
                    "Launch Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void PlayMediaFiles()
    {
        var musicDir = System.IO.Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "system", "music");
        if (!System.IO.Directory.Exists(musicDir))
        {
            MessageBox.Show("No media folder found. Place .mp3 files in the system/music/ directory.",
                "GoMedia", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var files = System.IO.Directory.GetFiles(musicDir, "*.mp3");
        if (files.Length == 0)
        {
            MessageBox.Show("No media files found. Add .mp3 files to system/music/ to use GoMedia.",
                "GoMedia", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var msg = $"GoMedia found {files.Length} audio file{(files.Length == 1 ? "" : "s")}:\n\n";
        msg += string.Join("\n", files.Select(f => "  ♪ " + System.IO.Path.GetFileNameWithoutExtension(f)).Take(20));
        if (files.Length > 20) msg += $"\n  ... and {files.Length - 20} more";

        var play = MessageBox.Show(msg + "\n\nOpen the containing folder?", "GoMedia",
            MessageBoxButton.YesNo, MessageBoxImage.Information);
        if (play == MessageBoxResult.Yes)
            Process.Start("explorer.exe", musicDir);
    }

    private void ShowSystemTools()
    {
        var info = new System.Text.StringBuilder();
        info.AppendLine("===== GoTools System Diagnostics =====\n");
        info.AppendLine($"OS: {Environment.OSVersion}");
        info.AppendLine($"Machine: {Environment.MachineName}");
        info.AppendLine($"User: {Environment.UserName}");
        info.AppendLine($"Processors: {Environment.ProcessorCount} cores");
        info.AppendLine($"System RAM: {GetTotalRamMB()} MB");
        info.AppendLine($"64-bit OS: {Environment.Is64BitOperatingSystem}");
        info.AppendLine($"Runtime: .NET {Environment.Version}");
        info.AppendLine($"Up time: {TimeSpan.FromMilliseconds(Environment.TickCount64):dd\\d\\ hh\\h\\ mm\\m}");
        info.AppendLine($"\nDrives:");
        foreach (var drive in System.IO.DriveInfo.GetDrives().Where(d => d.IsReady))
            info.AppendLine($"  {drive.Name} {drive.TotalSize / 1073741824} GB total, {(drive.TotalFreeSpace / 1073741824)} GB free");

        ShowInfoDialog("GoTools — System Diagnostics", info.ToString());
    }

    private static long GetTotalRamMB()
    {
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
            foreach (var obj in searcher.Get())
                return Convert.ToInt64(obj["TotalPhysicalMemory"]) / 1048576;
        }
        catch { }
        return 0;
    }

    private static void ShowInfoDialog(string title, string content)
    {
        var bg = new SolidColorBrush(Color.FromRgb(0x0D, 0x0D, 0x14));
        var accent = new SolidColorBrush(Color.FromRgb(0x00, 0xC9, 0xDB));
        var light = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x2E));
        var text = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0xAA));
        var white = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xFF));
        var dark = new SolidColorBrush(Color.FromRgb(0x0D, 0x0D, 0x14));

        var dlg = new Window
        {
            Title = title,
            Width = 520,
            Height = 420,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            WindowStyle = WindowStyle.None,
            Background = bg,
            ResizeMode = ResizeMode.NoResize,
            Topmost = true,
            Content = new Grid
            {
                Margin = new Thickness(20),
                RowDefinitions =
                {
                    new RowDefinition { Height = new GridLength(0, GridUnitType.Auto) },
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) },
                    new RowDefinition { Height = new GridLength(0, GridUnitType.Auto) }
                },
                Children =
                {
                    new TextBlock
                    {
                        Text = title,
                        FontSize = 20,
                        FontWeight = FontWeights.Bold,
                        Foreground = accent,
                        Margin = new Thickness(0, 0, 0, 12)
                    },
                    new Border
                    {
                        Background = light,
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(16, 16, 16, 16),
                        Child = new TextBlock
                        {
                            Text = content,
                            FontSize = 13,
                            Foreground = text,
                            FontFamily = new FontFamily("Consolas"),
                            TextWrapping = TextWrapping.Wrap
                        }
                    },
                    new Button
                    {
                        Content = "CLOSE",
                        FontSize = 14,
                        FontWeight = FontWeights.Bold,
                        Foreground = dark,
                        Background = accent,
                        BorderThickness = new Thickness(0),
                        Padding = new Thickness(16, 8, 16, 8),
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Cursor = Cursors.Hand,
                        Margin = new Thickness(0, 12, 0, 0)
                    }
                }
            }
        };

        var btn = (Button)((Grid)dlg.Content).Children[2];
        btn.Click += (_, _) => dlg.Close();
        dlg.KeyDown += (_, e) => { if (e.Key == Key.Escape) dlg.Close(); };

        dlg.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsVisible);
        dlg.ShowDialog();
    }

    private void Refresh_Click(object sender, MouseButtonEventArgs e)
    {
        LoadProjects();
    }

    private void CloseButton_Click(object sender, MouseButtonEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void UpdateSystemInfo()
    {
        try
        {
            var battery = BatteryManager.GetSystemBattery();
            if (battery.IsPresent)
                BatteryText.Text = battery.IsCharging ? $"⚡ {battery.Percent}%" : $"🔋 {battery.Percent}%";
            else
                BatteryText.Text = "🔌 AC";
        }
        catch
        {
            BatteryText.Text = "🔌 AC";
        }
    }

    public class ProjectInfo
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Version { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Description { get; set; } = "";
        public string? InstallPath { get; set; }
        public string Status { get; set; } = "";
    }

    private class BatteryManager
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool GetSystemPowerStatus(ref SYSTEM_POWER_STATUS sps);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct SYSTEM_POWER_STATUS
        {
            public byte ACLineStatus;
            public byte BatteryFlag;
            public byte BatteryLifePercent;
            public byte SystemStatusFlag;
            public uint BatteryLifeTime;
            public uint BatteryFullLifeTime;
        }

        public static BatteryInfo GetSystemBattery()
        {
            var sps = new SYSTEM_POWER_STATUS();
            if (GetSystemPowerStatus(ref sps))
            {
                return new BatteryInfo
                {
                    Percent = sps.BatteryLifePercent,
                    IsCharging = sps.ACLineStatus == 1,
                    IsPresent = sps.BatteryLifePercent <= 100
                };
            }
            return new BatteryInfo { Percent = 0, IsCharging = false, IsPresent = false };
        }
    }

    public struct BatteryInfo
    {
        public int Percent;
        public bool IsCharging;
        public bool IsPresent;
    }
}
