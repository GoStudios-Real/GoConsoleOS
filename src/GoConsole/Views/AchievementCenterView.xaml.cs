using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using GoConsoleOS.Shared;

namespace GoConsoleOS.GoConsole.Views;

public partial class AchievementCenterView : UserControl
{
    public AchievementCenterView()
    {
        InitializeComponent();
        AchievementManager.Unlock("achievement_hunter");
        LoadAchievements();
    }

    private void LoadAchievements()
    {
        var all = AchievementManager.Definitions
            .Select(d => new AchievementItem
            {
                Name = d.Name,
                Description = d.Description,
                Reward = d.Reward,
                IsUnlocked = AchievementManager.IsUnlocked(d.Id)
            })
            .ToList();

        AchievementList.ItemsSource = all;
        SummaryText.Text = $"{AchievementManager.UnlockedCount} / {AchievementManager.Definitions.Count} achievements earned";
    }

    public class AchievementItem
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Reward { get; set; } = "";
        public bool IsUnlocked { get; set; }
    }
}
