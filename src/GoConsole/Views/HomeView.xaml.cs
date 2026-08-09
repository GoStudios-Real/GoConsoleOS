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

        var platforms = PlatformDetection.GetInstalledPlatforms();
        foreach (var (name, installed) in platforms)
        {
            var tb = new TextBlock
            {
                Text = installed ? $"\u25CF {name}" : $"\u25CB {name}",
                FontSize = 13,
                Margin = new Thickness(0, 0, 16, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            tb.Foreground = installed
                ? FindResource("BrushSuccess") as System.Windows.Media.Brush
                : FindResource("BrushTextMuted") as System.Windows.Media.Brush;
            PlatformListPanel.Children.Add(tb);
        }

        if (recent.Count > 0)
        {
            QuickResumeText.Text = $"RESUME: {recent[0].Title.Split(' ').FirstOrDefault() ?? "GAME"}";
        }
        else
            QuickResumeBtn.Visibility = Visibility.Collapsed;

        ActivityFeedList.ItemsSource = ActivityFeed.Entries;

        AppsList.ItemsSource = new[]
        {
            new AppTileItem { Name = "WHAT'S NEW", Icon = "✨", Route = "whatsnew" },
            new AppTileItem { Name = "GOSTUDIOS STORE", Icon = "🛒", Route = "store" },
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
