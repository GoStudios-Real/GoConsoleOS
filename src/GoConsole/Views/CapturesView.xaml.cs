using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using GoConsoleOS.Shared;

namespace GoConsoleOS.GoConsole.Views;

public partial class CapturesView : UserControl
{
    private readonly string _screenshotDir;
    private List<CaptureItem> _captures = new();

    public CapturesView()
    {
        InitializeComponent();
        _screenshotDir = Path.Combine(ConfigReader.RootPath ?? "", "system", "screenshots");
        LoadCaptures();
    }

    private void LoadCaptures()
    {
        _captures.Clear();
        if (!Directory.Exists(_screenshotDir))
        {
            CapturesDetail.Text = "No captures yet. Press F12 to take a screenshot.";
            CapturesGrid.ItemsSource = null;
            return;
        }

        var files = Directory.GetFiles(_screenshotDir, "*.png");
        Array.Sort(files, (a, b) => string.Compare(b, a, StringComparison.OrdinalIgnoreCase));

        foreach (var file in files)
        {
            try
            {
                var info = new FileInfo(file);
                _captures.Add(new CaptureItem
                {
                    Path = file,
                    Name = info.Name,
                    ThumbPath = file,
                    Size = FormatSize(info.Length),
                    Date = info.LastWriteTime
                });
            }
            catch { }
        }

        CapturesGrid.ItemsSource = _captures;
        CapturesDetail.Text = $"{_captures.Count} capture{(_captures.Count == 1 ? "" : "s")}  |  Click to zoom  |  Click ✕ to delete";
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes}B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024}KB";
        return $"{bytes / (1024.0 * 1024.0):F1}MB";
    }

    private void Capture_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is CaptureItem item)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(item.Path);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                ZoomImage.Source = bitmap;
                ZoomOverlay.Visibility = Visibility.Visible;
            }
            catch
            {
                MessageBox.Show("Could not open this capture.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private void ZoomOverlay_Click(object sender, MouseButtonEventArgs e)
    {
        ZoomOverlay.Visibility = Visibility.Collapsed;
        ZoomImage.Source = null;
    }

    private void DeleteCapture_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is CaptureItem item)
        {
            var result = MessageBox.Show($"Delete \"{item.Name}\"?", "Delete Capture",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    File.Delete(item.Path);
                    _captures.Remove(item);
                    CapturesGrid.ItemsSource = null;
                    CapturesGrid.ItemsSource = _captures;
                    CapturesDetail.Text = $"{_captures.Count} capture{(_captures.Count == 1 ? "" : "s")}  |  Click to zoom  |  Click ✕ to delete";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not delete: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
    }

    public class CaptureItem
    {
        public string Path { get; set; } = "";
        public string Name { get; set; } = "";
        public string ThumbPath { get; set; } = "";
        public string Size { get; set; } = "";
        public DateTime Date { get; set; }
    }
}
