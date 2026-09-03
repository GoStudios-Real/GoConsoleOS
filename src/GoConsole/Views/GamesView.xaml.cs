using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GoConsoleOS.Shared;

namespace GoConsoleOS.GoConsole.Views;

public partial class GamesView : UserControl
{
    private readonly string _gamesDir;

    public GamesView()
    {
        InitializeComponent();
        _gamesDir = ConfigReader.ResolvePath("system\\games");
        Directory.CreateDirectory(_gamesDir);
        EnsureGamesExist();
        LoadGameList();
        InitializeWebView();
        // Auto-load first game so it's instantly playable
        Dispatcher.BeginInvoke(new Action(() => LoadGame("snake")), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private async void InitializeWebView()
    {
        try
        {
            await GameWebView.EnsureCoreWebView2Async(null);
        }
        catch
        {
            WelcomeOverlay.Visibility = Visibility.Visible;
        }
    }

    private void EnsureGamesExist()
    {
        var games = new Dictionary<string, string>
        {
            ["snake"] = GameFiles.Snake,
            ["pong"] = GameFiles.Pong,
            ["breakout"] = GameFiles.Breakout,
            ["tetris"] = GameFiles.Tetris,
            ["dino"] = GameFiles.DinoRunner,
            ["flappy"] = GameFiles.FlappyBird,
            ["invaders"] = GameFiles.SpaceInvaders,
            ["2048"] = GameFiles.Game2048,
            ["memory"] = GameFiles.MemoryMatch,
            ["minesweeper"] = GameFiles.Minesweeper,
        };

        foreach (var (id, html) in games)
        {
            var path = Path.Combine(_gamesDir, $"{id}.html");
            if (!File.Exists(path))
                File.WriteAllText(path, html);
        }
    }

    private void LoadGameList()
    {
        var games = new List<GameEntry>
        {
            new() { Id = "snake", Title = "Snake", Description = "Classic snake game", Emoji = "🐍" },
            new() { Id = "pong", Title = "Pong", Description = "VS AI pong", Emoji = "🏓" },
            new() { Id = "breakout", Title = "Breakout", Description = "Break all the bricks", Emoji = "🧱" },
            new() { Id = "tetris", Title = "Tetris", Description = "Stack the blocks", Emoji = "🔷" },
            new() { Id = "dino", Title = "Dino Runner", Description = "Jump over cacti", Emoji = "🦖" },
            new() { Id = "flappy", Title = "Flappy Bird", Description = "Flap through pipes", Emoji = "🐦" },
            new() { Id = "invaders", Title = "Space Invaders", Description = "Defend the earth", Emoji = "👾" },
            new() { Id = "2048", Title = "2048", Description = "Slide and merge tiles", Emoji = "🔢" },
            new() { Id = "memory", Title = "Memory Match", Description = "Match pairs of cards", Emoji = "🃏" },
            new() { Id = "minesweeper", Title = "Minesweeper", Description = "Clear the minefield", Emoji = "💣" },
        };
        GameList.ItemsSource = games;
    }

    private void SelectGame(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string id)
            LoadGame(id);
    }

    private void LoadGame(string id)
    {
        var path = Path.Combine(_gamesDir, $"{id}.html");
        if (File.Exists(path))
        {
            WelcomeOverlay.Visibility = Visibility.Collapsed;
            GameWebView.Source = new Uri($"file:///{path.Replace('\\', '/')}");
        }
    }

    public class GameEntry
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Emoji { get; set; } = "🎮";
    }
}
