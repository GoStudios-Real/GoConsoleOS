using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GoConsoleOS.Shared;

namespace GoConsoleOS.GoConsole.Views;

public partial class StorageView : UserControl
{
    private readonly string _rootPath;

    public StorageView()
    {
        InitializeComponent();
        _rootPath = ConfigReader.RootPath ?? "";
        LoadStorageInfo();
    }

    private void LoadStorageInfo()
    {
        try
        {
            var screenshotsDir = Path.Combine(_rootPath, "system", "screenshots");
            var logsDir = Path.Combine(_rootPath, "system", "logs");
            var cacheDir = Path.Combine(_rootPath, "system", "cache");

            var screenshotSize = GetDirSize(screenshotsDir);
            var logsSize = GetDirSize(logsDir);
            var cacheSize = GetDirSize(cacheDir);
            var total = screenshotSize + logsSize + cacheSize;

            StorageScreenshots.Text = FormatSize(screenshotSize);
            StorageLogs.Text = FormatSize(logsSize);
            StorageCache.Text = FormatSize(cacheSize);
            StorageTotal.Text = FormatSize(total);
            StorageSummary.Text = $"{FormatSize(total)} used across system storage";

            var gameLib = Path.Combine(_rootPath, "launcher", "library");
            if (Directory.Exists(gameLib))
            {
                var gameDirs = Directory.GetDirectories(gameLib).Length;
                var gameSize = GetDirSize(gameLib);
                GameLibraryInfo.Text = $"{gameDirs} game folders  •  {FormatSize(gameSize)} total";
            }
            else
            {
                GameLibraryInfo.Text = "No game library found";
            }
        }
        catch (Exception ex)
        {
            StorageSummary.Text = $"Error: {ex.Message}";
        }
    }

    private static long GetDirSize(string dir)
    {
        if (!Directory.Exists(dir)) return 0;
        try { return Directory.GetFiles(dir, "*", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length); }
        catch { return 0; }
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes}B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024}KB";
        if (bytes < 1024L * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F1}MB";
        return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2}GB";
    }

    private void ClearScreenshots(object sender, MouseButtonEventArgs e)
    {
        if (MessageBox.Show("Delete all screenshots?", "Clear Screenshots",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            var dir = Path.Combine(_rootPath, "system", "screenshots");
            if (Directory.Exists(dir))
            {
                foreach (var f in Directory.GetFiles(dir, "*.png")) File.Delete(f);
            }
            LoadStorageInfo();
        }
    }

    private void ClearLogs(object sender, MouseButtonEventArgs e)
    {
        if (MessageBox.Show("Delete all log files?", "Clear Logs",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            var dir = Path.Combine(_rootPath, "system", "logs");
            if (Directory.Exists(dir))
            {
                foreach (var f in Directory.GetFiles(dir, "*.log")) File.Delete(f);
            }
            LoadStorageInfo();
        }
    }

    private void ClearCache(object sender, MouseButtonEventArgs e)
    {
        if (MessageBox.Show("Delete all cached data?", "Clear Cache",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            var dir = Path.Combine(_rootPath, "system", "cache");
            if (Directory.Exists(dir))
            {
                foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories)) File.Delete(f);
            }
            LoadStorageInfo();
        }
    }
}
