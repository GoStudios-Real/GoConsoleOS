using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;

namespace GoDesk.Views;

public partial class GoConsoleModeView : UserControl
{
    public GoConsoleModeView()
    {
        InitializeComponent();
    }

    private void LaunchGoConsole(object s, MouseButtonEventArgs e)
    {
        // Try USB first, then local
        var usbPath = "D:\\GoConsoleOS.exe";
        var localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GoConsoleOS.exe");
        var parentPath = Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)?.FullName ?? AppDomain.CurrentDomain.BaseDirectory, "GoConsoleOS.exe");

        if (File.Exists(usbPath))
            Process.Start(new ProcessStartInfo(usbPath) { UseShellExecute = true });
        else if (File.Exists(localPath))
            Process.Start(new ProcessStartInfo(localPath) { UseShellExecute = true });
        else if (File.Exists(parentPath))
            Process.Start(new ProcessStartInfo(parentPath) { UseShellExecute = true });
    }
}
