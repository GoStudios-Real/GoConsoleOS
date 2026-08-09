using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using GoConsoleOS.Shared.Input;

namespace GoConsoleOS.GoConsole.Views;

public partial class OnScreenKeyboard : Window
{
    private readonly ControllerEngine? _controller;
    private bool _shift;
    private readonly List<Border> _allKeys = new();

    private static readonly string[] NumberRow = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };
    private static readonly string[] NumberRowShift = { "!", "@", "#", "$", "%", "^", "&", "*", "(", ")" };
    private static readonly string[] TopRow = { "q", "w", "e", "r", "t", "y", "u", "i", "o", "p" };
    private static readonly string[] TopRowShift = { "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P" };
    private static readonly string[] HomeRow = { "a", "s", "d", "f", "g", "h", "j", "k", "l" };
    private static readonly string[] HomeRowShift = { "A", "S", "D", "F", "G", "H", "J", "K", "L" };
    private static readonly string[] BottomRow = { "z", "x", "c", "v", "b", "n", "m", ",", ".", "/" };
    private static readonly string[] BottomRowShift = { "Z", "X", "C", "V", "B", "N", "M", "<", ">", "?" };

    public string InputText => InputBox.Text;

    private readonly List<List<Border>> _rows = new();
    private int _row;
    private int _col;
    private readonly DispatcherTimer _holdTimer;

    public OnScreenKeyboard(ControllerEngine? controller = null)
    {
        InitializeComponent();
        _controller = controller ?? (Application.Current.MainWindow as MainWindow)?.Controller;
        BuildKeyboard();

        _holdTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(90) };
        _holdTimer.Tick += (_, _) =>
        {
            if (_controller == null || !_controller.IsConnected) { _holdTimer.Stop(); return; }
            var buttons = _controller.CurrentState.Buttons;
            var held = false;
            if ((buttons & (ushort)ControllerButtons.DPadUp) != 0) { MoveCursor(-1, 0); held = true; }
            else if ((buttons & (ushort)ControllerButtons.DPadDown) != 0) { MoveCursor(1, 0); held = true; }
            else if ((buttons & (ushort)ControllerButtons.DPadLeft) != 0) { MoveCursor(0, -1); held = true; }
            else if ((buttons & (ushort)ControllerButtons.DPadRight) != 0) { MoveCursor(0, 1); held = true; }
            if (!held) _holdTimer.Stop();
        };

        Loaded += (_, _) =>
        {
            FocusInput();
            ConnectController();
        };
    }

    public void SetInitialText(string text)
    {
        InputBox.Text = text ?? "";
        CharCount.Text = InputBox.Text.Length.ToString();
    }

    private void ConnectController()
    {
        if (_controller == null) return;
        _controller.ButtonPressed += OnControllerButton;
        _controller.ButtonReleased += OnControllerButtonReleased;
        _controller.StateUpdated += OnControllerState;
        if (_rows.Count > 0)
            ApplyCursor();
    }

    private void DisconnectController()
    {
        if (_controller == null) return;
        _controller.ButtonPressed -= OnControllerButton;
        _controller.ButtonReleased -= OnControllerButtonReleased;
        _controller.StateUpdated -= OnControllerState;
    }

    private void OnControllerState(ControllerState state)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (_controller == null || !_controller.IsConnected) return;
            var x = state.ThumbLX;
            var y = state.ThumbLY;
            if (Math.Abs(x) < 12000 && Math.Abs(y) < 12000) return;

            if (Math.Abs(x) > Math.Abs(y))
                MoveCursor(0, x > 0 ? 1 : -1);
            else
                MoveCursor(y > 0 ? -1 : 1, 0);
        }));
    }

    private void OnControllerButton(ControllerButtons button)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            switch (button)
            {
                case ControllerButtons.DPadUp: MoveCursor(-1, 0); _holdTimer.Start(); break;
                case ControllerButtons.DPadDown: MoveCursor(1, 0); _holdTimer.Start(); break;
                case ControllerButtons.DPadLeft: MoveCursor(0, -1); _holdTimer.Start(); break;
                case ControllerButtons.DPadRight: MoveCursor(0, 1); _holdTimer.Start(); break;
                case ControllerButtons.A: ActivateCursor(); break;
                case ControllerButtons.B: Backspace_Click(null!, null!); break;
                case ControllerButtons.Y: Shift_Click(null!, null!); break;
                case ControllerButtons.X: Clear_Click(null!, null!); break;
                case ControllerButtons.Start: Enter_Click(null!, null!); break;
                case ControllerButtons.Guide:
                case ControllerButtons.Back:
                    Close();
                    break;
            }
        }));
    }

    private void OnControllerButtonReleased(ControllerButtons button)
    {
        if (button is ControllerButtons.DPadUp or ControllerButtons.DPadDown
            or ControllerButtons.DPadLeft or ControllerButtons.DPadRight)
            _holdTimer.Stop();
    }

    private void MoveCursor(int dr, int dc)
    {
        if (_rows.Count == 0) return;
        var newRow = Math.Clamp(_row + dr, 0, _rows.Count - 1);
        var targetCount = _rows[newRow].Count;
        var newCol = Math.Clamp(_col + dc, 0, targetCount - 1);
        if (dc != 0 && newCol == _col && Math.Abs(dc) > 0)
        {
            // wrap around within the row
            newCol = _col + dc;
            if (newCol < 0) newCol = targetCount - 1;
            if (newCol >= targetCount) newCol = 0;
        }
        _row = newRow;
        _col = newCol;
        ApplyCursor();
    }

    private void ApplyCursor()
    {
        var accent = FindResource("BrushAccentPrimary") as Brush ?? Brushes.Cyan;
        var transparent = Brushes.Transparent;
        foreach (var row in _rows)
            foreach (var key in row)
            {
                key.BorderBrush = transparent;
                key.BorderThickness = new Thickness(2);
            }
        if (_row < _rows.Count && _col < _rows[_row].Count)
        {
            var current = _rows[_row][_col];
            current.BorderBrush = accent;
            current.BorderThickness = new Thickness(3);
        }
    }

    private void ActivateCursor()
    {
        if (_row >= _rows.Count || _col >= _rows[_row].Count) return;
        var key = _rows[_row][_col];
        if (key == ShiftKey) { Shift_Click(null!, null!); return; }
        if (key == SpaceKey) { Space_Click(null!, null!); return; }
        if (key == BackspaceKey) { Backspace_Click(null!, null!); return; }
        if (key == ClearKey) { Clear_Click(null!, null!); return; }
        if (key == CopyKey) { Copy_Click(null!, null!); return; }
        if (key == PasteKey) { Paste_Click(null!, null!); return; }
        if (key == EnterKey) { Enter_Click(null!, null!); return; }
        if (key.Tag is string s)
            TypeKey(s);
    }

    private void FocusInput()
    {
        InputBox.Focus();
        InputBox.CaretIndex = InputBox.Text.Length;
    }

    private void BuildKeyboard()
    {
        _rows.Clear();
        BuildRow(Row1Panel, NumberRow, NumberRowShift);
        BuildRow(Row2Panel, TopRow, TopRowShift);
        BuildRow(Row3Panel, HomeRow, HomeRowShift);
        BuildRow(Row4Panel, BottomRow, BottomRowShift);
        _rows.Add(new List<Border> { ShiftKey, SpaceKey, BackspaceKey, CopyKey, PasteKey, ClearKey, EnterKey });
    }

    private void BuildRow(WrapPanel panel, string[] keys, string[] shiftKeys)
    {
        panel.Children.Clear();
        var row = new List<Border>();
        for (int i = 0; i < keys.Length; i++)
        {
            var idx = i;
            var key = keys[i];
            var border = new Border
            {
                CornerRadius = new CornerRadius(6),
                Background = FindResource("BrushBackgroundCard") as Brush ?? new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x32)),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(2, 2, 2, 2),
                Cursor = Cursors.Hand,
                BorderThickness = new Thickness(2),
                BorderBrush = Brushes.Transparent,
                Tag = key,
                Child = new TextBlock
                {
                    Text = _shift ? shiftKeys[idx] : key,
                    FontSize = 17,
                    FontWeight = FontWeights.Bold,
                    Foreground = FindResource("BrushTextPrimary") as Brush ?? Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };

            border.MouseLeftButtonDown += (_, _) => TypeKey(key);
            border.MouseEnter += (_, _) => border.Background = FindResource("BrushBackgroundCardHover") as Brush;
            border.MouseLeave += (_, _) =>
                border.Background = FindResource("BrushBackgroundCard") as Brush;

            _allKeys.Add(border);
            row.Add(border);
            panel.Children.Add(border);
        }
        _rows.Add(row);
    }

    private void RefreshKeyboard()
    {
        Row1Panel.Children.Clear();
        Row2Panel.Children.Clear();
        Row3Panel.Children.Clear();
        Row4Panel.Children.Clear();
        _allKeys.Clear();
        BuildKeyboard();
        UpdateShiftUI();
        ApplyCursor();
        FocusInput();
    }

    private void UpdateShiftUI()
    {
        ShiftKey.Background = _shift
            ? FindResource("BrushAccentPrimary") as Brush
            : FindResource("BrushBackgroundCard") as Brush;
        ShiftText.Foreground = _shift
            ? new SolidColorBrush(Color.FromRgb(0x0D, 0x0D, 0x14))
            : FindResource("BrushTextPrimary") as Brush;
    }

    private void TypeKey(string key)
    {
        InputBox.Text += _shift ? key.ToUpper() : key;
        InputBox.CaretIndex = InputBox.Text.Length;
        CharCount.Text = InputBox.Text.Length.ToString();
        SoundManager.Play("key");
        if (_shift) { _shift = false; RefreshKeyboard(); }
    }

    private void Overlay_Click(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource == OverlayBg)
            Close();
    }

    private void Shift_Click(object sender, MouseButtonEventArgs e)
    {
        _shift = !_shift;
        RefreshKeyboard();
    }

    private void Space_Click(object sender, MouseButtonEventArgs e)
    {
        InputBox.Text += " ";
        InputBox.CaretIndex = InputBox.Text.Length;
        CharCount.Text = InputBox.Text.Length.ToString();
    }

    private void Backspace_Click(object sender, MouseButtonEventArgs e)
    {
        if (InputBox.Text.Length > 0)
            InputBox.Text = InputBox.Text[..^1];
        InputBox.CaretIndex = InputBox.Text.Length;
        CharCount.Text = InputBox.Text.Length.ToString();
    }

    private void Clear_Click(object sender, MouseButtonEventArgs e)
    {
        InputBox.Text = "";
        CharCount.Text = "0";
        FocusInput();
    }

    private void Copy_Click(object sender, MouseButtonEventArgs e)
    {
        if (InputBox.Text.Length == 0)
        {
            SoundManager.Play("error");
            Clipboard.Clear();
            return;
        }
        try
        {
            Clipboard.SetText(InputBox.Text);
            SoundManager.Play("select");
        }
        catch
        {
            // clipboard in use elsewhere; ignore
        }
    }

    private void Paste_Click(object sender, MouseButtonEventArgs e)
    {
        try
        {
            if (!Clipboard.ContainsText())
            {
                SoundManager.Play("error");
                return;
            }
            var text = Clipboard.GetText();
            if (text == null) return;
            var sanitized = text.Replace("\r\n", "\n").Replace('\r', '\n');
            InputBox.Text = InputBox.Text + sanitized;
            InputBox.CaretIndex = InputBox.Text.Length;
            CharCount.Text = InputBox.Text.Length.ToString();
            SoundManager.Play("select");
        }
        catch
        {
            // clipboard in use elsewhere; ignore
        }
    }

    private void Enter_Click(object sender, MouseButtonEventArgs e)
    {
        DisconnectController();
        SoundManager.Play("select");
        DialogResult = true;
        Close();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            if (e.Key == Key.C) { Copy_Click(sender, null!); e.Handled = true; return; }
            if (e.Key == Key.V) { Paste_Click(sender, null!); e.Handled = true; return; }
        }
        switch (e.Key)
        {
            case Key.Enter:
                Enter_Click(sender, null!); break;
            case Key.Escape:
                Close(); break;
            case Key.Back:
                Backspace_Click(sender, null!); break;
            case Key.LeftShift:
            case Key.RightShift:
                Shift_Click(sender, null!); break;
            case Key.Space:
                Space_Click(sender, null!); break;
            default:
                var c = KeyToChar(e.Key);
                if (c != null) TypeKey(c);
                break;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _holdTimer.Stop();
        DisconnectController();
        base.OnClosed(e);
    }

    private static string? KeyToChar(Key key)
    {
        if (key >= Key.A && key <= Key.Z) return key.ToString().ToLower();
        if (key >= Key.D0 && key <= Key.D9) return ((char)('0' + (key - Key.D0))).ToString();
        if (key >= Key.NumPad0 && key <= Key.NumPad9) return ((char)('0' + (key - Key.NumPad0))).ToString();
        return key switch
        {
            Key.OemPeriod => ".",
            Key.OemComma => ",",
            Key.OemMinus => "-",
            Key.OemQuestion => "/",
            _ => null
        };
    }
}
