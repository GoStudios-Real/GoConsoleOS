using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GoConsoleOS.Shared;

namespace GoConsoleOS.GoConsole.Views;

public partial class DownloadManagerView : UserControl
{
    private readonly List<DownloadItem> _downloads = new();
    private readonly System.Windows.Threading.DispatcherTimer _updateTimer;

    public DownloadManagerView()
    {
        InitializeComponent();
        _updateTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _updateTimer.Tick += (_, _) => UpdateProgress();
        _updateTimer.Start();

        LoadSampleDownloads();
    }

    private void LoadSampleDownloads()
    {
        _downloads.Add(new DownloadItem
        {
            Id = "dl_1", Name = "Retro City Rampage", Status = "Downloading",
            TotalMb = 240, DownloadedMb = 87, Speed = 12.5, IsPaused = false
        });
        _downloads.Add(new DownloadItem
        {
            Id = "dl_2", Name = "Cyberpunk 2077 - Update 2.1", Status = "Paused",
            TotalMb = 1800, DownloadedMb = 1200, Speed = 0, IsPaused = true
        });
        _downloads.Add(new DownloadItem
        {
            Id = "dl_3", Name = "Stardew Valley Expansion Pack", Status = "Queued",
            TotalMb = 120, DownloadedMb = 0, Speed = 0, IsPaused = false
        });

        DownloadList.ItemsSource = _downloads;
        UpdateStatusText();
    }

    private void UpdateProgress()
    {
        var anyActive = false;
        foreach (var dl in _downloads)
        {
            if (!dl.IsPaused && dl.Status == "Downloading")
            {
                dl.DownloadedMb = Math.Min(dl.TotalMb, dl.DownloadedMb + dl.Speed * 0.5);
                dl.Speed = 8 + new Random().NextDouble() * 10;
                dl.Update();
                anyActive = true;
            }
        }
        if (anyActive) UpdateStatusText();
    }

    private void UpdateStatusText()
    {
        var active = _downloads.Count(d => d.Status == "Downloading");
        var paused = _downloads.Count(d => d.Status == "Paused");
        var queued = _downloads.Count(d => d.Status == "Queued");
        DownloadStatus.Text = $"{active} active  •  {paused} paused  •  {queued} queued";
    }

    private void TogglePause(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string id)
        {
            var dl = _downloads.FirstOrDefault(d => d.Id == id);
            if (dl == null) return;

            dl.IsPaused = !dl.IsPaused;
            dl.Status = dl.IsPaused ? "Paused" : "Downloading";
            dl.Speed = dl.IsPaused ? 0 : 8 + new Random().NextDouble() * 10;
            dl.Update();
            UpdateStatusText();
        }
    }
}

public class DownloadItem : INotifyPropertyChanged
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Status { get; set; } = "";
    public double TotalMb { get; set; }
    public double DownloadedMb { get; set; }
    public double Speed { get; set; }
    public bool IsPaused { get; set; }

    public double ProgressPercent => TotalMb > 0 ? DownloadedMb / TotalMb * 100 : 0;
    public string ProgressText => $"{DownloadedMb:F0} / {TotalMb:F0} MB  ({(Speed > 0 ? $"{Speed:F1} MB/s" : "—")})";
    public string ActionText => IsPaused ? "RESUME" : "PAUSE";
    public string StatusColor => Status switch
    {
        "Downloading" => "#00C9DB",
        "Paused" => "#FFD600",
        "Completed" => "#00E676",
        "Queued" => "#8888AA",
        _ => "#8888AA"
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Update()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProgressPercent)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProgressText)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActionText)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StatusColor)));
    }
}
