using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GoConsoleOS.Shared;
using GoConsoleOS.Shared.Models;

namespace GoConsoleOS.GoConsole.Views;

public partial class LibraryView : UserControl
{
    private LibraryData _library;
    private LibraryScanner _scanner;
    private string _currentFilter = "all";
    private Action<GameInfo>? _onGameSelected;

    public LibraryView(LibraryData library, LibraryScanner scanner, Action<GameInfo>? onGameSelected = null)
    {
        InitializeComponent();
        _library = library;
        _scanner = scanner;
        _onGameSelected = onGameSelected;
        ApplyFilter("all");
    }

    private void OpenKeyboard(object sender, MouseButtonEventArgs e)
    {
        SearchBox.Focus();
        (Window.GetWindow(this) as MainWindow)?.OpenOnScreenKeyboard();
    }

    private void ApplyFilter(string filter)
    {
        _currentFilter = filter.ToLowerInvariant();
        LibraryTitle.Text = filter.ToUpper() + " GAMES";

        var games = _library.Games.AsEnumerable();

        games = _currentFilter switch
        {
            "all" => games,
            "installed" => games.Where(g => g.IsInstalled),
            "favorites" => games.Where(g => g.IsFavorite),
            "2d" => games.Where(g => g.GameType == "2D"),
            "3d" => games.Where(g => g.GameType == "3D"),
            _ => games.Where(g =>
                g.Platform.Equals(_currentFilter, StringComparison.OrdinalIgnoreCase))
        };

        var search = SearchBox?.Text?.Trim().ToLowerInvariant() ?? "";
        if (!string.IsNullOrEmpty(search))
            games = games.Where(g => g.Title.ToLowerInvariant().Contains(search));

        var list = games.OrderBy(g => g.Title).ToList();
        GameGrid.ItemsSource = list;
        GameCountDetail.Text = $"{list.Count} game{(list.Count == 1 ? "" : "s")} found";
    }

    private void Search_Changed(object sender, TextChangedEventArgs e)
    {
        ApplyFilter(_currentFilter);
    }

    private void Filter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag)
            ApplyFilter(tag);
    }

    private void GameTile_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string gameId)
        {
            var game = _library.Games.FirstOrDefault(g => g.Id == gameId);
            if (game != null)
                _onGameSelected?.Invoke(game);
        }
        e.Handled = true;
    }

    private void PlayButton_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string gameId)
            LaunchGame(gameId);
        e.Handled = true;
    }

    private void LaunchGame(string gameId)
    {
        var game = _library.Games.FirstOrDefault(g => g.Id == gameId);
        if (game == null) return;

        var main = Application.Current.Windows.OfType<Window>()
            .FirstOrDefault(w => w is MainWindow) as MainWindow;

        // Open the game's platform page in the iframe browser
        if (main != null)
        {
            main.OpenGameInBrowser(game);
            ToastManager.Show($"Loading {game.Title} on {game.Platform}...");
        }

        // Also try native launch in background
        GameLauncher.Launch(game, () =>
        {
            game.LastPlayed = DateTime.UtcNow;
            game.PlaytimeMinutes += 1;
            _scanner.SaveLibrary(_library);
        });
    }

    private static void MinimizeHostWindow()
    {
        try
        {
            var window = Application.Current.Windows.OfType<Window>()
                .FirstOrDefault(w => w.IsVisible && w.GetType().Name == "MainWindow");
            if (window != null)
                window.WindowState = WindowState.Minimized;
        }
        catch { }
    }
}
