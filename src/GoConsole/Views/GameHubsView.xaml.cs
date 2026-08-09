using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GoConsoleOS.Shared;
using GoConsoleOS.Shared.Models;

namespace GoConsoleOS.GoConsole.Views;

public partial class GameHubsView : UserControl
{
    private readonly LibraryData _library = new();
    private readonly Random _rng = new();
    private List<HubGameItem> _games = new();

    public GameHubsView()
    {
        InitializeComponent();
        LoadLibrary();
        LoadGames();
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

    private void LoadGames()
    {
        _games = _library.Games
            .Where(g => g.IsInstalled)
            .OrderBy(g => g.Title)
            .Select(g => new HubGameItem { Title = g.Title, Platform = g.Platform })
            .ToList();

        if (_games.Count == 0)
            _games.Add(new HubGameItem { Title = "No games found", Platform = "Scan your library first" });

        GameList.ItemsSource = _games;
    }

    private void SelectGame(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.DataContext is HubGameItem item)
        {
            HubTitle.Text = item.Title;
            HubSubtitle.Text = $"{item.Platform} \u2022 Hub last updated {DateTime.Now:MMM dd, yyyy}";

            NewsList.ItemsSource = BuildNews(item.Title);
            DlcList.ItemsSource = BuildDlc(item.Title);
            TrophyList.ItemsSource = BuildTrophies(item.Title);
        }
    }

    private List<HubEntry> BuildNews(string title)
    {
        var news = new List<HubEntry>
        {
            new() { Title = $"Version {1 + _rng.Next(9)}.{_rng.Next(5)} update released", Detail = $"New content and fixes for {title}" },
            new() { Title = "Season event now live", Detail = "Limited-time challenges and rewards are active" },
            new() { Title = "Server maintenance scheduled", Detail = "Maintenance on " + DateTime.Now.AddDays(2).ToString("ddd, MMM dd") },
        };
        return news;
    }

    private List<HubEntry> BuildDlc(string title)
    {
        return new List<HubEntry>
        {
            new() { Title = $"{title}: Deluxe Pack", Detail = "Bonus outfits, weapons, and currency", Price = $"\u20AC{9 + _rng.Next(20):N2}" },
            new() { Title = $"{title}: Expansion Pass", Detail = "All upcoming expansion content", Price = $"\u20AC{19 + _rng.Next(30):N2}" },
            new() { Title = $"{title}: Season Pass", Detail = "Full season of content and events", Price = $"\u20AC{29 + _rng.Next(25):N2}" },
        };
    }

    private List<HubTrophy> BuildTrophies(string title)
    {
        var gameKey = title.Replace(" ", "_");
        var hours = _library.Games.FirstOrDefault(g => g.Title == title)?.PlaytimeMinutes ?? 0;
        return new List<HubTrophy>
        {
            new() { Icon = "★", Title = "Welcome Aboard", Detail = "Launch the game for the first time", Unlocked = hours > 0 },
            new() { Icon = "🔥", Title = "Warming Up", Detail = "Play for 30 minutes total", Unlocked = hours >= 30 },
            new() { Icon = "⏱", Title = "One Hour", Detail = "Play for over an hour", Unlocked = hours >= 60 },
            new() { Icon = "💎", Title = "Dedicated", Detail = "Play for 5+ hours total", Unlocked = hours >= 300 },
        };
    }

    public class HubGameItem
    {
        public string Title { get; set; } = "";
        public string Platform { get; set; } = "";
    }

    public class HubEntry
    {
        public string Title { get; set; } = "";
        public string Detail { get; set; } = "";
        public string Price { get; set; } = "";
    }

    public class HubTrophy
    {
        public string Icon { get; set; } = "★";
        public string Title { get; set; } = "";
        public string Detail { get; set; } = "";
        public bool Unlocked { get; set; }
        public double Opacity => Unlocked ? 1.0 : 0.4;
    }
}
