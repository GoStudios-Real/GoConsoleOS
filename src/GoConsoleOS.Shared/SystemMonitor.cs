using System.Diagnostics;
using System.Runtime.InteropServices;
using GoConsoleOS.Shared.Models;

namespace GoConsoleOS.Shared;

public class SystemMonitor : IDisposable
{
    private Thread? _monitorThread;
    private volatile bool _running;
    private int _intervalMs;
    private readonly PerformanceCounter? _cpuCounter;
    private readonly PerformanceCounter? _ramCounter;
    private readonly PerformanceCounter? _ramAvailableCounter;

    public SystemStats CurrentStats { get; } = new();
    public event Action<SystemStats>? StatsUpdated;

    public SystemMonitor(int intervalMs = 1000)
    {
        _intervalMs = Math.Max(100, intervalMs);
        try
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _cpuCounter.NextValue();
            _ramCounter = new PerformanceCounter("Memory", "% Committed Bytes In Use");
            _ramAvailableCounter = new PerformanceCounter("Memory", "Available MBytes");
        }
        catch
        {
            Logger.Warn("Performance counters not available (may need admin rights)");
        }
    }

    public void Start()
    {
        if (_running) return;
        _running = true;
        _monitorThread = new Thread(MonitorLoop)
        {
            IsBackground = true,
            Name = "SystemMonitor"
        };
        _monitorThread.Start();
    }

    public void Stop()
    {
        _running = false;
        _monitorThread?.Join(2000);
    }

    private void MonitorLoop()
    {
        var lastCpuSample = new TimeSpan[Environment.ProcessorCount];
        var lastCpuTime = new TimeSpan[Environment.ProcessorCount];
        var process = Process.GetCurrentProcess();

        while (_running)
        {
            try
            {
                var stats = new SystemStats
                {
                    Timestamp = DateTime.UtcNow
                };

                if (_cpuCounter != null)
                {
                    stats.CpuUsagePercent = (float)Math.Round(_cpuCounter.NextValue(), 1);
                }
                else
                {
                    stats.CpuUsagePercent = (float)Math.Round(GetCpuUsageFallback(), 1);
                }

                if (_ramCounter != null)
                {
                    stats.RamUsagePercent = (float)Math.Round(_ramCounter.NextValue(), 1);
                }

                if (_ramAvailableCounter != null)
                {
                    var availMb = _ramAvailableCounter.NextValue();
                    var totalMb = GetTotalRamMb();
                    stats.RamUsedMb = (long)(totalMb - availMb);
                    stats.RamTotalMb = (long)totalMb;
                    if (stats.RamUsagePercent == 0 && totalMb > 0)
                        stats.RamUsagePercent = (float)Math.Round(stats.RamUsedMb / totalMb * 100, 1);
                }

                StatsUpdated?.Invoke(stats);
            }
            catch { }

            Thread.Sleep(_intervalMs);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhys;
        public ulong AvailPhys;
        public ulong TotalPageFile;
        public ulong AvailPageFile;
        public ulong TotalVirtual;
        public ulong AvailVirtual;
        public ulong AvailExtendedVirtual;
    }

    [DllImport("kernel32.dll")]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx lpBuffer);

    private static double GetTotalRamMb()
    {
        try
        {
            var memStatus = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
            if (GlobalMemoryStatusEx(ref memStatus))
                return memStatus.TotalPhys / (1024.0 * 1024.0);
        }
        catch { }
        return 16384;
    }

    private static double GetCpuUsageFallback()
    {
        try
        {
            var startTime = DateTime.UtcNow;
            var startCpu = Process.GetCurrentProcess().TotalProcessorTime;
            Thread.Sleep(100);
            var endTime = DateTime.UtcNow;
            var endCpu = Process.GetCurrentProcess().TotalProcessorTime;
            var cpuUsedMs = (endCpu - startCpu).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            return Math.Round(cpuUsedMs / (totalMsPassed * Environment.ProcessorCount) * 100, 1);
        }
        catch
        {
            return 0;
        }
    }

    public void Dispose()
    {
        Stop();
        _cpuCounter?.Dispose();
        _ramCounter?.Dispose();
        _ramAvailableCounter?.Dispose();
        GC.SuppressFinalize(this);
    }
}
