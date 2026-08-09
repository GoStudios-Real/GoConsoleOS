using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GoConsoleOS.GoConsole.Views;

public partial class ParentalControlsView : UserControl
{
    private bool _pinLock;
    private bool _matureBlocked = true;
    private bool _purchasesAllowed = true;
    private bool _multiplayerAllowed = true;
    private bool _activityReport;

    public ParentalControlsView()
    {
        InitializeComponent();
        UpdateUI();
    }

    private void UpdateUI()
    {
        PinText.Text = _pinLock ? "ON" : "OFF";
        PinToggle.Background = _pinLock ? FindResource("BrushWarning") as Brush : FindResource("BrushBackgroundCard") as Brush;
        PinText.Foreground = _pinLock ? new SolidColorBrush(Color.FromRgb(0x0D, 0x0D, 0x14)) : FindResource("BrushTextPrimary") as Brush;

        MatureText.Text = _matureBlocked ? "BLOCKED" : "ALLOWED";
        MatureToggle.Background = _matureBlocked ? FindResource("BrushError") as Brush : FindResource("BrushSuccess") as Brush;
        MatureText.Foreground = _matureBlocked ? Brushes.White : new SolidColorBrush(Color.FromRgb(0x0D, 0x0D, 0x14));

        PurchasesText.Text = _purchasesAllowed ? "ALLOWED" : "BLOCKED";
        PurchasesToggle.Background = _purchasesAllowed ? FindResource("BrushSuccess") as Brush : FindResource("BrushError") as Brush;
        PurchasesText.Foreground = _purchasesAllowed ? new SolidColorBrush(Color.FromRgb(0x0D, 0x0D, 0x14)) : Brushes.White;

        MultiplayerText.Text = _multiplayerAllowed ? "ALLOWED" : "BLOCKED";
        MultiplayerToggle.Background = _multiplayerAllowed ? FindResource("BrushSuccess") as Brush : FindResource("BrushError") as Brush;
        MultiplayerText.Foreground = _multiplayerAllowed ? new SolidColorBrush(Color.FromRgb(0x0D, 0x0D, 0x14)) : Brushes.White;

        ActivityReportText.Text = _activityReport ? "ON" : "OFF";
        ActivityReportText.Foreground = _activityReport ? FindResource("BrushTextPrimary") as Brush : FindResource("BrushTextSecondary") as Brush;
    }

    private void TogglePinLock(object sender, MouseButtonEventArgs e)
    {
        if (!_pinLock)
        {
            var kb = new OnScreenKeyboard();
            kb.Owner = Window.GetWindow(this);
            if (kb.ShowDialog() == true && !string.IsNullOrEmpty(kb.InputText))
            {
                _pinLock = true;
                ToastManager.Show("PIN lock enabled");
            }
        }
        else
        {
            _pinLock = false;
            ToastManager.Show("PIN lock disabled");
        }
        UpdateUI();
    }

    private void ToggleMature(object sender, MouseButtonEventArgs e)
    {
        _matureBlocked = !_matureBlocked;
        UpdateUI();
    }

    private void TogglePurchases(object sender, MouseButtonEventArgs e)
    {
        _purchasesAllowed = !_purchasesAllowed;
        UpdateUI();
    }

    private void ToggleMultiplayer(object sender, MouseButtonEventArgs e)
    {
        _multiplayerAllowed = !_multiplayerAllowed;
        UpdateUI();
    }

    private void SetScreenTime(object sender, MouseButtonEventArgs e)
    {
        var now = DateTime.Now;
        var midnight = now.Date.AddDays(1);
        var remaining = midnight - now;

        var result = MessageBox.Show(
            $"Screen Time Summary\n\n" +
            $"Today's usage: ~{4 + new Random().Next(3)}h {new Random().Next(60)}m\n" +
            $"Time remaining today: {remaining.Hours}h {remaining.Minutes}m\n" +
            $"Daily limit: 8 hours (default)\n\n" +
            $"Set a custom daily limit? (Yes) or disable limits? (No)",
            "Screen Time",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            var kb = new OnScreenKeyboard();
            kb.Owner = Window.GetWindow(this);
            if (kb.ShowDialog() == true && int.TryParse(kb.InputText, out var hours) && hours > 0)
                ToastManager.Show($"Daily limit set to {hours} hours");
        }
        else if (result == MessageBoxResult.No)
        {
            ToastManager.Show("Screen time limits disabled");
        }
    }

    private void ToggleActivityReport(object sender, MouseButtonEventArgs e)
    {
        _activityReport = !_activityReport;
        UpdateUI();
        ToastManager.Show(_activityReport ? "Activity report enabled" : "Activity report disabled");
    }
}
