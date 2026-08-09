using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GoConsoleOS.Shared;

namespace GoConsoleOS.GoConsole.Views;

public partial class AccessibilityView : UserControl
{
    private bool _highContrast;
    private bool _colorFilter;
    private bool _reduceMotion;
    private bool _narrator;
    private bool _monoAudio;
    private bool _visualSound;
    private bool _largeText;
    private bool _cursorSnap = true;
    private bool _pen;
    private bool _touch;

    public AccessibilityView()
    {
        InitializeComponent();
        _pen = SettingsStore.GetBool("input.pen", true);
        _touch = SettingsStore.GetBool("input.touch", true);
        PenText.Text = _pen ? "ON" : "OFF";
        TouchText.Text = _touch ? "ON" : "OFF";
    }

    private void ToggleToggle(Border toggleBorder, TextBlock toggleText, ref bool state,
        string onLabel, string offLabel, Brush? onBg, Brush? offBg)
    {
        state = !state;
        toggleText.Text = state ? onLabel : offLabel;
        toggleBorder.Background = state
            ? (onBg ?? FindResource("BrushSuccess") as Brush ?? Brushes.Green)
            : (offBg ?? FindResource("BrushBackgroundCard") as Brush);
        toggleText.Foreground = state ? Brushes.Black : (FindResource("BrushTextPrimary") as Brush ?? Brushes.White);
    }

    private void ToggleHighContrast(object sender, MouseButtonEventArgs e)
    {
        ToggleToggle(HighContrastToggle, HighContrastText, ref _highContrast, "ON", "OFF",
            FindResource("BrushWarning") as Brush, FindResource("BrushBackgroundCard") as Brush);
        var main = Window.GetWindow(this) as MainWindow;
        if (main != null)
        {
            if (_highContrast)
            {
                main.Background = Brushes.Black;
                main.Foreground = Brushes.White;
            }
            else
            {
                var bgColor = Color.FromRgb(0x0D, 0x0D, 0x14);
                main.UpdateThemeColors(Color.FromRgb(0x00, 0xC9, 0xDB), "dark");
            }
        }
    }

    private void ToggleColorFilter(object sender, MouseButtonEventArgs e)
    {
        ToggleToggle(ColorFilterToggle, ColorFilterText, ref _colorFilter, "ON", "OFF",
            FindResource("BrushAccentSecondary") as Brush, FindResource("BrushBackgroundCard") as Brush);
    }

    private void ToggleReduceMotion(object sender, MouseButtonEventArgs e)
    {
        ToggleToggle(ReduceMotionToggle, ReduceMotionText, ref _reduceMotion, "ON", "OFF",
            FindResource("BrushBackgroundCard") as Brush, FindResource("BrushBackgroundCard") as Brush);
    }

    private void ToggleNarrator(object sender, MouseButtonEventArgs e)
    {
        ToggleToggle(NarratorToggle, NarratorText, ref _narrator, "ON", "OFF",
            FindResource("BrushError") as Brush, FindResource("BrushBackgroundCard") as Brush);
    }

    private void ToggleMonoAudio(object sender, MouseButtonEventArgs e)
    {
        ToggleToggle(MonoAudioToggle, MonoAudioText, ref _monoAudio, "ON", "OFF",
            FindResource("BrushWarning") as Brush, FindResource("BrushBackgroundCard") as Brush);
    }

    private void ToggleVisualSound(object sender, MouseButtonEventArgs e)
    {
        ToggleToggle(VisualSoundToggle, VisualSoundText, ref _visualSound, "ON", "OFF",
            FindResource("BrushAccentPrimary") as Brush, FindResource("BrushBackgroundCard") as Brush);
    }

    private void ToggleLargeText(object sender, MouseButtonEventArgs e)
    {
        ToggleToggle(LargeTextToggle, LargeTextText, ref _largeText, "ON", "OFF",
            FindResource("BrushAccentPrimary") as Brush, FindResource("BrushBackgroundCard") as Brush);
    }

    private void ToggleCursorSnap(object sender, MouseButtonEventArgs e)
    {
        ToggleToggle(CursorSnapToggle, CursorSnapText, ref _cursorSnap, "ON", "OFF",
            FindResource("BrushSuccess") as Brush, FindResource("BrushBackgroundCard") as Brush);
    }

    private void TogglePen(object sender, MouseButtonEventArgs e)
    {
        _pen = !_pen;
        SettingsStore.SetBool("input.pen", _pen);
        ToggleToggle(PenToggle, PenText, ref _pen, "ON", "OFF",
            FindResource("BrushSuccess") as Brush, FindResource("BrushBackgroundCard") as Brush);
        Logger.Info($"Pen support: {(_pen ? "ON" : "OFF")}");
    }

    private void ToggleTouch(object sender, MouseButtonEventArgs e)
    {
        _touch = !_touch;
        SettingsStore.SetBool("input.touch", _touch);
        ToggleToggle(TouchToggle, TouchText, ref _touch, "ON", "OFF",
            FindResource("BrushSuccess") as Brush, FindResource("BrushBackgroundCard") as Brush);
        Logger.Info($"Touchscreen support: {(_touch ? "ON" : "OFF")}");
    }
}
