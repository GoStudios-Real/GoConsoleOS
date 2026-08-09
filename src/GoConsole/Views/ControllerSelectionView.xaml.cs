using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using GoConsoleOS.GoConsole.Controls;
using GoConsoleOS.Shared;
using GoConsoleOS.Shared.Input;

namespace GoConsoleOS.GoConsole.Views;

public partial class ControllerSelectionView : UserControl
{
    private readonly ControllerEngine? _controller;
    private DispatcherTimer? _testTimer;
    private readonly Dictionary<ControllerButtons, (ControllerButtonIcon Icon, string Description)> _testButtons = new();

    public ControllerSelectionView()
    {
        InitializeComponent();
        _controller = (Application.Current.MainWindow as MainWindow)?.Controller;

        _testButtons[ControllerButtons.A] = (TestA, "A");
        _testButtons[ControllerButtons.B] = (TestB, "B");
        _testButtons[ControllerButtons.X] = (TestX, "X");
        _testButtons[ControllerButtons.Y] = (TestY, "Y");
        _testButtons[ControllerButtons.DPadUp] = (TestDPadUp, "D-Pad Up");
        _testButtons[ControllerButtons.DPadDown] = (TestDPadDown, "D-Pad Down");
        _testButtons[ControllerButtons.DPadLeft] = (TestDPadLeft, "D-Pad Left");
        _testButtons[ControllerButtons.DPadRight] = (TestDPadRight, "D-Pad Right");
        _testButtons[ControllerButtons.LeftShoulder] = (TestLB, "Left Bumper");
        _testButtons[ControllerButtons.RightShoulder] = (TestRB, "Right Bumper");
        _testButtons[ControllerButtons.Guide] = (TestGuide, "Guide");
        _testButtons[ControllerButtons.Start] = (TestStart, "Start");
        _testButtons[ControllerButtons.Back] = (TestBack, "Back");

        Loaded += (_, _) => OnLoaded();
        Unloaded += (_, _) => OnUnloaded();
    }

    private void OnLoaded()
    {
        var saved = SettingsStore.Get("controller.kind", "Auto") ?? "Auto";
        HighlightKind(saved);

        var mouseEmu = SettingsStore.GetBool("services.mouse_emulation", true);
        var vib = SettingsStore.GetBool("services.vibration", true);
        MouseEmulationCheck.IsChecked = mouseEmu;
        VibrationCheck.IsChecked = vib;

        if (_controller != null)
        {
            _controller.StateUpdated += OnControllerState;
            _controller.Connected += OnControllerConnected;
            _controller.Disconnected += OnControllerDisconnected;
            UpdateConnectionStatus();
        }

        RefreshLayout();

        _testTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _testTimer.Tick += (_, _) => UpdateTestInput();
        _testTimer.Start();
    }

    private void OnUnloaded()
    {
        _testTimer?.Stop();
        if (_controller != null)
        {
            _controller.StateUpdated -= OnControllerState;
            _controller.Connected -= OnControllerConnected;
            _controller.Disconnected -= OnControllerDisconnected;
        }
    }

    private void OnControllerConnected() => Dispatcher.Invoke(UpdateConnectionStatus);
    private void OnControllerDisconnected() => Dispatcher.Invoke(UpdateConnectionStatus);

    private void OnControllerState(ControllerState state)
    {
        Dispatcher.Invoke(() =>
        {
            foreach (var (button, entry) in _testButtons)
            {
                entry.Icon.Opacity = state.IsButtonDown(button) ? 1.0 : 0.35;
            }
        });
    }

    private void UpdateTestInput()
    {
        if (_controller == null || !_controller.IsConnected) return;
        var state = _controller.CurrentState;
        TestAnalogL.Text = $"L ⬅➡ {state.LeftStickX * 100:0}%  ⬆⬇ {state.LeftStickY * 100:0}%";
        TestAnalogR.Text = $"R ⬅➡ {state.RightStickX * 100:0}%  ⬆⬇ {state.RightStickY * 100:0}%";
    }

