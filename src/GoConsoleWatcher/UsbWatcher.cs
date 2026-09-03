using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace GoConsoleWatcher;

public class UsbWatcher : IDisposable
{
    private const int WM_DEVICECHANGE = 0x0219;
    private const int DBT_DEVICEARRIVAL = 0x8000;
    private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
    private const uint DBT_DEVTYP_VOLUME = 0x00000002;
    private const int VolumeOffset = 12;
    private const int DeviceTypeOffset = 4;

    private HwndSource? _hwndSource;
    private readonly Action<string> _log;
    private readonly Action<string> _balloon;
    private char? _gocDrive;

    public UsbWatcher(Action<string> log, Action<string> balloon)
    {
        _log = log;
        _balloon = balloon;
    }

    public void Attach(Window window)
    {
        var helper = new WindowInteropHelper(window);
        var source = HwndSource.FromHwnd(helper.Handle);
        if (source == null) return;

        _hwndSource = source;
        source.AddHook(WndProc);
        _log("USB device-change monitoring enabled (WM_DEVICECHANGE)");
    }

    public void Detach()
    {
        if (_hwndSource == null) return;
        _hwndSource.RemoveHook(WndProc);
        _hwndSource = null;
    }

    public void Dispose()
    {
        Detach();
        GC.SuppressFinalize(this);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WM_DEVICECHANGE) return IntPtr.Zero;

        switch (wParam.ToInt32())
        {
            case DBT_DEVICEARRIVAL:
                handled = true;
                OnDeviceEvent(true, lParam);
                break;
            case DBT_DEVICEREMOVECOMPLETE:
                handled = true;
                OnDeviceEvent(false, lParam);
                break;
        }

        return IntPtr.Zero;
    }

    private void OnDeviceEvent(bool pluggedIn, IntPtr lParam)
    {
        try
        {
            var letters = ParseVolumeLetters(lParam);
            if (letters == null || letters.Count == 0) return;

            foreach (var driveLetter in letters)
            {
                if (pluggedIn)
                    OnDriveArrived(driveLetter);
                else
                    OnDriveRemoved(driveLetter);
            }
        }
        catch (Exception ex)
        {
            _log($"USB event error: {ex.Message}");
        }
    }

    private void OnDriveArrived(char driveLetter)
    {
        var initCfgPath = $@"{driveLetter}:\boot\init.cfg";

        try
        {
            if (!File.Exists(initCfgPath)) return;

            var lines = File.ReadAllLines(initCfgPath);
            var isGoConsole = lines.Any(l =>
                l.StartsWith("os_name=", StringComparison.OrdinalIgnoreCase) &&
                l.Substring(8).Trim().Equals("GoConsoleOS", StringComparison.OrdinalIgnoreCase));

            if (!isGoConsole) return;

            var exePath = $@"{driveLetter}:\GoConsoleOS.exe";
            if (!File.Exists(exePath))
            {
                _log($"GoConsoleOS.exe not found on {driveLetter}:");
                return;
            }

            _gocDrive = driveLetter;
            _log($"GoConsoleOS detected on {driveLetter}: - launching");

            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true
            });

            _balloon($"GoConsoleOS launching from {driveLetter}:");
        }
        catch (Exception ex)
        {
            _log($"Error checking {driveLetter}: {ex.Message}");
        }
    }

    private void OnDriveRemoved(char driveLetter)
    {
        if (_gocDrive == null || _gocDrive.Value != driveLetter) return;

        _log($"GoConsoleOS drive {driveLetter}: removed - closing");

        try
        {
            var processes = Process.GetProcessesByName("GoConsoleOS");
            foreach (var proc in processes)
            {
                try
                {
                    proc.CloseMainWindow();
                }
                catch
                {
                    // ignore
                }
            }
        }
        catch (Exception ex)
        {
            _log($"Error closing GoConsoleOS: {ex.Message}");
        }

        _gocDrive = null;
    }

    private static List<char>? ParseVolumeLetters(IntPtr lParam)
    {
        if (lParam == IntPtr.Zero) return null;

        var deviceType = (uint)Marshal.ReadInt32(lParam, DeviceTypeOffset);
        if (deviceType != DBT_DEVTYP_VOLUME) return null;

        var mask = Marshal.ReadInt32(lParam, VolumeOffset);
        var letters = new List<char>();

        for (var i = 0; i < 26; i++)
        {
            if ((mask & (1 << i)) != 0)
                letters.Add((char)('A' + i));
        }

        return letters.Count == 0 ? null : letters;
    }
}
