using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GoConsoleOS.Shared;

namespace GoConsoleOS.GoConsole.Views;

public partial class WhatsNewView : UserControl
{
    private static readonly (string Icon, string Title, string Detail, string Tag)[] Features =
    {
        ("💬", "Discord", "Chat, voice calls, friends list, and the Discord Token Creator. Your token stays on-device at system\\discord\\config.json.", "SOCIAL"),
        ("🗃️", "Data Store", "A real persistent settings database (system\\settings.json) powers sound, display, theme and performance options.", "SYSTEM"),
        ("🏆", "Achievements", "8 unlockable achievements with toast notifications and a dedicated Achievement Center.", "SYSTEM"),
        ("💾", "Game Save Backup", "Back up and restore saves for your installed games with one click.", "STORAGE"),
        ("🖥️", "True Fullscreen", "Boot straight into borderless fullscreen, or a tidy 1280x720 window.", "DISPLAY"),
        ("🎨", "Accent Colors", "8 preset accent colors plus a custom color picker to theme the whole console.", "THEME"),
        ("🖥️", "CPU Support", "Auto-detects Intel Core i5/i7/i9 and AMD Ryzen 3/5/7/9 and shows your tier recommendation.", "HARDWARE"),
        ("🛒", "Expanded Store", "The catalog grew to 43 items with osu!, OpenRCT2, FreeCiv, The Powder Toy, Shattered Pixel Dungeon, HandBrake and Python.", "STORE"),
        ("🌙", "Night Mode", "Dim the whole experience for late-night sessions.", "DISPLAY"),
        ("🎮", "Controller Engine", "Gamepad-first navigation with mouse emulation and vibration.", "INPUT"),
        ("🎮", "Controller Selection", "A full Controller screen — auto-detect, pick Xbox, PlayStation 5 (DualSense) or Nintendo Switch 2 layouts, live button & stick test, vibration test, and a per-kind button layout preview.", "INPUT"),
        ("🛡️", "USB Device Health", "S.M.A.R.T.-aware health scores for every connected USB drive — status, errors, serial, firmware, and mounted volumes.", "STORAGE"),
        ("⬇️", "On-Screen Scroll", "A visible scroll indicator appears as you scroll, and the D-Pad now scrolls long pages on the home screen.", "INPUT"),
        ("✏️", "Pen Support", "Use a stylus to tap and navigate the whole console.", "INPUT"),
        ("👆", "Touchscreen Support", "Fully touch-friendly — tap, swipe and select with your finger.", "INPUT"),
        ("🎬", "Brand Splash", "GoStudios brand screen auto-plays before the console logo on every boot.", "BRAND"),
        ("⚡", "Performance Modes", "Balanced, Power and Battery Saver profiles.", "SYSTEM"),
        ("💡", "New Sounds", "An all-new 44.1 kHz sound engine mixing Xbox crispness with PS5 airy reverb.", "AUDIO")
    };

    public WhatsNewView()
    {
        InitializeComponent();
        BuildList();
    }

    private void BuildList()
    {
        foreach (var (icon, title, detail, tag) in Features)
        {
            var border = new Border
            {
                CornerRadius = new CornerRadius(12),
                Background = (Brush)FindResource("BrushBackgroundLight"),
                Padding = new Thickness(18),
                Margin = new Thickness(0, 0, 0, 12)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var iconText = new TextBlock { Text = icon, FontSize = 30, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(iconText, 0);

            var textStack = new StackPanel { Margin = new Thickness(16, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            var titleText = new TextBlock
            {
                Text = title,
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                Foreground = (Brush)FindResource("BrushTextPrimary")
            };
            var detailText = new TextBlock
            {
                Text = detail,
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 0),
                Foreground = (Brush)FindResource("BrushTextSecondary")
            };
            textStack.Children.Add(titleText);
            textStack.Children.Add(detailText);
            Grid.SetColumn(textStack, 1);

            var tagBorder = new Border
            {
                CornerRadius = new CornerRadius(4),
                Background = (Brush)FindResource("BrushBackgroundCard"),
                Padding = new Thickness(8, 3, 8, 3),
                VerticalAlignment = VerticalAlignment.Center
            };
            var tagText = new TextBlock
            {
                Text = tag,
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = (Brush)FindResource("BrushAccentSecondary")
            };
            tagBorder.Child = tagText;
            Grid.SetColumn(tagBorder, 2);

            grid.Children.Add(iconText);
            grid.Children.Add(textStack);
            grid.Children.Add(tagBorder);
            border.Child = grid;

            FeatureList.Children.Add(border);
        }
    }
}
