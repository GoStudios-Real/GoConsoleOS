using System.Management;
using System.Text.RegularExpressions;

namespace GoConsoleOS.Shared;

public enum DriveKind
{
    Usb,
    Removable,
    Internal
}

public static class DriveClassifier
{
    public static Dictionary<string, string> GetLogicalDiskToBusType()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var diskTypes = new Dictionary<string, string>();
            var partitionToDisk = new Dictionary<string, string>();

            using (var diskSearcher = new ManagementObjectSearcher("SELECT DeviceID, InterfaceType FROM Win32_DiskDrive"))
            {
                foreach (ManagementBaseObject o in diskSearcher.Get())
                {
                    var id = o["DeviceID"]?.ToString();
                    var type = o["InterfaceType"]?.ToString() ?? "";
                    if (id != null) diskTypes[id] = type;
                }
            }

            using (var linkSearcher = new ManagementObjectSearcher("SELECT Antecedent, Dependent FROM Win32_DiskDriveToDiskPartition"))
            {
                foreach (ManagementBaseObject o in linkSearcher.Get())
                {
                    var diskId = ExtractKey(o["Antecedent"]?.ToString(), "DeviceID");
                    var partId = ExtractKey(o["Dependent"]?.ToString(), "DeviceID");
                    if (diskId != null && partId != null)
                        partitionToDisk[partId] = diskId;
                }
            }

            using (var diskSearcher = new ManagementObjectSearcher("SELECT Antecedent, Dependent FROM Win32_LogicalDiskToPartition"))
            {
                foreach (ManagementBaseObject o in diskSearcher.Get())
                {
                    var partId = ExtractKey(o["Antecedent"]?.ToString(), "DeviceID");
                    var logicalId = ExtractKey(o["Dependent"]?.ToString(), "DeviceID");
                    if (partId != null && logicalId != null && partitionToDisk.TryGetValue(partId, out var diskId))
                        result[logicalId] = diskTypes.TryGetValue(diskId, out var type) ? type : "";
                }
            }
        }
        catch
        {
            Logger.Warn("WMI drive classification unavailable");
        }

        return result;
    }

    public static DriveKind Classify(DriveInfo drive, Dictionary<string, string> busTypes)
    {
        var key = drive.RootDirectory.FullName.TrimEnd('\\');
        busTypes.TryGetValue(key, out var iface);

        if (drive.DriveType == DriveType.Removable)
            return string.Equals(iface, "USB", StringComparison.OrdinalIgnoreCase) ? DriveKind.Usb : DriveKind.Removable;

        if (string.Equals(iface, "USB", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(iface, "IEEE 1394", StringComparison.OrdinalIgnoreCase))
            return DriveKind.Usb;

        if (busTypes.Count == 0)
            return DriveKind.Removable;

        return DriveKind.Internal;
    }

    public static string KindLabel(DriveKind kind)
    {
        return kind switch
        {
            DriveKind.Usb => "USB DRIVE",
            DriveKind.Removable => "REMOVABLE / SD",
            _ => "INTERNAL DISK"
        };
    }

    public static string KindBrush(DriveKind kind)
    {
        return kind switch
        {
            DriveKind.Usb => "#0066FF",
            DriveKind.Removable => "#43A047",
            _ => "#FB8C00"
        };
    }

    private static string? ExtractKey(string? path, string key)
    {
        if (path == null) return null;
        var match = Regex.Match(path, key + "=\"([^\"]*)\"");
        return match.Success ? match.Groups[1].Value : null;
    }
}
