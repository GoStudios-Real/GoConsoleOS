using System.Collections.Generic;
using System.Windows.Controls;

namespace GoConsoleOS.GoConsole.Views;

public partial class GameStatsView : UserControl
{
    public GameStatsView()
    {
        InitializeComponent();
        LoadStats();
    }

    private void LoadStats()
    {
        var stats = new List<GameStatItem>
        {
            new() { Icon = "🏎️", Title = "Neon Drift", Platform = "Steam", Playtime = "42h 15m", Achievements = "18/30", Completion = "60%" },
            new() { Icon = "🚀", Title = "Void Marauders", Platform = "Xbox", Playtime = "28h 30m", Achievements = "12/20", Completion = "60%" },
            new() { Icon = "💎", Title = "Crystal Realms", Platform = "Steam", Playtime = "15h 45m", Achievements = "8/25", Completion = "32%" },
            new() { Icon = "🌲", Title = "Spirit Falls", Platform = "Epic", Playtime = "8h 20m", Achievements = "5/15", Completion = "33%" },
            new() { Icon = "⚔️", Title = "Tactical Command", Platform = "GOG", Playtime = "5h 10m", Achievements = "3/12", Completion = "25%" },
            new() { Icon = "🌪️", Title = "Storm Chasers", Platform = "Steam", Playtime = "3h 00m", Achievements = "2/10", Completion = "20%" },
        };
        StatsList.ItemsSource = stats;
    }

    public class GameStatItem
    {
        public string Icon { get; set; } = "🎮";
        public string Title { get; set; } = "";
        public string Platform { get; set; } = "";
        public string Playtime { get; set; } = "0h";
        public string Achievements { get; set; } = "0/0";
        public string Completion { get; set; } = "0%";
    }
}
