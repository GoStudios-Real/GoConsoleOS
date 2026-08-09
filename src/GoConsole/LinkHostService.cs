using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Windows;
using GoConsoleOS.Shared;

namespace GoConsoleOS.GoConsole;

/// <summary>
/// Hosts the Android companion transport inside the GoConsoleOS shell.
/// Answers discovery beacons, serves the game catalogue, streams USB health and
/// accepts Cast frames — without changing the shell's normal behavior.
/// </summary>
public sealed class LinkHostService
{
    private readonly LibraryScanner _scanner;
    private readonly Func<string, bool> _launchGame;
    private LinkServer? _server;

    public LinkHostService(LibraryScanner scanner, Func<string, bool> launchGame)
    {
        _scanner = scanner;
        _launchGame = launchGame;
    }

    public void Start()
    {
        if (_server != null) return;
        try
        {
            _server = new LinkServer(
                gamesProvider: ListGames,
                usbProvider: ListUsbHealth,
                launchAction: title => RunOnUiThread(() => _launchGame(title)),
                openInstallerAction: OpenUsbInstaller,
                castFrame: _ => { /* cast sink wired separately if a TV is attached */ });
            _server.Start();
        }
        catch (Exception ex)
        {
            Logger.Warn($"LinkHost start failed: {ex.Message}");
        }
    }

    public void Stop() => _server?.Dispose();

    private IEnumerable<string> ListGames()
    {
        try
        {
            return _scanner.LoadLibrary().Games
                .Select(g => g.Title)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct()
                .OrderBy(t => t);
        }
        catch
        {
            return new List<string>();
        }
    }

    private IEnumerable<UsbHealthRecord> ListUsbHealth()
    {
        var items = new List<UsbHealthRecord>();
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT DeviceID, Model, Status, InterfaceType, MediaType, Size, SerialNumber, FirmwareRevision FROM Win32_DiskDrive");
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
                var sizeBytes = ParseUlong(o["Size"]);

                var health = Healthy(status);
                items.Add(new UsbHealthRecord
                {
                    Id = o["DeviceID"]?.ToString() ?? "",
                    Label = model,
                    Vendor = "USB",
                    Product = model,
                    Serial = o["SerialNumber"]?.ToString()?.Trim() ?? "",
                    Health = health.Label,
                    HealthScore = health.Score,
                    Total = (long)sizeBytes,
                    Interface = iface,
                    Issue = health.Issue,
                    Mounted = status.Equals("OK", StringComparison.OrdinalIgnoreCase),
                });
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"LinkServer usb.list: {ex.Message}");
        }
        return items;
    }

    private static (string Label, int Score, string Issue) Healthy(string status)
    {
        if (!status.Equals("OK", StringComparison.OrdinalIgnoreCase))
            return ("poor", 15, $"Status: {status}");
        return ("ok", 100, "");
    }

    private static ulong ParseUlong(object? value)
    {
        try { return value != null && ulong.TryParse(value.ToString(), out var v) ? v : 0; }
        catch { return 0; }
    }

    private void OpenUsbInstaller()
    {
        try
        {
            var exe = Path.Combine(ConfigReader.RootPath ?? Directory.GetCurrentDirectory(), "GoUsbMaker.exe");
            if (File.Exists(exe))
            {
                Logger.Info("LinkServer: opening GoUsbMaker.exe via Android companion");
                Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true });
            }
            else
            {
                Logger.Warn("LinkServer: GoUsbMaker.exe not found");
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"LinkServer installer: {ex.Message}");
        }
    }

    private static void RunOnUiThread(Action action)
    {
        try
        {
            var app = System.Windows.Application.Current;
            if (app?.Dispatcher != null && !app.Dispatcher.CheckAccess())
                app.Dispatcher.Invoke(action);
            else
                action();
        }
        catch (Exception ex)
        {
            Logger.Warn($"LinkServer UI action: {ex.Message}");
        }
    }
}