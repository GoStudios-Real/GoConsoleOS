using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GoConsoleOS.Shared;
using GoConsoleOS.Shared.Models;

namespace GoConsoleOS.GoConsole.Views;

public partial class ControllerProfilesView : UserControl
{
    private readonly LibraryData _library = new();
    private readonly Dictionary<string, string> _activeProfiles = new();
    private readonly List<ProfileTemplate> _templates = new();
    private List<GameProfileItem> _games = new();

    public ControllerProfilesView()
    {
        InitializeComponent();
        _templates.AddRange(new[]
        {
            new ProfileTemplate { Name = "Gamepad (Default)", Description = "Standard dual-stick gamepad layout" },
            new ProfileTemplate { Name = "Gamepad + Gyro", Description = "Motion-assisted aiming for shooters" },
            new ProfileTemplate { Name = "Keyboard & Mouse", Description = "Simulates keyboard and mouse input" },
            new ProfileTemplate { Name = "Trackpad Only", Description = "Touch and click trackpad scheme" },
            new ProfileTemplate { Name = "Fighting Pad", Description = "Six-button layout for fighting games" },
            new ProfileTemplate { Name = "Arcade Stick", Description = "Simulated arcade stick controls" },
        });
        LoadLibrary();
        LoadGames();
    }

    private void LoadLibrary()
    {
        try
        {
            var path = Path.Combine(ConfigReader.RootPath ?? "", "launcher", "library", "library.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                _library.Games = System.Text.Json.JsonSerializer.Deserialize<List<GameInfo>>(json) ?? new();
            }
        }
        catch { }
    }

    private void LoadGames()
    {
        var games = _library.Games
            .Where(g => g.IsInstalled)
            .OrderBy(g => g.Title)
            .Select(g => new GameProfileItem
            {
                Title = g.Title,
                Profile = "Gamepad (Default)",
                IsSelected = false
            })
            .ToList();

        if (games.Count == 0)
        {
            games.Add(new GameProfileItem { Title = "No games found", Profile = "Scan your library first", IsSelected = false });
        }

        _games = games;
        GameList.ItemsSource = _games;
    }

    private void SelectGame(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.DataContext is GameProfileItem item)
        {
            SelectedGameText.Text = item.Title;
            SelectedGameDesc.Text = "Choose a controller configuration for this game:";

            foreach (var game in _games)
                game.IsSelected = game == item;

            GameList.ItemsSource = null;
            GameList.ItemsSource = _games;

            ProfileList.ItemsSource = _templates.Select(t => new ProfileTemplate
            {
                Name = t.Name,
                Description = t.Description,
                ActiveText = _activeProfiles.TryGetValue(item.Title, out var active) && active == t.Name ? "ACTIVE" : ""
            }).ToList();
        }
    }

    private void ApplyProfile(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.DataContext is ProfileTemplate template &&
            SelectedGameText.Text != "Select a game to configure")
        {
            _activeProfiles[SelectedGameText.Text] = template.Name;

            foreach (var game in _games)
            {
                if (game.Title == SelectedGameText.Text)
                    game.Profile = template.Name;
            }

            GameList.ItemsSource = null;
            GameList.ItemsSource = _games;

            ProfileList.ItemsSource = _templates.Select(t => new ProfileTemplate
            {
                Name = t.Name,
                Description = t.Description,
                ActiveText = t.Name == template.Name ? "ACTIVE" : ""
            }).ToList();

            ToastManager.Show($"{template.Name} applied to {SelectedGameText.Text}");
        }
    }

    public class GameProfileItem
    {
        public string Title { get; set; } = "";
        public string Profile { get; set; } = "Gamepad (Default)";
        public bool IsSelected { get; set; }
    }

    public class ProfileTemplate
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string ActiveText { get; set; } = "";
    }
}
