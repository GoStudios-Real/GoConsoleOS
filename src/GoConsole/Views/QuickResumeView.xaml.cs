using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GoConsoleOS.GoConsole.Views;

public partial class QuickResumeView : UserControl
{
    public QuickResumeView()
    {
        InitializeComponent();
        LoadSuspendedGames();
    }

    private void LoadSuspendedGames()
    {
        var games = new List<SuspendItem>
        {
            new() { Title = "Neon Drift", Platform = "Steam", Icon = "🏎️", Status = "SUSPENDED", SavedAt = "2m ago" },
            new() { Title = "Void Marauders", Platform = "Xbox", Icon = "🚀", Status = "SUSPENDED", SavedAt = "15m ago" },
            new() { Title = "Crystal Realms", Platform = "Steam", Icon = "💎", Status = "SUSPENDED", SavedAt = "1h ago" },
            new() { Title = "Spirit Falls", Platform = "Epic", Icon = "🌲", Status = "SUSPENDED", SavedAt = "2h ago" },
        };
        ResumeList.ItemsSource = games;
    }

    private void ResumeGame(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string title)
            ToastManager.Show($"Resuming {title}...");
    }

    private void ResumeGameBtn(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string title)
            ToastManager.Show($"Resuming {title} from Quick Resume...");
    }

    public class SuspendItem
    {
        public string Title { get; set; } = "";
        public string Platform { get; set; } = "";
        public string Icon { get; set; } = "🎮";
        public string Status { get; set; } = "SUSPENDED";
        public string SavedAt { get; set; } = "";
    }
}
