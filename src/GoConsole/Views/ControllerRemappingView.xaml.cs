using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GoConsoleOS.Shared;

namespace GoConsoleOS.GoConsole.Views;

public partial class ControllerRemappingView : UserControl
{
    private readonly Dictionary<string, string> _mappings = new()
    {
        ["A"] = "A (Accept)",
        ["B"] = "B (Back)",
        ["X"] = "X (Action)",
        ["Y"] = "Y (Search)",
        ["Guide"] = "Guide (Menu)",
        ["Start"] = "Start (Options)",
        ["Back"] = "Back (Select)",
        ["DPadUp"] = "D-Pad Up",
        ["DPadDown"] = "D-Pad Down",
        ["DPadLeft"] = "D-Pad Left",
        ["DPadRight"] = "D-Pad Right",
        ["LB"] = "LB (Left Bumper)",
        ["RB"] = "RB (Right Bumper)",
        ["LT"] = "LT (Left Trigger)",
        ["RT"] = "RT (Right Trigger)",
        ["LStick"] = "L-Stick Click",
        ["RStick"] = "R-Stick Click",
    };

    public ControllerRemappingView()
    {
        InitializeComponent();
        BuildList();
    }

    private void BuildList()
    {
        var items = new List<RemapItem>();
        foreach (var kv in _mappings)
        {
            items.Add(new RemapItem
            {
                Id = kv.Key,
                Button = kv.Key,
                MappedTo = kv.Value
            });
        }
        RemapGrid.ItemsSource = items;
    }

    private void RemapButton(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string id)
        {
            var keyboard = new OnScreenKeyboard();
            keyboard.Owner = Window.GetWindow(this);

            if (keyboard.ShowDialog() == true && !string.IsNullOrEmpty(keyboard.InputText))
            {
                var text = keyboard.InputText.Trim();
                _mappings[id] = text;
                BuildList();
                Logger.Info($"Remapped {id} → {text}");
            }
        }
    }

    private void ResetButton(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string id)
        {
            var defaults = new Dictionary<string, string>
            {
                ["A"] = "A (Accept)", ["B"] = "B (Back)", ["X"] = "X (Action)", ["Y"] = "Y (Search)",
                ["Guide"] = "Guide (Menu)", ["Start"] = "Start (Options)", ["Back"] = "Back (Select)",
                ["DPadUp"] = "D-Pad Up", ["DPadDown"] = "D-Pad Down", ["DPadLeft"] = "D-Pad Left", ["DPadRight"] = "D-Pad Right",
                ["LB"] = "LB (Left Bumper)", ["RB"] = "RB (Right Bumper)",
                ["LT"] = "LT (Left Trigger)", ["RT"] = "RT (Right Trigger)",
                ["LStick"] = "L-Stick Click", ["RStick"] = "R-Stick Click",
            };

            if (defaults.TryGetValue(id, out var def))
            {
                _mappings[id] = def;
                BuildList();
            }
        }
    }

    public class RemapItem
    {
        public string Id { get; set; } = "";
        public string Button { get; set; } = "";
        public string MappedTo { get; set; } = "";
    }
}
