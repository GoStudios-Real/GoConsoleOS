using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;

namespace GoDesk.Views;

public partial class GamesView : UserControl
{
    private static readonly string GameDir = Path.Combine(
        Directory.GetParent(Directory.GetCurrentDirectory())?.FullName ?? Directory.GetCurrentDirectory(),
        "games");

    public GamesView()
    {
        InitializeComponent();
    }

    private void LaunchGame(string file)
    {
        var path = Path.Combine(GameDir, file);
        if (File.Exists(path))
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    private void PlayPong(object s, MouseButtonEventArgs e) => LaunchGame("pong.html");
    private void PlaySnake(object s, MouseButtonEventArgs e) => LaunchGame("snake.html");
    private void PlayTetris(object s, MouseButtonEventArgs e) => LaunchGame("tetris.html");
    private void PlayBreakout(object s, MouseButtonEventArgs e) => LaunchGame("breakout.html");
    private void PlayMemory(object s, MouseButtonEventArgs e) => LaunchGame("memory.html");
    private void PlayFlappyBird(object s, MouseButtonEventArgs e) => LaunchGame("flappybird.html");
    private void PlayMinesweeper(object s, MouseButtonEventArgs e) => LaunchGame("minesweeper.html");
    private void PlaySudoku(object s, MouseButtonEventArgs e) => LaunchGame("sudoku.html");
    private void Play2048(object s, MouseButtonEventArgs e) => LaunchGame("2048.html");
    private void PlayWhackAMole(object s, MouseButtonEventArgs e) => LaunchGame("whackamole.html");
}
