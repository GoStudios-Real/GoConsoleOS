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

    public UsbGamingConsoleMakerView()
    {
        InitializeComponent();
        RefreshDrives();
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
        var confirm = MessageBox.Show(
            $"This will erase ALL files on {target} and install GoConsoleOS.\n\n" +
            $"Drive: {target}\nLabel: {_selected.Label}\n\nContinue?",
            "USB Gaming Console Maker",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;

        _busy = true;
        InstallBtn.IsEnabled = false;
        InstallProgress.Visibility = Visibility.Visible;
        InstallProgress.Value = 0;
        InstallStatus.Text = "Starting installation...";

        var source = AppContext.BaseDirectory;
        Logger.Info($"USB install started: source={source} target={target}");

        try
        {
            await Task.Run(() => UsbInstaller.RunInstall(source, target, Report));
            var ok = UsbInstaller.Verify(target);
            Logger.Info($"USB install completed on {target} (verified={ok})");
            InstallStatus.Text = ok
                ? "Installation complete — GoConsoleOS ready on " + target
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
