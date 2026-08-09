using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GoConsoleOS.Shared;
using GoConsoleOS.Shared.Models;

namespace GoConsoleOS.GoConsole.Views;

public partial class GameDetailsView : UserControl
{
    private readonly GameInfo _game;
    private readonly LibraryData _library;
    private readonly LibraryScanner _scanner;
    private readonly PerformanceManager _perfManager;
    private readonly GameAssetManager _assets = new();

    public GameDetailsView(GameInfo game, LibraryData library, LibraryScanner scanner, PerformanceManager perfManager)
    {
        InitializeComponent();
        _game = game;
        _library = library;
        _scanner = scanner;
        _perfManager = perfManager;

        LoadGame();
    }

    private void LoadGame()
    {
        GameTitleText.Text = _game.Title;
        GamePlatformText.Text = _game.Platform;

        try
        {
            var heroPath = _assets.GetHeroPath(_game);
            if (File.Exists(heroPath))
                HeroImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(heroPath));
            var iconPath = _assets.GetIconPath(_game);
            if (File.Exists(iconPath))
                GameIconImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(iconPath));
        }
        catch { }

        var hours = _game.PlaytimeMinutes / 60;
        PlaytimeText.Text = hours > 0 ? $"{hours}h" : "< 1h";

        LastPlayedText.Text = _game.LastPlayed?.ToString("MMM dd, yyyy") ?? "Never";
        GenreTags.Text = _game.Genres.Count > 0 ? string.Join(", ", _game.Genres.Take(3)) : "--";

        DescriptionText.Text = $"Experience {_game.Title} on {_game.Platform}.";

        LoadAchievements();
        LoadGuides();
    }

    private void LoadAchievements()
    {
        var gameKey = _game.Id.Replace(":", "_").Replace(".", "_");
        var all = new List<AchievementDisplay>
        {
            new() { Id = $"{gameKey}_first", Name = "First Launch", Description = "Launch the game for the first time", Icon = "★", Unlocked = _game.PlaytimeMinutes > 0 },
            new() { Id = $"{gameKey}_hour", Name = "One Hour", Description = "Play for over an hour", Icon = "⏱", Unlocked = _game.PlaytimeMinutes >= 60 },
            new() { Id = $"{gameKey}_five", Name = "Dedicated", Description = "Play for 5+ hours total", Icon = "🔥", Unlocked = _game.PlaytimeMinutes >= 300 },
            new() { Id = $"{gameKey}_ten", Name = "Committed", Description = "Play for 10+ hours total", Icon = "💎", Unlocked = _game.PlaytimeMinutes >= 600 },
        };

        foreach (var a in all)
            a.Opacity = a.Unlocked ? 1.0 : 0.4;

        AchievementsList.ItemsSource = all;
    }

    private void LoadGuides()
    {
        var hours = _game.PlaytimeMinutes / 60;
        var guides = new List<GuideItem>
        {
            new() { Category = "Tips & Tricks", Title = $"Essential {_game.Title} Tips", Description = "Master the basics with these pro tips." },
            new() { Category = "Getting Started", Title = $"{_game.Title} Beginner's Guide", Description = "New to the game? Start here." },
        };

        if (hours > 2)
            guides.Add(new GuideItem { Category = "Hidden Secrets", Title = $"Secrets in {_game.Title}", Description = "Discover easter eggs and hidden areas." });

        RelatedGuidesList.ItemsSource = guides;
    }

    private void Back_Click(object sender, MouseButtonEventArgs e)
    {
        var main = Window.GetWindow(this) as MainWindow;
        main?.NavigateTo("library");
    }

    private void Play_Click(object sender, MouseButtonEventArgs e)
    {
        var main = Application.Current.Windows.OfType<Window>()
            .FirstOrDefault(w => w is MainWindow) as MainWindow;

        // Open the game's platform page in the iframe browser
        if (main != null)
        {
            main.OpenGameInBrowser(_game);
            ToastManager.Show($"Loading {_game.Title} on {_game.Platform}...");
        }

        // Also try native launch in background
        GameLauncher.Launch(_game, () =>
        {
            var wasFirstLaunch = _game.PlaytimeMinutes == 0;
            _game.LastPlayed = DateTime.UtcNow;
            _game.PlaytimeMinutes += 1;
            _scanner.SaveLibrary(_library);

            if (wasFirstLaunch)
            {
                Dispatcher.Invoke(() =>
                {
                    LoadAchievements();
                    ToastManager.Show($"★ Achievement: First Launch! Welcome to {_game.Title}.");
                });
            }
        });
    }

    private class GuideItem
    {
        public string Category { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
    }

    private class AchievementDisplay
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "★";
        public bool Unlocked { get; set; }
        public double Opacity { get; set; } = 0.4;
    }
}
