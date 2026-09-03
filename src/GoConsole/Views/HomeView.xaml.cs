using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GoConsoleOS.Shared;
using GoConsoleOS.Shared.Models;

namespace GoConsoleOS.GoConsole.Views;

public partial class HomeView : UserControl
{
    private readonly LibraryData _library;
    private readonly UserProfile? _profile;
    private readonly PerformanceManager _perf;
    private readonly InitConfig _config;
    private readonly Action<GameInfo>? _onGameSelected;
    private readonly GameAssetManager _assets = new();

    public HomeView(LibraryData library, UserProfile? profile, PerformanceManager perf, InitConfig config, Action<GameInfo>? onGameSelected = null)
    {
        InitializeComponent();
        _library = library;
        _profile = profile;
        _perf = perf;
        _config = config;
        _onGameSelected = onGameSelected;

        var recent = library.Games
            .Where(g => g.LastPlayed.HasValue && g.IsInstalled)
            .OrderByDescending(g => g.LastPlayed)
            .Take(10)
            .Select(g => new GameDisplayItem(g, _assets))
            .ToList();

        ContinuePlayingList.ItemsSource = recent;

        var recommended = library.Games
            .Where(g => g.IsInstalled)
            .OrderByDescending(g => g.PlaytimeMinutes)
            .Take(10)
            .Select(g => new GameDisplayItem(g, _assets))
            .ToList();

        RecommendedList.ItemsSource = recommended;

        GameCountText.Text = $"{library.Games.Count} games";

        if (profile != null)
            HeroGreeting.Text = $"Welcome back, {profile.DisplayName}. Ready to play?";

        // Built-in browser games
        BuiltInGamesList.ItemsSource = new[]
        {
            new BuiltInGame { Id = "snake", Title = "Snake", Description = "Classic snake", Emoji = "🐍" },
            new BuiltInGame { Id = "pong", Title = "Pong", Description = "VS AI pong", Emoji = "🏓" },
            new BuiltInGame { Id = "breakout", Title = "Breakout", Description = "Break bricks", Emoji = "🧱" },
            new BuiltInGame { Id = "tetris", Title = "Tetris", Description = "Stack blocks", Emoji = "🔷" },
            new BuiltInGame { Id = "dino", Title = "Dino Runner", Description = "Jump cacti", Emoji = "🦖" },
            new BuiltInGame { Id = "flappy", Title = "Flappy Bird", Description = "Flap & fly", Emoji = "🐦" },
            new BuiltInGame { Id = "invaders", Title = "Space Invaders", Description = "Alien defense", Emoji = "👾" },
            new BuiltInGame { Id = "2048", Title = "2048", Description = "Merge tiles", Emoji = "🔢" },
            new BuiltInGame { Id = "memory", Title = "Memory Match", Description = "Match pairs", Emoji = "🃏" },
            new BuiltInGame { Id = "minesweeper", Title = "Minesweeper", Description = "Clear mines", Emoji = "💣" },
        };

        // Load hero banner
        var heroGame = library.Games.Where(g => g.IsInstalled).OrderByDescending(g => g.PlaytimeMinutes).FirstOrDefault();
        if (heroGame != null)
        {
            try
            {
                var heroPath = _assets.GetHeroPath(heroGame);
                if (System.IO.File.Exists(heroPath))
                    HeroImage.Source = new System.Windows.Media.Imaging.BitmapImage(new System.Uri(heroPath));
                HeroTitle.Text = heroGame.Title;
                HeroGreeting.Text = $"Welcome back, {profile?.DisplayName ?? "Guest"}. Continue playing {heroGame.Title}?";
                var iconPath = _assets.GetIconPath(heroGame);
                if (System.IO.File.Exists(iconPath))
                    HeroGameIcon.Source = new System.Windows.Media.Imaging.BitmapImage(new System.Uri(iconPath));
            }
            catch { }
        }

        // Show GoStudios games status
        var games2D = library.Games.Count(g => g.GameType == "2D");
        var games3D = library.Games.Count(g => g.GameType == "3D");
        var totalGames = library.Games.Count;

        var tb2D = new TextBlock
        {
            Text = $"🎮 {games2D} 2D Games",
            FontSize = 13,
            Margin = new Thickness(0, 0, 16, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = FindResource("BrushSuccess") as System.Windows.Media.Brush
        };
        PlatformListPanel.Children.Add(tb2D);

        var tb3D = new TextBlock
        {
            Text = $"🕹️ {games3D} 3D Games",
            FontSize = 13,
            Margin = new Thickness(0, 0, 16, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = FindResource("BrushSuccess") as System.Windows.Media.Brush
        };
        PlatformListPanel.Children.Add(tb3D);

        if (recent.Count > 0)
        {
            QuickResumeBtn.Content = $"RESUME: {recent[0].Title.Split(' ').FirstOrDefault() ?? "GAME"}";
        }
        else
            QuickResumeBtn.Visibility = Visibility.Collapsed;

        ActivityFeedList.ItemsSource = ActivityFeed.Entries;

        AppsList.ItemsSource = new[]
        {
            new AppTileItem { Name = "PLAYTREE.EXE", Icon = "🌳", Route = "playtree" },
            new AppTileItem { Name = "GOSTUDIOS CORPORATION STORE", Icon = "🛒", Route = "store" },
            new AppTileItem { Name = "WHAT'S NEW", Icon = "✨", Route = "whatsnew" },
            new AppTileItem { Name = "CONTROLLER", Icon = "🎮", Route = "controller" },
            new AppTileItem { Name = "USB HEALTH", Icon = "🛡️", Route = "usbhealth" },
            new AppTileItem { Name = "DISCORD", Icon = "💬", Route = "discord" },
            new AppTileItem { Name = "BROWSER", Icon = "🌐", Route = "browser" },
            new AppTileItem { Name = "TOKEN CREATOR", Icon = "🎫", Route = "tokencreator" }
        };
    }

    private void AppTile_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is AppTileItem tile)
        {
            var main = Window.GetWindow(this) as MainWindow;
            main?.NavigateTo(tile.Route);
        }
    }

