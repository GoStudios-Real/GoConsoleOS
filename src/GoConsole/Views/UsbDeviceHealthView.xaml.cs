using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GoConsoleOS.Shared;

namespace GoConsoleOS.GoConsole.Views;

public partial class UsbDeviceHealthView : UserControl
{
    private UsbDevice? _selected;

    public UsbDeviceHealthView()
    {
        InitializeComponent();
        Loaded += (_, _) => RefreshHealth();
    }

    private void RefreshHealth_Click(object sender, MouseButtonEventArgs e) => RefreshHealth();

    /// <summary>Public entry point used by the shell when a USB device event arrives.</summary>
    public void RefreshNow() => RefreshHealth();

    private void RefreshHealth()
    {
        var items = new List<UsbDevice>();

        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT DeviceID, Model, Status, InterfaceType, MediaType, Size, SerialNumber, FirmwareRevision, Partitions, SCSIBus, SCSIPort, PNPDeviceID FROM Win32_DiskDrive");
            foreach (ManagementBaseObject o in searcher.Get())
            {
                var iface = o["InterfaceType"]?.ToString() ?? "";
                var pnp = o["PNPDeviceID"]?.ToString() ?? "";
                var isUsb = iface.Equals("USB", StringComparison.OrdinalIgnoreCase) ||
                            pnp.Contains("USBSTOR", StringComparison.OrdinalIgnoreCase) ||
                            iface.Equals("IEEE 1394", StringComparison.OrdinalIgnoreCase);

                if (!isUsb) continue;

                var status = o["Status"]?.ToString() ?? "Unknown";
                var model = o["Model"]?.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(model)) model = o["DeviceID"]?.ToString() ?? "USB Storage Device";
                var serial = o["SerialNumber"]?.ToString()?.Trim() ?? "";
                var mediaType = o["MediaType"]?.ToString() ?? "";
                var sizeBytes = ParseUlong(o["Size"]);
                var firmware = o["FirmwareRevision"]?.ToString() ?? "";
                var deviceId = o["DeviceID"]?.ToString() ?? "";
                var partitions = o["Partitions"]?.ToString() ?? "—";

                    ReadSmartStatus(deviceId, out var smartOk, out var smartFailPredict, out var smartErrors);

                var health = ComputeHealth(status, smartOk, smartFailPredict, smartErrors);
                var drives = MapToDriveLetters(deviceId);

                items.Add(new UsbDevice
                {
                    DeviceId = deviceId,
                    Name = model,
                    Detail = $"{iface}  •  {FormatSize(sizeBytes)}  •  {mediaType}".Trim(),
                    Serial = serial,
                    MediaType = mediaType,
                    Firmware = firmware,
                    Interface = iface,
                    Partitions = partitions,
                    SizeBytes = sizeBytes,
                    Status = status,
                    SmartOk = smartOk,
                    SmartFailPredict = smartFailPredict,
                    SmartErrors = smartErrors,
                    Drives = drives,
                    HealthScore = health.Score,
                    HealthLabel = health.Label,
                    HealthBrush = health.Brush,
                    HealthDesc = health.Description,
                    Stats = BuildStats(o, deviceId, status, serial, mediaType, firmware, sizeBytes, drives, health)
                });
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"USB health scan failed: {ex}");
            DeviceStatus.Text = "Health scan failed: " + ex.Message;
        }

        DeviceList.ItemsSource = items;

        if (items.Count == 0)
            DeviceStatus.Text = "No USB storage devices found — plug one in and press Refresh";
        else
            DeviceStatus.Text = $"{items.Count} USB device{(items.Count == 1 ? "" : "s")} found";

        if (_selected != null)
        {
            var match = items.FirstOrDefault(d => d.DeviceId == _selected.DeviceId);
            if (match != null) Select(match);
            else _selected = null;
        }

        if (_selected == null && items.Count > 0)
            Select(items[0]);
        else if (_selected == null)
            ClearDetail();
    }

    private static (int Score, string Label, string Brush, string Description) ComputeHealth(
        string status, bool smartOk, bool smartFailPredict, long smartErrors)
    {
        var ok = status.Equals("OK", StringComparison.OrdinalIgnoreCase) ||
                 status.Equals("Degraded", StringComparison.OrdinalIgnoreCase);

        if (!status.Equals("OK", StringComparison.OrdinalIgnoreCase))
            return (15, "POOR", "#E53935", $"Device status reports \"{status}\". The device may be failing.");

        if (!smartOk)
            return (55, "FAIR", "#FB8C00", "S.M.A.R.T. is not exposed for this USB bridge, so error stats are unavailable.");

        if (smartFailPredict)
            return (15, "POOR", "#E53935", "S.M.A.R.T. predicts imminent failure — back up your data now.");

        if (smartErrors > 0)
            return (70, "FAIR", "#FB8C00", $"{smartErrors} S.M.A.R.T. error events detected. Monitor closely.");

        return (100, "EXCELLENT", "#43A047", "S.M.A.R.T. reports healthy. No predicted failures or recorded errors.");
    }

    private static bool ReadSmartStatus(string deviceId, out bool smartOk, out bool failPredict, out long errorEvents)
    {
        smartOk = false;
        failPredict = false;
        errorEvents = 0;
        try
        {
            var normDevice = Regex.Match(deviceId, @"\\(?<id>\d+)$");
            if (!normDevice.Success) return false;
            var instance = $@"\\.\PHYSICALDRIVE{normDevice.Groups["id"].Value}";
            var instanceName = "\\\\.\\" + instance.TrimStart('\\') + ":";
            smartOk = true;

            using var rel = new ManagementObjectSearcher(
                "SELECT InstanceName, FailurePredictStatus, ReadErrorsTotal, WriteErrorsTotal FROM MSStorageDriver_FailurePredictStatus");
            foreach (ManagementBaseObject o in rel.Get())
            {
                var inst = o["InstanceName"]?.ToString() ?? "";
                if (!inst.Equals(instanceName, StringComparison.OrdinalIgnoreCase) &&
                    !inst.Equals(instance, StringComparison.OrdinalIgnoreCase)) continue;

                failPredict = (o["FailurePredictStatus"] as bool?) ?? false;
                var read = (o["ReadErrorsTotal"] as uint?) ?? 0;
                var write = (o["WriteErrorsTotal"] as uint?) ?? 0;
                errorEvents = (long)read + write;
                break;
            }
        }
        catch
        {
            smartOk = false;
        }
        return true;
    }

    private static List<HealthStat> BuildStats(
        ManagementBaseObject o, string deviceId, string status, string serial, string mediaType,
        string firmware, ulong sizeBytes, List<string> drives, (int Score, string Label, string Brush, string Description) health)
    {
        var stats = new List<HealthStat>
        {
            new("Health Score", $"{health.Score}/100 — {health.Label}"),
            new("Status", status),
            new("Size", FormatSize(sizeBytes)),
            new("Media Type", string.IsNullOrWhiteSpace(mediaType) ? "Unknown" : mediaType),
            new("Interface", o["InterfaceType"]?.ToString() ?? "Unknown"),
            new("Serial Number", string.IsNullOrWhiteSpace(serial) ? "Not reported" : serial),
            new("Firmware", string.IsNullOrWhiteSpace(firmware) ? "Not reported" : firmware),
            new("Partitions", o["Partitions"]?.ToString() ?? "—"),
        };

        if (drives.Count > 0)
            stats.Add(new HealthStat("Mounted Volumes", string.Join(", ", drives)));

        return stats;
    }

    private static List<string> MapToDriveLetters(string deviceId)
    {
        var letters = new List<string>();
        try
        {
            var diskIndex = Regex.Match(deviceId, @"\\(?<id>\d+)$");
            if (!diskIndex.Success) return letters;
            var diskNum = diskIndex.Groups["id"].Value;

            var partToDisk = new Dictionary<string, string>();
            using (var linkSearcher = new ManagementObjectSearcher("SELECT Antecedent, Dependent FROM Win32_DiskDriveToDiskPartition"))
            {
                foreach (ManagementBaseObject o in linkSearcher.Get())
                {
                    var diskId = ExtractKey(o["Antecedent"]?.ToString(), "DeviceID");
                    var partId = ExtractKey(o["Dependent"]?.ToString(), "DeviceID");
                    if (diskId != null && partId != null && diskId.EndsWith(diskNum, StringComparison.Ordinal))
                        partToDisk[partId] = diskId;
                }
            }

            using var logToPart = new ManagementObjectSearcher("SELECT Antecedent, Dependent FROM Win32_LogicalDiskToPartition");
            foreach (ManagementBaseObject o in logToPart.Get())
            {
                var partId = ExtractKey(o["Antecedent"]?.ToString(), "DeviceID");
                var logicalId = ExtractKey(o["Dependent"]?.ToString(), "DeviceID");
                if (partId != null && logicalId != null && partToDisk.ContainsKey(partId))
                    letters.Add(logicalId);
            }
        }
        catch
        {
            Logger.Warn($"WMI volume mapping failed for {deviceId}");
        }
        return letters;
    }

    private void SelectDevice(object sender, MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is UsbDevice device)
            Select(device);
    }

    private void Select(UsbDevice device)
    {
        _selected = device;
        DetailName.Text = device.Name;
        DetailSubtitle.Text = device.DeviceId;

        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(device.HealthBrush));

        OverallScore.Text = $"{device.HealthScore}";
        OverallScore.Foreground = brush;
        OverallLabel.Text = device.HealthLabel;
        OverallLabel.Foreground = brush;
        OverallDesc.Text = device.HealthDesc;
        OverallDesc.Foreground = FindBrush("BrushTextMuted");

        StatList.ItemsSource = device.Stats;
    }

    private void ClearDetail()
    {
        _selected = null;
        DetailName.Text = "Select a device";
        DetailSubtitle.Text = "";
        OverallScore.Text = "—";
        OverallScore.Foreground = FindBrush("BrushTextMuted");
        OverallLabel.Text = "No data";
        OverallLabel.Foreground = FindBrush("BrushTextPrimary");
        OverallDesc.Text = "Connect a USB storage device to see diagnostics.";
        OverallDesc.Foreground = FindBrush("BrushTextMuted");
        StatList.ItemsSource = null;
    }

    private static Brush FindBrush(string key)
        => (Application.Current.TryFindResource(key) as Brush) ?? Brushes.Gray;

    private static ulong ParseUlong(object? value)
        => ulong.TryParse(value?.ToString(), out var v) ? v : 0;

    private static string FormatSize(ulong bytes)
    {
        if (bytes == 0) return "Unknown";
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double size = bytes;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }
        return $"{size:0.##} {units[unit]}";
    }

    private static string? ExtractKey(string? path, string key)
    {
        if (path == null) return null;
        var match = Regex.Match(path, key + "=\"([^\"]*)\"");
        return match.Success ? match.Groups[1].Value : null;
    }

    public sealed class UsbDevice
    {
        public string DeviceId { get; set; } = "";
        public string Name { get; set; } = "";
        public string Detail { get; set; } = "";
        public string Serial { get; set; } = "";
        public string MediaType { get; set; } = "";
        public string Firmware { get; set; } = "";
        public string Interface { get; set; } = "";
        public string Partitions { get; set; } = "";
        public ulong SizeBytes { get; set; }
        public string Status { get; set; } = "";
        public bool SmartOk { get; set; }
        public bool SmartFailPredict { get; set; }
        public long SmartErrors { get; set; }
        public List<string> Drives { get; set; } = new();
        public int HealthScore { get; set; }
        public string HealthLabel { get; set; } = "";
        public string HealthBrush { get; set; } = "#43A047";
        public string HealthDesc { get; set; } = "";
        public List<HealthStat> Stats { get; set; } = new();
    }

    public sealed record HealthStat(string Label, string Value);
}
