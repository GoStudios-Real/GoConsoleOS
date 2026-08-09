using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GoConsoleOS.Shared;
using GoConsoleOS.Shared.Models;

namespace GoConsoleOS.GoConsole.Views;

public partial class CloudSaveView : UserControl
{
    private readonly LibraryData _library = new();
    private readonly List<CloudSaveItem> _items = new();
    private readonly Random _rng = new();

    public CloudSaveView()
    {
        InitializeComponent();
        LoadLibrary();
        LoadSaves();
        UpdateStorage();
    }

    private void LoadLibrary()
    {
        try
        {
            var path = Path.Combine(ConfigReader.RootPath ?? "", "launcher", "library", "library.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                _library.Games = System.Text.Json.JsonSerializer.Deserialize<List<GameInfo>>(json) ?? new();
            }
        }
        catch { }
    }

    private void LoadSaves()
    {
        var games = _library.Games.Where(g => g.IsInstalled).OrderByDescending(g => g.PlaytimeMinutes).ToList();
        foreach (var game in games)
        {
            var hours = game.PlaytimeMinutes / 60;
            var sizeMb = 5 + _rng.Next(120);
            _items.Add(new CloudSaveItem
            {
                Id = game.Id,
                Title = game.Title,
                Detail = $"{sizeMb} MB  \u2022  synced {hours}h ago",
                Status = "Synced",
                StatusBrush = FindResource("BrushSuccess") as Brush,
                SizeMb = sizeMb
            });
        }

        if (_items.Count == 0)
        {
            _items.Add(new CloudSaveItem
            {
                Id = "",
                Title = "No games found",
                Detail = "Cloud saves appear here once you have games installed",
                Status = "Idle",
                StatusBrush = FindResource("BrushTextMuted") as Brush,
                SizeMb = 0
            });
        }

        SaveList.ItemsSource = _items;
    }

    private void UpdateStorage()
    {
        var totalMb = _items.Sum(i => i.SizeMb);
        StorageUsedText.Text = $"{totalMb} MB";
        StorageBar.Value = totalMb;
    }

    private async void SyncAll(object sender, RoutedEventArgs e)
    {
        SyncAllBtn.IsEnabled = false;
        SyncProgress.Visibility = Visibility.Visible;
        CloudStatus.Text = "Syncing all saves to the cloud...";

        foreach (var item in _items.Where(i => i.Status != "Synced"))
        {
            item.Status = "Syncing";
            item.StatusBrush = FindResource("BrushWarning") as Brush;
        }
        SaveList.ItemsSource = null;
        SaveList.ItemsSource = _items;

        for (int i = 0; i <= 100; i += 10)
        {
            SyncProgress.Value = i;
            await Task.Delay(80);
        }

        foreach (var item in _items)
        {
            item.Status = "Synced";
            item.StatusBrush = FindResource("BrushSuccess") as Brush;
        }
        SaveList.ItemsSource = null;
        SaveList.ItemsSource = _items;

        LastSyncText.Text = DateTime.Now.ToString("hh:mm tt");
        CloudStatus.Text = "All saves synced to the cloud";
        SyncProgress.Visibility = Visibility.Collapsed;
        SyncAllBtn.IsEnabled = true;
        ToastManager.Show("Cloud saves synced");
    }

    private async void SyncGame(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string id)
        {
            var item = _items.FirstOrDefault(i => i.Id == id);
            if (item == null) return;

            btn.IsEnabled = false;
            item.Status = "Syncing";
            item.StatusBrush = FindResource("BrushWarning") as Brush;
            SaveList.ItemsSource = null;
            SaveList.ItemsSource = _items;

            await Task.Delay(600);

            item.Status = "Synced";
            item.StatusBrush = FindResource("BrushSuccess") as Brush;
            btn.IsEnabled = true;
            SaveList.ItemsSource = null;
            SaveList.ItemsSource = _items;

            LastSyncText.Text = DateTime.Now.ToString("hh:mm tt");
            ToastManager.Show($"{item.Title} synced");
        }
    }

    public class CloudSaveItem
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Detail { get; set; } = "";
        public string Status { get; set; } = "Idle";
        public Brush? StatusBrush { get; set; }
        public int SizeMb { get; set; }
    }
}
