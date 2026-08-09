using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace GoConsoleOS.GoConsole.Views;

public partial class GamePassView : UserControl
{
    public GamePassView()
    {
        InitializeComponent();
        LoadCatalog();
    }

    private void LoadCatalog()
    {
        var catalog = new List<GamePassItem>
        {
            new() { Title = "Neon Drift", Genre = "Action Racing", Emoji = "🏎️" },
            new() { Title = "Void Marauders", Genre = "Space Shooter", Emoji = "🚀" },
            new() { Title = "Spirit Falls", Genre = "Adventure", Emoji = "🌲" },
            new() { Title = "Crystal Realms", Genre = "RPG", Emoji = "💎" },
            new() { Title = "Tactical Command", Genre = "Strategy", Emoji = "⚔️" },
            new() { Title = "Pixel Heroes", Genre = "Indie RPG", Emoji = "🪄" },
            new() { Title = "Storm Chasers", Genre = "Action", Emoji = "🌪️" },
            new() { Title = "Deep Recon", Genre = "Adventure", Emoji = "🌊" },
            new() { Title = "Aether Knights", Genre = "RPG", Emoji = "🗡️" },
            new() { Title = "Circuit Breakers", Genre = "Puzzle", Emoji = "⚡" },
            new() { Title = "Feral Frontier", Genre = "Open World", Emoji = "🐺" },
            new() { Title = "Starforge", Genre = "Strategy", Emoji = "⭐" },
        };

        CatalogList.ItemsSource = catalog;
        PassStatus.Text = $"{catalog.Count} titles available in Game Pass";
    }

    private void InstallGamePass(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string title)
        {
            ToastManager.Show($"Installing {title} from Game Pass...");
        }
    }

    public class GamePassItem
    {
        public string Title { get; set; } = "";
        public string Genre { get; set; } = "";
        public string Emoji { get; set; } = "🎮";
    }
}
