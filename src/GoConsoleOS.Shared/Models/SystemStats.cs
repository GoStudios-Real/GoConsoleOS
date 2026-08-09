namespace GoConsoleOS.Shared.Models;

public class SystemStats
{
    public float CpuUsagePercent { get; set; }
    public float GpuUsagePercent { get; set; }
    public float RamUsagePercent { get; set; }
    public long RamUsedMb { get; set; }
    public long RamTotalMb { get; set; }
    public float GpuTemperatureCelsius { get; set; }
    public float CpuTemperatureCelsius { get; set; }
    public int Fps { get; set; }
    public int NetworkDownKbps { get; set; }
    public int NetworkUpKbps { get; set; }
    public string ActivePerformanceMode { get; set; } = "balanced";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
