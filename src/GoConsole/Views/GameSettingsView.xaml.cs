using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GoConsoleOS.Shared;
using GoConsoleOS.Shared.Models;

namespace GoConsoleOS.GoConsole.Views;

public partial class GameSettingsView : UserControl
{
    private readonly LibraryData _library = new();
    private Dictionary<string, GameConfig> _configs = new();
    private string _configPath = "";
    private List<GameItem> _games = new();
    private string _selectedGameId = "";

    public GameSettingsView()
    {
        InitializeComponent();
        _configPath = Path.Combine(ConfigReader.RootPath ?? "", "system", "game_settings.json");
        LoadConfigs();
        LoadLibrary();
        LoadGames();
    }

    private void LoadConfigs()
    {
        try
        {
            if (File.Exists(_configPath))
                _configs = JsonSerializer.Deserialize<Dictionary<string, GameConfig>>(File.ReadAllText(_configPath)) ?? new();
        }
        catch { }
    }

    private void LoadLibrary()
    {
        try
        {
            var path = Path.Combine(ConfigReader.RootPath ?? "", "launcher", "library", "library.json");
            if (File.Exists(path))
                _library.Games = JsonSerializer.Deserialize<List<GameInfo>>(File.ReadAllText(path)) ?? new();
        }
        catch { }
    }

    private void LoadGames()
    {
        _games = _library.Games
            .Where(g => g.IsInstalled)
            .OrderBy(g => g.Title)
            .Select(g => new GameItem { Id = g.Id, Title = g.Title, Platform = g.Platform })
            .ToList();

        if (_games.Count == 0)
            _games.Add(new GameItem { Id = "", Title = "No games found", Platform = "Scan your library first" });

        GameList.ItemsSource = _games;
    }

    private void SelectGame(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.DataContext is GameItem item)
        {
            _selectedGameId = item.Id;
            SelectedGameText.Text = item.Title;
            SelectedGameDesc.Text = $"{item.Platform} \u2022 Per-game configuration";
            SavedText.Text = "";

            if (_configs.TryGetValue(item.Id, out var cfg))
            {
                PresetBox.SelectedIndex = cfg.Preset;
                FrameLimitBox.SelectedIndex = cfg.FrameLimit;
                CompatBox.SelectedIndex = cfg.Compatibility;
                LaunchArgsBox.Text = cfg.LaunchArgs;
            }
            else
            {
                PresetBox.SelectedIndex = 2;
                FrameLimitBox.SelectedIndex = 1;
                CompatBox.SelectedIndex = 0;
                LaunchArgsBox.Text = "";
            }
        }
    }

    private void SaveSettings(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedGameId))
        {
            SavedText.Text = "Select a game first";
            return;
        }

        _configs[_selectedGameId] = new GameConfig
        {
            Preset = PresetBox.SelectedIndex,
            FrameLimit = FrameLimitBox.SelectedIndex,
            Compatibility = CompatBox.SelectedIndex,
            LaunchArgs = LaunchArgsBox.Text.Trim()
        };

        try
        {
            var dir = Path.GetDirectoryName(_configPath);
            if (dir != null) Directory.CreateDirectory(dir);
            File.WriteAllText(_configPath, JsonSerializer.Serialize(_configs, new JsonSerializerOptions { WriteIndented = true }));
            SavedText.Text = $"Settings saved for {SelectedGameText.Text}";
            ToastManager.Show($"Settings saved for {SelectedGameText.Text}");
        }
        catch (System.Exception ex)
        {
            SavedText.Text = $"Failed to save: {ex.Message}";
        }
    }

    private void ResetSettings(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedGameId)) return;
        _configs.Remove(_selectedGameId);
        SaveSettings(sender, e);
        PresetBox.SelectedIndex = 2;
        FrameLimitBox.SelectedIndex = 1;
        CompatBox.SelectedIndex = 0;
        LaunchArgsBox.Text = "";
    }

    public class GameItem
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Platform { get; set; } = "";
    }

    public class GameConfig
    {
        public int Preset { get; set; }
        public int FrameLimit { get; set; }
        public int Compatibility { get; set; }
        public string LaunchArgs { get; set; } = "";
    }
}
