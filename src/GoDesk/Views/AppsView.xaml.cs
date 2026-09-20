using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;

namespace GoDesk.Views;

public partial class AppsView : UserControl
{
    public AppsView()
    {
        InitializeComponent();
    }

    private void Launch(string app, string args = "")
    {
        try { Process.Start(new ProcessStartInfo(app, args) { UseShellExecute = true }); }
        catch { }
    }

    private void LaunchSettings(object s, MouseButtonEventArgs e) => Launch("ms-settings:");
    private void LaunchExplorer(object s, MouseButtonEventArgs e) => Launch("explorer.exe");
    private void LaunchTerminal(object s, MouseButtonEventArgs e) => Launch("cmd.exe");
    private void LaunchTaskManager(object s, MouseButtonEventArgs e) => Launch("taskmgr.exe");
    private void LaunchBrowser(object s, MouseButtonEventArgs e) => Launch("http://gostudios.app.com");
    private void LaunchCalculator(object s, MouseButtonEventArgs e) => Launch("calc.exe");
    private void LaunchNotepad(object s, MouseButtonEventArgs e) => Launch("notepad.exe");
    private void LaunchPaint(object s, MouseButtonEventArgs e) => Launch("mspaint.exe");
    private void LaunchSnippingTool(object s, MouseButtonEventArgs e) => Launch("snippingtool.exe");
    private void LaunchDeviceManager(object s, MouseButtonEventArgs e) => Launch("devmgmt.msc");
    private void LaunchControlPanel(object s, MouseButtonEventArgs e) => Launch("control.exe");
}
