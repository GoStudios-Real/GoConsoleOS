using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GoConsoleOS.Shared.Input;

namespace GoConsoleOS.GoConsole.Controls;

public partial class ControllerButtonIcon : UserControl
{
    public static readonly DependencyProperty ButtonProperty =
        DependencyProperty.Register(nameof(Button), typeof(ControllerButtons), typeof(ControllerButtonIcon),
            new PropertyMetadata(ControllerButtons.A, OnVisualChanged));

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(ControllerButtonIcon),
            new PropertyMetadata(null, OnVisualChanged));

    public ControllerButtons Button
    {
        get => (ControllerButtons)GetValue(ButtonProperty);
        set => SetValue(ButtonProperty, value);
    }

    public string? Label
    {
        get => (string?)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    private static readonly Brush BrushWhite = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xFF));
    private static readonly Brush BrushDark = new SolidColorBrush(Color.FromRgb(0x0D, 0x0D, 0x14));
    private static readonly Brush BrushGreen = new SolidColorBrush(Color.FromRgb(0x1F, 0xAF, 0x55));
    private static readonly Brush BrushRed = new SolidColorBrush(Color.FromRgb(0xD6, 0x3A, 0x2F));
    private static readonly Brush BrushBlue = new SolidColorBrush(Color.FromRgb(0x3A, 0x76, 0xD2));
    private static readonly Brush BrushYellow = new SolidColorBrush(Color.FromRgb(0xE5, 0xA9, 0x28));

    public ControllerButtonIcon()
    {
        InitializeComponent();
        Render();
    }

    private static void OnVisualChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((ControllerButtonIcon)d).Render();

    private void Render()
    {
        Face.Visibility = Visibility.Collapsed;
        Keycap.Visibility = Visibility.Collapsed;

        if (!string.IsNullOrEmpty(Label))
        {
            Keycap.Visibility = Visibility.Visible;
            Glyph.Text = Label;
            Glyph.FontSize = Label!.Length > 2 ? 11 : 12;
            Glyph.Foreground = BrushWhite;
            return;
        }

        switch (Button)
        {
            case ControllerButtons.A: SetFace(BrushGreen, "A", false); break;
            case ControllerButtons.B: SetFace(BrushRed, "B", false); break;
            case ControllerButtons.X: SetFace(BrushBlue, "X", false); break;
            case ControllerButtons.Y: SetFace(BrushYellow, "Y", true); break;
            case ControllerButtons.DPadUp: SetKey("▲"); break;
            case ControllerButtons.DPadDown: SetKey("▼"); break;
            case ControllerButtons.DPadLeft: SetKey("◀"); break;
            case ControllerButtons.DPadRight: SetKey("▶"); break;
            case ControllerButtons.Start: SetKey("≡"); break;
            case ControllerButtons.Back: SetKey("▢▢"); break;
            case ControllerButtons.Guide: SetKey("◉"); break;
            case ControllerButtons.LeftShoulder: SetKey("LB"); break;
            case ControllerButtons.RightShoulder: SetKey("RB"); break;
            default: SetKey("●"); break;
        }
    }

    private void SetFace(Brush fill, string ch, bool darkGlyph)
    {
        Face.Visibility = Visibility.Visible;
        Face.Fill = fill;
        Glyph.Text = ch;
        Glyph.FontSize = 14;
        Glyph.Foreground = darkGlyph ? BrushDark : BrushWhite;
    }

    private void SetKey(string ch)
    {
        Keycap.Visibility = Visibility.Visible;
        Glyph.Text = ch;
        Glyph.FontSize = ch.Length > 1 ? 12 : 14;
        Glyph.Foreground = BrushWhite;
    }
}
