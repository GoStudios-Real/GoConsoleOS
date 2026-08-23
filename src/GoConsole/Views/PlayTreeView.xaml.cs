using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GoConsoleOS.Shared;
using GoConsoleOS.Shared.Models;

namespace GoConsoleOS.GoConsole.Views;

public partial class PlayTreeView : UserControl
{
    private readonly LibraryScanner _scanner;
    private readonly LibraryData _library;

    public PlayTreeView()
    {
        InitializeComponent();
        _scanner = new LibraryScanner(ConfigReader.RootPath ?? "");
        _library = _scanner.LoadLibrary();
        
        LoadGames();
    }

    private void LoadGames()
    {
        var games2D = _library.Games.Where(g => g.GameType == "2D").ToList();
        var games3D = _library.Games.Where(g => g.GameType == "3D").ToList();
        
        Games2DList.ItemsSource = games2D;
        Games3DList.ItemsSource = games3D;
    }

    private void GameTile_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string gameId)
        {
            var game = _library.Games.FirstOrDefault(g => g.Id == gameId);
            if (game != null)
            {
                LaunchGame(game);
            }
        }
    }

    private void LaunchGame(GameInfo game)
    {
        var main = Window.GetWindow(this) as MainWindow;
        if (main != null)
        {
            if (!string.IsNullOrEmpty(game.WebUrl))
            {
                // Launch in IFRAME/browser
                main.OpenGameInBrowser(game);
                ToastManager.Show($"Loading {game.Title} in IFRAME...");
            }
            else if (!string.IsNullOrEmpty(game.ExecutablePath) && System.IO.File.Exists(game.ExecutablePath))
            {
                // Launch native executable
                GameLauncher.Launch(game, () =>
                {
                    game.LastPlayed = System.DateTime.UtcNow;
                    game.PlaytimeMinutes += 1;
                    _scanner.SaveLibrary(_library);
                });
                ToastManager.Show($"Launching {game.Title}...");
            }
            else
            {
                ToastManager.Show($"{game.Title} - Coming Soon!");
            }
        }
    }

    private void Launch2DGames_Click(object sender, MouseButtonEventArgs e)
    {
        var main = Window.GetWindow(this) as MainWindow;
        main?.NavigateTo("library");
    }

    private void Launch3DGames_Click(object sender, MouseButtonEventArgs e)
    {
        var main = Window.GetWindow(this) as MainWindow;
        main?.NavigateTo("library");
    }

    private void LaunchAllGames_Click(object sender, MouseButtonEventArgs e)
    {
        var main = Window.GetWindow(this) as MainWindow;
        main?.NavigateTo("library");
    }

    private void OpenStore_Click(object sender, MouseButtonEventArgs e)
    {
        var main = Window.GetWindow(this) as MainWindow;
        main?.NavigateTo("store");
    }
}
