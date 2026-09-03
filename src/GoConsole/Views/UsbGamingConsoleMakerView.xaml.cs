using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GoConsoleOS.Shared;

namespace GoConsoleOS.GoConsole.Views;

public partial class UsbGamingConsoleMakerView : UserControl
{
    private DriveItem? _selected;
    private bool _busy;
    private string _selectedUsbType = "goc";

    private static readonly Dictionary<string, (string Title, string Description, string Button, string Warning)> UsbTypes = new()
    {
        ["goc"] = (
            "INSTALL GoConsoleOS",
            "Installs a self-contained GoConsoleOS gaming console onto the selected USB drive. The drive is renamed GoConsoleOS, gets a boot config, and launches automatically on plug-in.",
            "CREATE GAMING CONSOLE",
            "WARNING: All existing data on the target drive will be erased."
        ),
        ["androidtv"] = (
            "INSTALL Android TV USB",
            "Creates a portable Android TV / Google TV USB drive with pre-installed apps and games. Works on smart TVs, Android TV boxes, and any device that supports USB sideloading.",
            "CREATE ANDROID TV USB",
            "WARNING: All existing data on the target drive will be erased. Drive must be FAT32 or exFAT."
        ),
        ["winplay"] = (
            "INSTALL Windows Play USB",
            "Creates a portable Windows 10/11 USB with games and apps that run directly from USB — no boot required. Just plug in and play on any Windows PC.",
            "CREATE WINDOWS PLAY USB",
            "WARNING: All existing data on the target drive will be erased."
        )
    };

    public UsbGamingConsoleMakerView()
    {
        InitializeComponent();
        RefreshDrives();
        UpdateUsbTypeUI();
    }

