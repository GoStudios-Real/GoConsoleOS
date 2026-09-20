using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;

namespace GoDesk.Views;

public partial class GameBarView : UserControl
{
    public GameBarView()
    {
        InitializeComponent();
        VolumeSlider.ValueChanged += (_, e) => VolumeText.Text = $"{(int)e.NewValue}%";
        BrightnessSlider.ValueChanged += (_, e) => BrightnessText.Text = $"{(int)e.NewValue}%";
    }

    private void ToggleWifi(object s, MouseButtonEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("netsh", "interface set interface \"Wi-Fi\" disable") { UseShellExecute = true, Verb = "runas" }); }
        catch { }
    }

    private void ToggleBluetooth(object s, MouseButtonEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("ms-settings:bluetooth") { UseShellExecute = true }); }
        catch { }
    }

    private void ToggleDND(object s, MouseButtonEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("ms-settings:quiet-hours") { UseShellExecute = true }); }
        catch { }
    }

    private void TakeScreenshot(object s, MouseButtonEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("snippingtool.exe") { UseShellExecute = true }); }
        catch { }
    }

    private void ToggleRecording(object s, MouseButtonEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("ms-screenclip:") { UseShellExecute = true }); }
        catch { }
    }

    private void OpenTaskManager(object s, MouseButtonEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo("taskmgr.exe") { UseShellExecute = true }); }
        catch { }
    }
}