    private class AppTileItem
    {
        public string Name { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Route { get; set; } = "";
    }

    public class BuiltInGame
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Emoji { get; set; } = "🎮";
    }

    private void BuiltInGame_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string gameId)
        {
            var main = Window.GetWindow(this) as MainWindow;
            main?.NavigateTo("games");
        }
    }

    private void DashboardCard_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string route)
        {
            var main = Window.GetWindow(this) as MainWindow;
            main?.NavigateTo(route);
        }
    }

    private void GameTile_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string gameId)
        {
            var game = _library.Games.FirstOrDefault(g => g.Id == gameId);
            if (game != null)
                _onGameSelected?.Invoke(game);
        }
    }

    private void GameTile_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string gameId)
        {
            var game = _library.Games.FirstOrDefault(g => g.Id == gameId);
            if (game != null)
                _onGameSelected?.Invoke(game);
        }
    }

    private void QuickResume_Click(object sender, RoutedEventArgs e)
    {
        var recent = _library.Games
            .Where(g => g.LastPlayed.HasValue && g.IsInstalled)
            .OrderByDescending(g => g.LastPlayed)
            .FirstOrDefault();

        if (recent != null)
            _onGameSelected?.Invoke(recent);
    }

    private void ViewAll_Click(object sender, RoutedEventArgs e)
    {
        var main = Window.GetWindow(this) as MainWindow;
        main?.NavigateTo("library");
    }

    private class GameDisplayItem
    {
        public string Id { get; }
        public string Title { get; }
        public string Platform { get; }
        public string BannerPath { get; }

        public GameDisplayItem(GameInfo game, GameAssetManager assets)
        {
            Id = game.Id;
            Title = game.Title;
            Platform = game.Platform;
            try { BannerPath = assets.GetBannerPath(game); }
            catch { BannerPath = ""; }
        }
    }
}
