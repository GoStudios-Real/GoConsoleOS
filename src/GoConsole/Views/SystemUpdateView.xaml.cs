using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace GoConsoleOS.GoConsole.Views;

public partial class SystemUpdateView : UserControl
{
    private const string CurrentVersion = "1.7.0";
    private const string LatestVersion = "1.7.0";
    private readonly List<string> _history = new()
    {
        "v1.7.0 \u2014 Added a full Controller selection screen (auto-detect, per-kind layouts, live button & stick test, vibration test), USB device health with S.M.A.R.T. scoring, and on-screen scroll with D-Pad scrolling on long pages.",
        "v1.6.0 \u2014 Added controller type selection (Xbox, PS5 DualSense, Nintendo Switch 2), pen and touchscreen support, a Discord KEY button, a GoStudios brand splash screen, the What's New hub, a persistent settings database, 8 achievements with toast notifications, game save backup & restore, true fullscreen boot, accent color picker, CPU detection, and 7 new store items.",
        "v1.5.0 \u2014 Added Discord with chat, voice calls, and friends, the Discord Token Creator, the built-in GoBrowser (WebView2), and the APPS row on the home page.",
        "v1.4.0 \u2014 Added game hubs, deals tracker, backup & restore, and per-game settings.",
        "v1.3.0 \u2014 Added system updates, cloud saves, remote play, compatibility ratings, controller profiles, and the Xbox-style exit menu.",
        "v1.2.0 \u2014 Added themes, platform stores, built-in games, and controller profiles.",
        "v1.1.0 \u2014 Added quick resume, game recording, rewards, and game stats hub.",
        "v1.0.0 \u2014 Initial release with library, store, friends, and settings."
    };
    private bool _isDownloading;

    public SystemUpdateView()
    {
        InitializeComponent();
        LoadHistory();
    }

    private void LoadHistory()
    {
        foreach (var entry in _history)
        {
            HistoryList.Children.Add(new TextBlock
            {
                Text = entry,
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 6),
                Foreground = FindResource("BrushTextMuted") as Brush
            });
        }
    }

    private async void CheckUpdates(object sender, RoutedEventArgs e)
    {
        CheckUpdatesBtn.IsEnabled = false;
        UpdateStatus.Text = "Checking for updates...";

        await Task.Delay(1200);

        UpdateCard.Visibility = Visibility.Visible;
        UpdateTitle.Text = $"GoConsoleOS v{LatestVersion} available";
        UpdateDetail.Text = "A new system update is available with new features, performance improvements, and fixes.";
        UpdateProgress.Value = 0;
        UpdateProgressText.Text = "Ready to download";
        InstallBtn.Visibility = Visibility.Visible;
        ApplyBtn.Visibility = Visibility.Collapsed;
        UpdateStatus.Text = $"Update v{LatestVersion} found (current: v{CurrentVersion})";

        CheckUpdatesBtn.IsEnabled = true;
    }

    private async void InstallUpdate(object sender, RoutedEventArgs e)
    {
        if (_isDownloading) return;
        _isDownloading = true;
        InstallBtn.IsEnabled = false;
        ApplyBtn.Visibility = Visibility.Collapsed;
        UpdateProgressText.Text = "Downloading update...";

        var rng = new Random();
        for (int i = 0; i <= 100; i += 5)
        {
            UpdateProgress.Value = i;
            UpdateProgressText.Text = $"Downloading update... {i}%  ({rng.Next(2, 9)} MB/s)";
            await Task.Delay(80);
        }

        UpdateProgressText.Text = "Download complete. Update ready to apply.";
        InstallBtn.Visibility = Visibility.Collapsed;
        ApplyBtn.Visibility = Visibility.Visible;
        ApplyBtn.IsEnabled = true;
        _isDownloading = false;
    }

    private void ApplyUpdate(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            $"Apply GoConsoleOS v{LatestVersion}? The console will restart to install the update.",
            "Apply Update", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        ToastManager.Show($"GoConsoleOS v{LatestVersion} installed successfully!");
        _history.Insert(0, $"v{LatestVersion} — Latest system update installed.");

        UpdateTitle.Text = "GoConsoleOS is up to date";
        UpdateDetail.Text = $"You are running the latest version (v{LatestVersion}).";
        UpdateProgressText.Text = "";
        ApplyBtn.Visibility = Visibility.Collapsed;
        UpdateStatus.Text = $"GoConsoleOS is up to date (v{LatestVersion})";

        LoadHistory();
    }
}
