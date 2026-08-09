using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GoConsoleOS.GoConsole.Views;

public partial class KeyboardWindow : Window
{
    private bool _shift;
    private readonly Brush _defaultBg;
    private readonly Brush _hoverBg;

    private static readonly string[] Row1 = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };
    private static readonly string[] Row1Shift = { "!", "@", "#", "$", "%", "^", "&", "*", "(", ")" };
    private static readonly string[] Row2 = { "q", "w", "e", "r", "t", "y", "u", "i", "o", "p" };
    private static readonly string[] Row2Shift = { "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P" };
    private static readonly string[] Row3 = { "a", "s", "d", "f", "g", "h", "j", "k", "l", "-" };
    private static readonly string[] Row3Shift = { "A", "S", "D", "F", "G", "H", "J", "K", "L", "_" };
    private static readonly string[] Row4 = { "z", "x", "c", "v", "b", "n", "m", ".", ",", "/" };
    private static readonly string[] Row4Shift = { "Z", "X", "C", "V", "B", "N", "M", ".", ",", "?" };

    public string InputText => InputBox.Text;

    public KeyboardWindow()
    {
        InitializeComponent();
        _defaultBg = FindResource("BrushBackgroundCard") as Brush ?? Brushes.Gray;
        _hoverBg = FindResource("BrushBackgroundCardHover") as Brush ?? Brushes.DimGray;
        BuildKeyboard();
        UpdateCapsIndicator();
    }

    private void BuildKeyboard()
    {
        var rows = new[] { Row1, Row2, Row3, Row4 };

        for (int r = 0; r < rows.Length; r++)
        {
            var wrap = new WrapPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 3, 0, 3)
            };
            Grid.SetRow(wrap, r);

            foreach (var key in rows[r])
            {
                var btn = MakeKeyButton(key, r);
                wrap.Children.Add(btn);
            }

            KeyboardGrid.Children.Add(wrap);
        }
    }

    private Button MakeKeyButton(string key, int row)
    {
        var display = GetDisplayKey(key, row);
        var btn = new Button
        {
            Content = display,
            Tag = key,
            Width = 72,
            Height = 44,
            Margin = new Thickness(3),
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            Foreground = FindResource("BrushTextPrimary") as Brush ?? Brushes.White,
            Background = _defaultBg,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand
        };
        btn.MouseEnter += (_, _) => btn.Background = _hoverBg;
        btn.MouseLeave += (_, _) => btn.Background = _defaultBg;
        btn.Click += Key_Click;
        return btn;
    }

    private string GetDisplayKey(string key, int row)
    {
        if (!_shift) return key;
        return row switch
        {
            0 => Row1Shift[Array.IndexOf(Row1, key)],
            1 => Row2Shift[Array.IndexOf(Row2, key)],
            2 => Row3Shift[Array.IndexOf(Row3, key)],
            3 => Row4Shift[Array.IndexOf(Row4, key)],
            _ => key
        };
    }

    private void RefreshKeyboard()
    {
        KeyboardGrid.Children.Clear();
        BuildKeyboard();
        UpdateCapsIndicator();
    }

    private void UpdateCapsIndicator()
    {
        if (_shift)
        {
            CapsText.Text = "ABC";
            CapsText.Foreground = FindResource("BrushAccentPrimary") as Brush ?? Brushes.Cyan;
        }
        else
        {
            CapsText.Text = "abc";
            CapsText.Foreground = FindResource("BrushTextSecondary") as Brush ?? Brushes.Gray;
        }
    }

    private void Key_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            var key = btn.Tag?.ToString() ?? "";
            InputBox.Text += _shift ? key.ToUpper() : key;
            if (_shift) { _shift = false; RefreshKeyboard(); }
            InputBox.CaretIndex = InputBox.Text.Length;
        }
    }

    private void Shift_Click(object sender, RoutedEventArgs e)
    {
        _shift = !_shift;
        RefreshKeyboard();
    }

    private void Backspace_Click(object sender, RoutedEventArgs e)
    {
        if (InputBox.Text.Length > 0)
            InputBox.Text = InputBox.Text[..^1];
        InputBox.CaretIndex = InputBox.Text.Length;
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        InputBox.Text = "";
    }

    private void Done_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                DialogResult = true;
                Close();
                break;
            case Key.Escape:
                DialogResult = false;
                Close();
                break;
            case Key.Back:
                Backspace_Click(sender, e);
                break;
        }
    }
}
