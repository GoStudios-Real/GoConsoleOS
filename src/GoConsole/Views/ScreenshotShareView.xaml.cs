using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using GoConsoleOS.Shared;

namespace GoConsoleOS.GoConsole.Views;

public partial class ScreenshotShareView : UserControl
{
    private string? _selectedPath;

    public ScreenshotShareView()
    {
        InitializeComponent();
        LoadScreenshots();
    }

    private void LoadScreenshots()
    {
        var dir = Path.Combine(ConfigReader.RootPath ?? "", "system", "screenshots");
        if (!Directory.Exists(dir))
        {
            ShareStatus.Text = "No screenshots yet. Press F12 to capture.";
            return;
        }

        var files = Directory.GetFiles(dir, "*.png")
                     .Select(f => new { Path = f, Name = Path.GetFileName(f) })
                     .ToList();

        if (files.Count == 0)
        {
            ShareStatus.Text = "No screenshots yet. Press F12 to capture.";
            return;
        }

        ScreenshotGrid.ItemsSource = files;
        ShareStatus.Text = $"{files.Count} screenshot{(files.Count == 1 ? "" : "s")} available";
    }

    private void SelectScreenshot(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string path)
        {
            _selectedPath = path;
            SelectedName.Text = Path.GetFileName(path);
            ExportDesktopBtn.IsEnabled = true;
            ShareClipboardBtn.IsEnabled = true;
        }
    }

    private void ExportToDesktop(object sender, RoutedEventArgs e)
    {
        if (_selectedPath == null) return;
        try
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var dest = Path.Combine(desktop, Path.GetFileName(_selectedPath));
            File.Copy(_selectedPath, dest, true);
            ShareStatus.Text = $"Exported to: {dest}";
            ToastManager.Show("Screenshot exported to desktop");
        }
        catch (Exception ex)
        {
            ShareStatus.Text = $"Export failed: {ex.Message}";
        }
    }

    private void CopyToClipboard(object sender, RoutedEventArgs e)
    {
        if (_selectedPath == null) return;
        try
        {
            var img = new BitmapImage();
            img.BeginInit();
            img.UriSource = new Uri(_selectedPath);
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.EndInit();
            Clipboard.SetImage(img);
            ShareStatus.Text = "Copied to clipboard!";
            ToastManager.Show("Screenshot copied to clipboard");
        }
        catch (Exception ex)
        {
            ShareStatus.Text = $"Copy failed: {ex.Message}";
        }
    }
}