    private void UpdateConnectionStatus()
    {
        if (_controller == null)
        {
            ConnectionStatus.Text = "Controller engine unavailable";
            ConnectionStatus.Foreground = FindBrush("BrushTextMuted");
            DetectedBadge.Visibility = Visibility.Collapsed;
            return;
        }

        if (_controller.IsConnected)
        {
            ConnectionStatus.Text = $"Controller connected (Player {_controller.ControllerIndex + 1})";
            ConnectionStatus.Foreground = FindBrush("BrushSuccess");
            DetectedBadge.Visibility = Visibility.Visible;
            DetectedText.Text = _controller.GetKindName();
        }
        else
        {
            ConnectionStatus.Text = "No controller detected";
            ConnectionStatus.Foreground = FindBrush("BrushTextMuted");
            DetectedBadge.Visibility = Visibility.Collapsed;
        }
    }

    private static Brush FindBrush(string key)
        => (Application.Current.TryFindResource(key) as Brush) ?? Brushes.Gray;

    private void HighlightKind(string kind)
    {
        foreach (var border in new[] { KindAuto, KindXbox, KindPlayStation5, KindSwitch2, KindGeneric })
        {
            var active = border.Tag as string == kind;
            border.Background = active ? FindBrush("BrushAccentPrimary") : FindBrush("BrushBackgroundLight");
            var text = (border.Child as StackPanel)?.Children[1] as TextBlock;
            if (text != null) text.Foreground = active ? Brushes.Black : FindBrush("BrushTextPrimary");
        }
    }

    private void SelectKind(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border b && b.Tag is string kind)
        {
            HighlightKind(kind);
            SettingsStore.Set("controller.kind", kind);
            if (_controller != null && Enum.TryParse<ControllerKind>(kind, out var parsed))
                _controller.SetKind(parsed);
            RefreshLayout();
            SoundManager.Play("select");
        }
    }

    private void RefreshLayout()
    {
        var kindName = _controller?.GetKindName() ?? "Auto (Xbox layout)";
        LayoutKindText.Text = kindName;

        var items = new List<LayoutItem>
        {
            new(ControllerButtons.A, _controller?.GetButtonLabel(ControllerButtons.A) ?? "A", "Primary action / Confirm"),
            new(ControllerButtons.B, _controller?.GetButtonLabel(ControllerButtons.B) ?? "B", "Cancel / Back"),
            new(ControllerButtons.X, _controller?.GetButtonLabel(ControllerButtons.X) ?? "X", "Secondary action"),
            new(ControllerButtons.Y, _controller?.GetButtonLabel(ControllerButtons.Y) ?? "Y", "Tertiary action"),
            new(ControllerButtons.Start, _controller?.GetButtonLabel(ControllerButtons.Start) ?? "≡", "Menu"),
            new(ControllerButtons.Back, _controller?.GetButtonLabel(ControllerButtons.Back) ?? "▢▢", "View / Guide"),
            new(ControllerButtons.LeftShoulder, _controller?.GetButtonLabel(ControllerButtons.LeftShoulder) ?? "LB", "Left bumper"),
            new(ControllerButtons.RightShoulder, _controller?.GetButtonLabel(ControllerButtons.RightShoulder) ?? "RB", "Right bumper"),
            new(ControllerButtons.Guide, _controller?.GetButtonLabel(ControllerButtons.Guide) ?? "◉", "System"),
        };
        LayoutList.ItemsSource = items;
    }

    private void OnToggleChanged(object sender, RoutedEventArgs e)
    {
        SettingsStore.SetBool("services.mouse_emulation", MouseEmulationCheck.IsChecked == true);
        SettingsStore.SetBool("services.vibration", VibrationCheck.IsChecked == true);
    }

    private void TestVibration_Click(object sender, MouseButtonEventArgs e)
    {
        _controller?.SetVibration(65535, 65535);
        var t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        t.Tick += (_, _) => { t.Stop(); _controller?.SetVibration(0, 0); };
        t.Start();
        SoundManager.Play("select");
    }

    private void OpenRemapping_Click(object sender, MouseButtonEventArgs e)
    {
        (Application.Current.MainWindow as MainWindow)?.NavigateTo("remap");
    }

    private sealed record LayoutItem(ControllerButtons Button, string Label, string Description);
}
