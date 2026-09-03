using Microsoft.Win32;

namespace GoConsoleWatcher;

public static class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppValueName = "GoConsoleWatcher";

    public static void EnsureRegistered()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
            if (key == null) return;

            var currentExe = Environment.ProcessPath ?? "";
            var existing = key.GetValue(AppValueName) as string ?? "";

            if (existing != currentExe)
            {
                key.SetValue(AppValueName, $"\"{currentExe}\"");
            }
        }
        catch
        {
            // silently fail - non-critical
        }
    }

    public static void Unregister()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
            if (key == null) return;
            key.DeleteValue(AppValueName, false);
        }
        catch
        {
            // silently fail
        }
    }
}
