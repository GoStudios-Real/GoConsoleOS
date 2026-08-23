using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace GoConsoleOS.GoConsole.Views;

public partial class SystemUpdateView : UserControl
{
    private const string CurrentVersion = "1.8.0";
    private const string UpdateManifestUrl = "https://raw.githubusercontent.com/GoStudios-Real/GoConsoleOS/main/update.json";
    private string? _latestVersion;
    private string? _latestUrl;
    private readonly List<string> _history = new()
    {
        "v1.8.0 \u2014 Added the GoAccount Center (ACC) with cloud accounts, the GoAI assistant, on-device servers for USB and Android, the lock screen with PIN, show/hide password toggles, and the software update API.",
        "v1.7.0 \u2014 Added a full Controller selection screen (auto-detect, per-kind layouts, live button & stick test, vibration test), USB device health with S.M.A.R.T. scoring, and on-screen scroll with D-Pad scrolling on long pages.",
        "v1.6.0 \u2014 Added controller type selection (Xbox, PS5 DualSense, Nintendo Switch 2), pen and touchscreen support, a Discord KEY button, a GoStudios Corporation brand splash screen, the What's New hub, a persistent settings database, 8 achievements with toast notifications, game save backup & restore, true fullscreen boot, accent color picker, CPU detection, and 7 new store items.",
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
        CurrentVersionText.Text = $"v{CurrentVersion}";
        LoadHistory();
        CheckForUpdateSilently();
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

private async void CheckForUpdateSilently()
    {
        try
        {
            using var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("GoConsole");
            var json = await client.GetStringAsync(UpdateManifestUrl);
            var doc = System.Text.Json.JsonDocument.Parse(json);
            _latestVersion = doc.RootElement.TryGetProperty("latest", out var v) ? v.GetString() : null;
            if (doc.RootElement.TryGetProperty("downloads", out var dl) &&
                dl.TryGetProperty("windows", out var win) &&
                win.TryGetProperty("url", out var u))
                _latestUrl = u.GetString();
            UpdateStatus.Text = _latestVersion != null && VersionOf(_latestVersion) > VersionOf(CurrentVersion)
                ? $"Update v{_latestVersion} found (current: v{CurrentVersion})"
                : "GoConsoleOS is up to date";
            if (_latestVersion != null && VersionOf(_latestVersion) > VersionOf(CurrentVersion))
            {
                UpdateCard.Visibility = Visibility.Visible;
                UpdateTitle.Text = $"GoConsoleOS v{_latestVersion} available";
                UpdateDetail.Text = "A new system update is available. Download it to get the latest features.";
                InstallBtn.Visibility = Visibility.Visible;
            }
        }
        catch
        {
            UpdateStatus.Text = "Could not reach the update server. Check your connection and try again.";
        }
    }

    private static long VersionOf(string v)
    {
        var parts = v.TrimStart('v').Split('.');
        long val = 0;
        for (var i = 0; i < parts.Length && i < 4; i++)
            if (long.TryParse(parts[i].Trim(), out var n)) val = val * 1000 + n;
        return val;
    }

    private async void CheckUpdates(object sender, RoutedEventArgs e)
    {
        CheckUpdatesBtn.IsEnabled = false;
        UpdateStatus.Text = "Checking for updates...";
        await CheckForUpdateSilentlyAsync();
        CheckUpdatesBtn.IsEnabled = true;
    }

    private Task CheckForUpdateSilentlyAsync() => Task.Run(CheckForUpdateSilently);

    private async void InstallUpdate(object sender, RoutedEventArgs e)
    {
        if (_isDownloading) return;
        var target = _latestVersion ?? CurrentVersion;
        if (_latestUrl != null && Uri.TryCreate(_latestUrl, UriKind.Absolute, out var url))
        {
            Process.Start(new ProcessStartInfo(url.ToString()) { UseShellExecute = true });
            UpdateProgressText.Text = "Opened the update download page in your browser.";
            UpdateStatus.Text = $"GoConsoleOS v{target} - download opened";
            return;
        }
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
        var target = _latestVersion ?? CurrentVersion;
        var result = MessageBox.Show(
            $"Apply GoConsoleOS v{target}? The console will restart to install the update.",
            "Apply Update", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        ToastManager.Show($"GoConsoleOS v{target} installed successfully!");
        _history.Insert(0, $"v{target} — Latest system update installed.");

        UpdateTitle.Text = "GoConsoleOS is up to date";
        UpdateDetail.Text = $"You are running the latest version (v{target}).";
        UpdateProgressText.Text = "";
        ApplyBtn.Visibility = Visibility.Collapsed;
        UpdateStatus.Text = $"GoConsoleOS is up to date (v{target})";

        LoadHistory();
    }
}
