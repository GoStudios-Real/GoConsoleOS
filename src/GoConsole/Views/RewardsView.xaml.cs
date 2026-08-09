using System.Collections.Generic;
using System.Windows.Controls;

namespace GoConsoleOS.GoConsole.Views;

public partial class RewardsView : UserControl
{
    public RewardsView()
    {
        InitializeComponent();
        LoadRewards();
    }

    private void LoadRewards()
    {
        var rewards = new List<RewardItem>
        {
            new() { Icon = "🎮", Title = "Game Launch", Description = "Launch any game", Points = "+10" },
            new() { Icon = "📷", Title = "Screenshot", Description = "Take a screenshot (F12)", Points = "+5" },
            new() { Icon = "🏆", Title = "Achievement Unlocked", Description = "Earn any achievement", Points = "+25" },
            new() { Icon = "👥", Title = "Friend Added", Description = "Add a new friend", Points = "+15" },
            new() { Icon = "⭐", Title = "Wishlist Item", Description = "Add a game to wishlist", Points = "+3" },
            new() { Icon = "🎬", Title = "Game Clip", Description = "Record a gameplay clip", Points = "+20" },
            new() { Icon = "🌐", Title = "Browse Store", Description = "Visit the store", Points = "+2" },
            new() { Icon = "🔁", Title = "Daily Login", Description = "Open GoConsoleOS daily", Points = "+50" },
        };
        RewardList.ItemsSource = rewards;
    }

    public class RewardItem
    {
        public string Icon { get; set; } = "★";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Points { get; set; } = "+0";
    }
}
