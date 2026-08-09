using System;
using System.Linq;
using System.Windows;

namespace GoConsoleOS.GoConsole;

public static class ToastManager
{
    public static void Show(string message, int seconds = 3)
    {
        var window = Application.Current.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w is MainWindow);

        if (window is MainWindow main)
        {
            main.ShowNotification(message, seconds);
        }
    }
}
