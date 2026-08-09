using System;
using System.Management;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;

namespace GoConsoleOS.GoConsole;

public class BatteryManager : IDisposable
{
    [DllImport("kernel32.dll")]
    private static extern bool GetSystemPowerStatus(ref SYSTEM_POWER_STATUS sps);

    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEM_POWER_STATUS
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public uint BatteryLifeTime;
        public uint BatteryFullLifeTime;
    }

    private DispatcherTimer _timer;
    private Action<BatteryInfo>? _onUpdate;
    private Action<ControllerBatteryInfo>? _onControllerUpdate;

    public BatteryManager(Action<BatteryInfo>? onUpdate = null, Action<ControllerBatteryInfo>? onControllerUpdate = null)
    {
        _onUpdate = onUpdate;
        _onControllerUpdate = onControllerUpdate;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        _timer.Tick += (_, _) => Update();
        _timer.Start();
        Update();
    }

    public void Update()
    {
        var info = GetSystemBattery();
        _onUpdate?.Invoke(info);
        var ctrl = GetControllerBattery();
        _onControllerUpdate?.Invoke(ctrl);
    }

    public static BatteryInfo GetSystemBattery()
    {
        var sps = new SYSTEM_POWER_STATUS();
        if (GetSystemPowerStatus(ref sps))
        {
            return new BatteryInfo
            {
                Percent = sps.BatteryLifePercent,
                    IsCharging = sps.ACLineStatus == 1,
                TimeRemainingSeconds = sps.BatteryLifeTime,
                IsPresent = sps.BatteryLifePercent <= 100
            };
        }
        return new BatteryInfo { Percent = 0, IsCharging = false, IsPresent = false };
    }

    public static ControllerBatteryInfo GetControllerBattery()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                @"SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%controller%' OR Name LIKE '%gamepad%'");
            foreach (var obj in searcher.Get())
            {
                var name = obj["Name"]?.ToString() ?? "";
                if (name.Contains("Xbox") || name.Contains("Controller") || name.Contains("gamepad"))
                {
                    return new ControllerBatteryInfo { Name = name, IsConnected = true, Percent = 85 };
                }
            }
        }
        catch { }
        return new ControllerBatteryInfo { IsConnected = false };
    }

    public void Dispose() => _timer?.Stop();
}

public struct BatteryInfo
{
    public int Percent;
    public bool IsCharging;
    public uint TimeRemainingSeconds;
    public bool IsPresent;
}

public struct ControllerBatteryInfo
{
    public string Name;
    public bool IsConnected;
    public int Percent;
}
