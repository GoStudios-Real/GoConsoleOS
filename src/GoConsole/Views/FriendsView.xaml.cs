using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GoConsoleOS.Shared;
using GoConsoleOS.Shared.Models;

namespace GoConsoleOS.GoConsole.Views;

public partial class FriendsView : UserControl
{
    private readonly ProfileManager _profileManager;
    private readonly LibraryData? _library;

    public FriendsView()
    {
        InitializeComponent();
        _profileManager = new ProfileManager(ConfigReader.RootPath ?? "");
        _library = null;
        LoadProfiles();
    }

    public FriendsView(ProfileManager profileManager, LibraryData? library)
    {
        InitializeComponent();
        _profileManager = profileManager;
        _library = library;
        LoadProfiles();
    }

    private void LoadProfiles()
    {
        var names = _profileManager.GetProfileNames();
        var profiles = new List<UserProfile>();

        foreach (var name in names)
        {
            var p = _profileManager.LoadProfile(name);
            if (p != null) profiles.Add(p);
        }

        if (_profileManager.CurrentProfile != null && !profiles.Any(p => p.Username == _profileManager.CurrentProfile.Username))
            profiles.Insert(0, _profileManager.CurrentProfile);

        ProfileListBox.ItemsSource = profiles;
        UpdateCurrentProfileDisplay(_profileManager.CurrentProfile);

        var recentActivity = new List<ActivityItem>();
        if (_library != null)
        {
            foreach (var game in _library.Games.Where(g => g.LastPlayed.HasValue)
                         .OrderByDescending(g => g.LastPlayed).Take(5))
            {
                recentActivity.Add(new ActivityItem
                {
                    Title = game.Title,
                    Description = $"Last played: {game.LastPlayed:g}"
                });
            }
        }

        if (recentActivity.Count == 0)
        {
            recentActivity.Add(new ActivityItem
            {
                Title = "No recent activity",
                Description = "Launch a game to start tracking playtime"
            });
        }

        ActivityList.ItemsSource = recentActivity;
    }

    private void UpdateCurrentProfileDisplay(UserProfile? profile)
    {
        if (profile == null) return;

        CurrentProfileName.Text = profile.DisplayName;
        CurrentProfileDetails.Text = profile.IsGuest
            ? "Guest profile (no permanent storage)"
            : $"Local profile \u2022 Created {profile.CreatedAt:MMM dd, yyyy}";

        StatGames.Text = _library?.Games.Count.ToString() ?? "0";
        StatPlaytime.Text = $"{profile.TotalPlaytimeMinutes / 60}h";
        StatAchievements.Text = profile.Achievements.Count(a => a.Value.IsUnlocked).ToString();
    }

    private void Profile_Selected(object sender, SelectionChangedEventArgs e)
    {
        if (ProfileListBox.SelectedItem is UserProfile selected)
        {
            _profileManager.LoadProfile(selected.Username);
            UpdateCurrentProfileDisplay(selected);
            ProfileListBox.SelectedItem = null;

            Logger.Info($"Switched to profile: {selected.DisplayName}");

            var mainWindow = Window.GetWindow(this);
            if (mainWindow != null)
            {
                MessageBox.Show($"Switched to profile: {selected.DisplayName}\n\n" +
                                "Some settings may apply after restarting GoConsoleOS.",
                                "Profile Switched",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
            }
        }
    }

    private void AddFriend_Click(object sender, MouseButtonEventArgs e)
    {
        var keyboard = new OnScreenKeyboard();
        keyboard.Owner = Window.GetWindow(this);

        if (keyboard.ShowDialog() == true && !string.IsNullOrEmpty(keyboard.InputText))
        {
            MessageBox.Show(
                $"Friend request would be sent to \"{keyboard.InputText}\".\n\n" +
                "Social features are local-only in this build. In a future update, " +
                "this will connect to GoConsoleOS network services.",
                "Add Friend",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    private void CreateProfile_Click(object sender, MouseButtonEventArgs e)
    {
        var keyboard = new OnScreenKeyboard();
        keyboard.Owner = Window.GetWindow(this);

        if (keyboard.ShowDialog() == true && !string.IsNullOrEmpty(keyboard.InputText))
        {
            var name = keyboard.InputText.Trim();
            if (_profileManager.GetProfileNames().Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                MessageBox.Show($"Profile \"{name}\" already exists.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var profile = _profileManager.CreateProfile(name, name);
            UpdateCurrentProfileDisplay(profile);
            LoadProfiles();

            MessageBox.Show($"Profile \"{name}\" created!\n\n" +
                            "You can now switch to this profile from the list.",
                            "Profile Created", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private class ActivityItem
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
    }
}
