using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GoConsoleOS.GoConsole.Views;

public partial class CustomColorPicker : Window
{
    private Color _selectedColor = Color.FromRgb(0x00, 0xC9, 0xDB);
    private string _selectedTheme = "dark";

    public CustomColorPicker()
    {
        InitializeComponent();
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape) Close();
        };
    }

    private void PickColor(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string hex)
        {
            _selectedColor = (Color)ColorConverter.ConvertFromString(hex);
            PreviewColor.Background = new SolidColorBrush(_selectedColor);
            HexInput.Text = hex;
        }
    }

    private void SetTheme(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string theme)
        {
            _selectedTheme = theme;
            var accent = TryFindResource("BrushAccentPrimary") as SolidColorBrush;
            var muted = TryFindResource("BrushTextMuted") as SolidColorBrush;
            ThemeDark.Background = theme == "dark" ? accent : new SolidColorBrush(Color.FromRgb(0x0D, 0x0D, 0x14));
            ThemeDarker.Background = theme == "darker" ? accent : new SolidColorBrush(Color.FromRgb(0x05, 0x05, 0x08));
            ThemeAmber.Background = theme == "amber" ? accent : new SolidColorBrush(Color.FromRgb(0x1A, 0x15, 0x08));
        }
    }

    private void HexInput_Changed(object sender, TextChangedEventArgs e)
    {
        try
        {
            if (HexInput.Text.Length == 7 && HexInput.Text[0] == '#')
            {
                _selectedColor = (Color)ColorConverter.ConvertFromString(HexInput.Text);
                PreviewColor.Background = new SolidColorBrush(_selectedColor);
            }
        }
        catch { }
    }

    private void Apply_Click(object sender, MouseButtonEventArgs e)
    {
        var resources = Application.Current.Resources;
        resources["BrushAccentPrimary"] = new SolidColorBrush(_selectedColor);
        resources["BrushFocusGlow"] = new SolidColorBrush(_selectedColor);

        foreach (Window w in Application.Current.Windows)
        {
            if (w.IsVisible)
            {
                w.InvalidateVisual();
                if (w is MainWindow main)
                {
                    main.UpdateThemeColors(_selectedColor, _selectedTheme);
                }
            }
        }

        Close();
    }

    private void Cancel_Click(object sender, MouseButtonEventArgs e)
    {
        Close();
    }
}
