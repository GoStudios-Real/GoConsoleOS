using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GoConsoleOS.GoConsole.Views;

public partial class ThemeSettingsView : UserControl
{
    private ThemeDefinition? _currentTheme;

    public ThemeSettingsView()
    {
        InitializeComponent();
        LoadPresets();
        LoadCurrentTheme();
    }

    private void LoadPresets()
    {
        PresetList.ItemsSource = ThemeManager.GetAllThemes();
    }

    private void LoadCurrentTheme()
    {
        var name = ThemeManager.CurrentThemeName ?? "GoConsole Dark";
        CurrentThemeLabel.Text = $"Current: {name}";
        var theme = ThemeManager.GetAllThemes().FirstOrDefault(t => t.Name == name)
                    ?? ThemeManager.PresetThemes[0];
        _currentTheme = theme;
        ApplyToFields(theme);
    }

    private void ApplyToFields(ThemeDefinition theme)
    {
        AccentPrimaryBox.Text = theme.AccentPrimary;
        AccentSecondaryBox.Text = theme.AccentSecondary;
        AccentTertiaryBox.Text = theme.AccentTertiary;
        BgDarkBox.Text = theme.BackgroundDark;
        BgLightBox.Text = theme.BackgroundLight;
        TextPrimaryBox.Text = theme.TextPrimary;
        UpdatePreview();
    }

    private ThemeDefinition ReadFromFields()
    {
        return new ThemeDefinition
        {
            Name = _currentTheme?.Name ?? "Custom",
            AccentPrimary = AccentPrimaryBox.Text,
            AccentSecondary = AccentSecondaryBox.Text,
            AccentTertiary = AccentTertiaryBox.Text,
            BackgroundDark = BgDarkBox.Text,
            BackgroundLight = BgLightBox.Text,
            TextPrimary = TextPrimaryBox.Text,
        };
    }

    private void UpdatePreview()
    {
        try
        {
            PreviewSwatch.Background = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(AccentPrimaryBox.Text));
            PreviewText.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString(TextPrimaryBox.Text));
        }
        catch { }
    }

    private void SelectTheme(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string name)
        {
            var theme = ThemeManager.GetAllThemes().FirstOrDefault(t => t.Name == name);
            if (theme != null)
            {
                _currentTheme = theme;
                ThemeManager.CurrentThemeName = name;
                CurrentThemeLabel.Text = $"Current: {name}";
                ThemeManager.ApplyTheme(theme);
                ApplyToFields(theme);
                ToastManager.Show($"Theme applied: {name}");
            }
        }
    }

    private void ColorFieldChanged(object sender, TextChangedEventArgs e)
    {
        UpdatePreview();
        if (sender is TextBox tb && tb.Tag is string key && !string.IsNullOrEmpty(tb.Text))
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(tb.Text);
                Application.Current.Resources[key] = new SolidColorBrush(color);
                // Update derived brushes
                if (key == "AccentPrimary")
                {
                    Application.Current.Resources["BrushFocusGlow"] = new SolidColorBrush(color);
                    PreviewSwatch.Background = new SolidColorBrush(color);
                }
            }
            catch { }
        }
    }

    private void SaveTheme(object sender, RoutedEventArgs e)
    {
        var theme = ReadFromFields();
        theme.Name = $"Custom {DateTime.Now:yyMMdd-HHmm}";
        ThemeManager.SaveCustomTheme(theme);
        ThemeManager.CurrentThemeName = theme.Name;
        CurrentThemeLabel.Text = $"Current: {theme.Name}";
        ThemeManager.ApplyTheme(theme);
        LoadPresets();
        ToastManager.Show($"Theme saved: {theme.Name}");
    }

    private void ResetToDefault(object sender, RoutedEventArgs e)
    {
        var def = ThemeManager.PresetThemes[0];
        _currentTheme = def;
        ThemeManager.CurrentThemeName = def.Name;
        CurrentThemeLabel.Text = $"Current: {def.Name}";
        ThemeManager.ApplyTheme(def);
        ApplyToFields(def);
        ToastManager.Show("Reset to GoConsole Dark");
    }
}