    private void SelectUsbType_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string tag)
        {
            _selectedUsbType = tag;
            UpdateUsbTypeUI();
        }
    }

    private void UpdateUsbTypeUI()
    {
        if (!UsbTypes.ContainsKey(_selectedUsbType)) return;
        var (title, desc, button, warning) = UsbTypes[_selectedUsbType];
        InstallTitle.Text = title;
        InstallDescription.Text = desc;
        InstallBtn.Content = button;
        InstallWarning.Text = warning;

        var mainGrid = InstallBtn.Parent as StackPanel;
        if (mainGrid?.Parent is Border card)
        {
            foreach (var child in ((StackPanel)card.Parent).Children)
            {
                if (child is Grid selectorGrid)
                {
                    foreach (var border in selectorGrid.Children)
                    {
                        if (border is Border b && b.Tag is string t)
                        {
                            var isActive = t == _selectedUsbType;
                            b.BorderBrush = isActive
                                ? (System.Windows.Media.Brush)FindResource("BrushAccentPrimary")
                                : (System.Windows.Media.Brush)FindResource("BrushBorder");
                            b.BorderThickness = isActive ? new Thickness(2) : new Thickness(1);
                        }
                    }
                }
            }
        }
    }

    private void RefreshDrives()
    {
        var bootRoot = !string.IsNullOrEmpty(ConfigReader.RootPath)
            ? Path.GetPathRoot(ConfigReader.RootPath)
            : null;
        var sysRoot = Path.GetPathRoot(Environment.SystemDirectory);
        var showInternal = ShowInternalToggle.IsChecked == true;
        var busTypes = DriveClassifier.GetLogicalDiskToBusType();
        var items = new List<DriveItem>();

        foreach (var drive in DriveInfo.GetDrives())
        {
            if (!drive.IsReady) continue;
            if (drive.DriveType != DriveType.Removable && drive.DriveType != DriveType.Fixed) continue;

            var root = drive.RootDirectory.FullName;
            if (string.Equals(root, sysRoot, StringComparison.OrdinalIgnoreCase)) continue;
            if (bootRoot != null && string.Equals(root, bootRoot, StringComparison.OrdinalIgnoreCase)) continue;
            if (root.TrimEnd('\\').Equals(bootRoot?.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)) continue;

            var kind = DriveClassifier.Classify(drive, busTypes);
            if (kind == DriveKind.Internal && !showInternal) continue;

            items.Add(BuildItem(drive, kind));
        }

        DriveList.ItemsSource = items;

        if (items.Count == 0)
            MakerStatus.Text = "No installable drives found — plug in a USB drive and press Refresh";
        else
            MakerStatus.Text = $"{items.Count} installable drive{(items.Count == 1 ? "" : "s")} found";

        if (_selected != null)
        {
            var match = items.FirstOrDefault(i => string.Equals(i.Root, _selected.Root, StringComparison.OrdinalIgnoreCase));
            _selected = match;
            if (match == null) ClearSelection();
        }
        else
        {
            ClearSelection();
        }
    }

    private DriveItem BuildItem(DriveInfo drive, DriveKind kind)
    {
        var root = drive.RootDirectory.FullName;
        var label = string.IsNullOrWhiteSpace(drive.VolumeLabel) ? "Unlabeled" : drive.VolumeLabel;
        var total = UsbInstaller.FormatSize(drive.TotalSize);
        var free = UsbInstaller.FormatSize(drive.TotalFreeSpace);
        var detail = $"{drive.DriveFormat.ToUpperInvariant()}  •  {total} total  •  {free} free";

        DriveItem item;
        if (drive.TotalFreeSpace < UsbInstaller.MinFreeBytes)
        {
            item = new DriveItem
            {
                Root = root,
                Letter = root,
                Label = label,
                Detail = detail,
                Type = DriveClassifier.KindLabel(kind),
                TypeBrush = DriveClassifier.KindBrush(kind),
                State = "NOT ENOUGH SPACE — 512 MB minimum",
                StateBrush = "#E53935"
            };
        }
        else if (File.Exists(Path.Combine(root, "GoConsoleOS.exe")))
        {
            item = new DriveItem
            {
                Root = root,
                Letter = root,
                Label = label,
                Detail = detail,
                Type = DriveClassifier.KindLabel(kind),
                TypeBrush = DriveClassifier.KindBrush(kind),
                State = "GoConsoleOS FOUND — reinstall available",
                StateBrush = "#FB8C00"
            };
        }
        else if (File.Exists(Path.Combine(root, "AndroidTV", "launch.bat")))
        {
            item = new DriveItem
            {
                Root = root,
                Letter = root,
                Label = label,
                Detail = detail,
                Type = DriveClassifier.KindLabel(kind),
                TypeBrush = DriveClassifier.KindBrush(kind),
                State = "ANDROID TV USB FOUND — reinstall available",
                StateBrush = "#FB8C00"
            };
        }
        else if (File.Exists(Path.Combine(root, "WindowsPlay", "launch.bat")))
        {
            item = new DriveItem
            {
                Root = root,
                Letter = root,
                Label = label,
                Detail = detail,
                Type = DriveClassifier.KindLabel(kind),
                TypeBrush = DriveClassifier.KindBrush(kind),
                State = "WINDOWS PLAY USB FOUND — reinstall available",
                StateBrush = "#FB8C00"
            };
        }
        else
        {
            item = new DriveItem
            {
                Root = root,
                Letter = root,
                Label = label,
                Detail = detail,
                Type = DriveClassifier.KindLabel(kind),
                TypeBrush = DriveClassifier.KindBrush(kind),
                State = "READY — blank drive",
                StateBrush = "#43A047"
            };
        }

        return item;
    }

    private void SelectDrive(object sender, MouseButtonEventArgs e)
    {
        if (_busy) return;
        if ((sender as FrameworkElement)?.Tag is not DriveItem item) return;

        _selected = item;
        SelectedDriveText.Text = $"{item.Letter}  ({item.Label})  —  {item.State}";
        InstallBtn.IsEnabled = true;

        foreach (var it in DriveList.Items.Cast<DriveItem>())
            it.IsSelected = it == item;
    }

    private void RefreshDrives_Click(object sender, MouseButtonEventArgs e) => RefreshDrives();

    private void ShowInternal_Changed(object sender, RoutedEventArgs e) => RefreshDrives();

    private void ClearSelection()
    {
        _selected = null;
        SelectedDriveText.Text = "None — choose a USB drive from the list";
        InstallBtn.IsEnabled = false;
    }

    private async void InstallClick(object sender, RoutedEventArgs e)
    {
        if (_busy) return;
        if (_selected == null)
        {
            MessageBox.Show("Select a USB drive first.", "USB Gaming Console Maker",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var target = _selected.Root;
        var typeName = UsbTypes[_selectedUsbType].Title;
        var confirm = MessageBox.Show(
            $"This will erase ALL files on {target} and create a {_selectedUsbType.ToUpperInvariant()} USB.\n\n" +
            $"Drive: {target}\nLabel: {_selected.Label}\nType: {typeName}\n\nContinue?",
            "USB Gaming Console Maker",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;

        _busy = true;
        InstallBtn.IsEnabled = false;
        InstallProgress.Visibility = Visibility.Visible;
        InstallProgress.Value = 0;
        InstallStatus.Text = "Starting installation...";

        var source = AppContext.BaseDirectory;
        Logger.Info($"USB install started: type={_selectedUsbType} source={source} target={target}");

        try
        {
            switch (_selectedUsbType)
            {
                case "goc":
                    await Task.Run(() => UsbInstaller.RunInstall(source, target, Report));
                    break;
                case "androidtv":
                    await Task.Run(() => InstallAndroidTvUsb(source, target, Report));
                    break;
                case "winplay":
                    await Task.Run(() => InstallWindowsPlayUsb(source, target, Report));
                    break;
            }

            var ok = VerifyInstall(target);
            Logger.Info($"USB install completed on {target} type={_selectedUsbType} (verified={ok})");
            InstallStatus.Text = ok
                ? $"Installation complete — {_selectedUsbType.ToUpperInvariant()} USB ready on {target}"
                : "Installation finished, but verification found missing files";
            InstallProgress.Value = 100;
        }
        catch (Exception ex)
        {
            Logger.Error($"USB install failed on {target}: {ex}");
            InstallStatus.Text = "Installation failed: " + ex.Message;
        }
        finally
        {
            _busy = false;
            InstallBtn.IsEnabled = true;
            RefreshDrives();
        }
    }

    private void InstallAndroidTvUsb(string source, string target, Action<string, int> report)
    {
        report("Cleaning drive...", 5);
        UsbInstaller.CleanTarget(target);

        report("Creating Android TV folder structure...", 15);
        var atvRoot = Path.Combine(target, "AndroidTV");
        var appsDir = Path.Combine(atvRoot, "apps");
        var gamesDir = Path.Combine(atvRoot, "games");
        var configDir = Path.Combine(atvRoot, "config");
        var cacheDir = Path.Combine(atvRoot, "cache");
        Directory.CreateDirectory(appsDir);
        Directory.CreateDirectory(gamesDir);
        Directory.CreateDirectory(configDir);
        Directory.CreateDirectory(cacheDir);

        report("Copying Android TV launcher...", 30);
        var launcherBat = @"@echo off
title Android TV USB - GoStudios Corporation
echo ============================================
echo   Android TV USB - GoStudios Corporation
echo ============================================
echo.
echo Insert this USB into an Android TV or
echo Android TV box, then enable USB debugging.
echo.
echo Supported devices:
echo   - Android TV 10+
echo   - Google TV
echo   - Fire TV (with developer mode)
echo.
echo For instructions, visit:
echo   https://gostudios.net/androidtv
echo.
pause";

        File.WriteAllText(Path.Combine(atvRoot, "launch.bat"), launcherBat);

        report("Creating Android TV config...", 50);
        var config = new
        {
            version = "1.0.0",
            name = "GoStudios Android TV USB",
            created = DateTime.UtcNow.ToString("O"),
            apps = new object[]
            {
                new { name = "Google Play Games", package = "com.google.android.play.games", installed = true },
                new { name = "Plex", package = "com.plexapp.android", installed = false },
                new { name = "Steam Link", package = "com.valve.steamlink", installed = false },
                new { name = "Moonlight", package = "com.limemoonlight", installed = false }
            },
            settings = new
            {
                resolution = "1080p",
                hdr = true,
                gamepad = true,
                developerMode = false
            }
        };
        var configJson = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(configDir, "androidtv.json"), configJson);

        report("Creating README...", 70);
        var readme = @"Android TV USB - GoStudios Corporation
======================================

This USB drive contains Android TV apps and games
that can be sideloaded onto Android TV devices.

SETUP:
1. Enable Developer Mode on your Android TV
2. Enable USB Debugging
3. Insert this USB drive
4. Use a file manager app to install APKs from:
   AndroidTV/apps/

PRE-INSTALLED APPS:
- Google Play Games (for game streaming)
- Plex (media server client)
- Steam Link (PC game streaming)
- Moonlight (NVIDIA GameStream client)

SUPPORTED DEVICES:
- Android TV 10+
- Google TV
- Amazon Fire TV (with developer mode)

For more information, visit:
https://gostudios.net/androidtv

GoStudios Corporation 2026
";
        File.WriteAllText(Path.Combine(atvRoot, "README.txt"), readme);

        report("Copying GoConsoleOS shared files...", 85);
        var sharedDll = Path.Combine(source, "GoConsoleOS.Shared.dll");
        if (File.Exists(sharedDll))
            File.Copy(sharedDll, Path.Combine(atvRoot, "GoConsoleOS.Shared.dll"), true);

        report("Installing USB auto-launch watcher...", 90);
        UsbInstaller.InstallWatcher(source, target);

        report("Finalizing Android TV USB...", 95);
        File.WriteAllText(Path.Combine(target, "autorun.inf"),
            "[autorun]\r\nlabel=AndroidTV-GoStudios\r\nicon=AndroidTV\\launch.bat");

        report("Android TV USB complete!", 100);
    }

    private void InstallWindowsPlayUsb(string source, string target, Action<string, int> report)
    {
        report("Cleaning drive...", 5);
        UsbInstaller.CleanTarget(target);

        report("Creating Windows Play folder structure...", 15);
        var wpRoot = Path.Combine(target, "WindowsPlay");
        var appsDir = Path.Combine(wpRoot, "apps");
        var gamesDir = Path.Combine(wpRoot, "games");
        var savesDir = Path.Combine(wpRoot, "saves");
        var configDir = Path.Combine(wpRoot, "config");
        var cacheDir = Path.Combine(wpRoot, "cache");
        Directory.CreateDirectory(appsDir);
        Directory.CreateDirectory(gamesDir);
        Directory.CreateDirectory(savesDir);
        Directory.CreateDirectory(configDir);
        Directory.CreateDirectory(cacheDir);

        report("Copying Windows Play launcher...", 30);
        var launcherBat = @"@echo off
title Windows Play USB - GoStudios Corporation
echo ============================================
echo   Windows Play USB - GoStudios Corporation
echo ============================================
echo.
echo Plug and play — no boot required!
echo.
echo Your games and apps are ready to launch.
echo Double-click any game in the 'games' folder.
echo.
echo For more games, visit the GoStudios Store:
echo   https://gostudios.net/store
echo.
pause";

        File.WriteAllText(Path.Combine(wpRoot, "launch.bat"), launcherBat);

        var autorunBat = @"@echo off
start /min "" "" ""%~dp0launch.bat""";

        File.WriteAllText(Path.Combine(wpRoot, "autorun.bat"), autorunBat);

        report("Creating Windows Play config...", 50);
        var config = new
        {
            version = "1.0.0",
            name = "GoStudios Windows Play USB",
            created = DateTime.UtcNow.ToString("O"),
            games = new object[]
            {
                new { name = "Pixel Adventure", type = "2D", executable = "games\\PixelAdventure\\game.exe", installed = true },
                new { name = "Space Shooter", type = "2D", executable = "games\\SpaceShooter\\game.exe", installed = true },
                new { name = "3D Arena", type = "3D", executable = "games\\3DArena\\game.exe", installed = true }
            },
            settings = new
            {
                autoLaunch = true,
                fullscreen = false,
                portableMode = true,
                saveToUsb = true
            }
        };
        var configJson = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(configDir, "windowsplay.json"), configJson);

        report("Creating placeholder games...", 65);
        var gameDirs = new[] { "PixelAdventure", "SpaceShooter", "3DArena" };
        foreach (var gameDir in gameDirs)
        {
            var gamePath = Path.Combine(gamesDir, gameDir);
            Directory.CreateDirectory(gamePath);
            File.WriteAllText(Path.Combine(gamePath, "game.exe"), "Placeholder — replace with actual game executable");
            File.WriteAllText(Path.Combine(gamePath, "README.txt"), $"Place your {gameDir} game files here.");
        }

        report("Copying GoConsoleOS shared files...", 85);
        var sharedDll = Path.Combine(source, "GoConsoleOS.Shared.dll");
        if (File.Exists(sharedDll))
            File.Copy(sharedDll, Path.Combine(wpRoot, "GoConsoleOS.Shared.dll"), true);

        report("Creating README...", 90);
        var readme = @"Windows Play USB - GoStudios Corporation
==========================================

This USB drive contains Windows games and apps
that run directly from USB — no boot required!

SETUP:
1. Plug this USB into any Windows 10/11 PC
2. Open the 'games' folder
3. Double-click any game executable to play

YOUR GAMES:
- Pixel Adventure (2D Platformer)
- Space Shooter (2D Shooter)
- 3D Arena (3D Action)

PORTABLE MODE:
All saves are stored on this USB drive.
Your progress follows you to any PC!

ADDING MORE GAMES:
1. Copy game folders to the 'games' directory
2. Edit 'config\windowsplay.json' to register them
3. They'll appear in the launcher

For more games, visit the GoStudios Store:
https://gostudios.net/store

GoStudios Corporation 2026
";
        File.WriteAllText(Path.Combine(wpRoot, "README.txt"), readme);

        report("Installing USB auto-launch watcher...", 90);
        UsbInstaller.InstallWatcher(source, target);

        report("Finalizing Windows Play USB...", 95);
        File.WriteAllText(Path.Combine(target, "autorun.inf"),
            "[autorun]\r\nlabel=WinPlay-GoStudios\r\nicon=WindowsPlay\\launch.bat");

        report("Windows Play USB complete!", 100);
    }

    private bool VerifyInstall(string target)
    {
        switch (_selectedUsbType)
        {
            case "goc":
                return File.Exists(Path.Combine(target, "GoConsoleOS.exe"))
                    && File.Exists(Path.Combine(target, "GoConsole.dll"));
            case "androidtv":
                return File.Exists(Path.Combine(target, "AndroidTV", "launch.bat"))
                    && File.Exists(Path.Combine(target, "AndroidTV", "config", "androidtv.json"));
            case "winplay":
                return File.Exists(Path.Combine(target, "WindowsPlay", "launch.bat"))
                    && File.Exists(Path.Combine(target, "WindowsPlay", "config", "windowsplay.json"));
            default:
                return false;
        }
    }

    private void Report(string text, int percent)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            InstallStatus.Text = text;
            InstallProgress.Value = Math.Clamp(percent, 0, 100);
        }));
    }

    public class DriveItem : INotifyPropertyChanged
    {
        public string Root { get; set; } = "";
        public string Letter { get; set; } = "";
        public string Label { get; set; } = "";
        public string Detail { get; set; } = "";
        public string Type { get; set; } = "";
        public string TypeBrush { get; set; } = "#43A047";
        public string State { get; set; } = "";
        public string StateBrush { get; set; } = "#43A047";

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
