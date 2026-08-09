using System;
using System.IO;
using System.Threading.Tasks;
using GoConsoleOS.Shared;
using Microsoft.Web.WebView2.Core;

namespace GoConsoleOS.GoConsole.Views;

internal static class WebView2Support
{
    private static CoreWebView2Environment? _cachedEnv;
    private static string? _dataFolder;

    public static async Task<CoreWebView2Environment> GetEnvironmentAsync()
    {
        if (_cachedEnv != null) return _cachedEnv;
        _dataFolder = Path.Combine(ConfigReader.RootPath ?? Path.GetTempPath(), "system", "webview2");
        var options = new CoreWebView2EnvironmentOptions
        {
            AdditionalBrowserArguments = "--disable-gpu"
        };
        _cachedEnv = await CoreWebView2Environment.CreateAsync(null, _dataFolder, options);
        return _cachedEnv;
    }

    public static void KillOwnProcesses()
    {
        try
        {
            if (_dataFolder == null) return;
            using var searcher = new System.Management.ManagementObjectSearcher(
                "SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name = 'msedgewebview2.exe'");
            var killed = 0;
            foreach (var o in searcher.Get())
            {
                var cmd = o["CommandLine"]?.ToString() ?? "";
                if (cmd.IndexOf(_dataFolder, StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (int.TryParse(o["ProcessId"]?.ToString(), out var pid))
                {
                    try { System.Diagnostics.Process.GetProcessById(pid)?.Kill(); killed++; } catch { }
                }
            }
            if (killed > 0) Logger.Info($"WebView2 cleanup killed {killed} process(es)");
        }
        catch (Exception ex)
        {
            Logger.Warn($"WebView2 cleanup: {ex.Message}");
        }
    }
}
