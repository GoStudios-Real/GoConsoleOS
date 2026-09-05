using System.Runtime.InteropServices;
using System.Text;

namespace GoConsoleOS.Shared;

public static class UsbInstaller
{
    public const long MinFreeBytes = 512L * 1024 * 1024;

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    private static extern bool SetVolumeLabel(string rootPathName, string volumeName);

    public static void RunInstall(string source, string target, Action<string, int> report)
    {
        report("Preparing target drive...", 2);
        CleanTarget(target);

        report("Renaming volume to GoConsoleOS...", 4);
        try
        {
            if (!SetVolumeLabel(target, "GoConsoleOS"))
                Logger.Warn($"SetVolumeLabel returned false for {target} (may need the drive label set manually)");
        }
        catch (Exception ex)
        {
            Logger.Warn($"Failed to rename volume {target}: {ex.Message}");
        }

        report("Copying GoConsoleOS files...", 6);
        CopyDirectory(source, target, report);

        report("Configuring launcher...", 88);
        ConfigureLauncher(target);

        report("Writing boot config...", 92);
        WriteInitConfig(target);

        report("Creating autorun...", 95);
        WriteAutorun(target);

        report("Installing USB auto-launch watcher...", 96);
        InstallWatcher(source, target);

        report("Verifying installation...", 97);
        Verify(target);

        Logger.Info($"USB install finished on {target}");
        report("Ready! Unplug the drive and plug it into any Windows PC to launch GoConsoleOS.", 100);
    }

    public static void CleanTarget(string target)
    {
        var failures = 0;

        foreach (var dir in Directory.GetDirectories(target))
        {
            if (dir.EndsWith("System Volume Information", StringComparison.OrdinalIgnoreCase)) continue;
            try { Directory.Delete(dir, true); }
            catch (Exception ex) { failures++; Logger.Warn($"Could not remove {dir}: {ex.Message}"); }
        }

        foreach (var file in Directory.GetFiles(target))
        {
            try { File.Delete(file); }
            catch (Exception ex) { failures++; Logger.Warn($"Could not remove {file}: {ex.Message}"); }
        }

        if (failures > 0)
            Logger.Warn($"Cleaned target with {failures} locked item(s) skipped");
    }

    public static void CopyDirectory(string source, string target, Action<string, int>? report = null)
    {
        var files = Directory.GetFiles(source, "*", SearchOption.AllDirectories);
        var totalBytes = files.Sum(f => new FileInfo(f).Length);
        long done = 0;

        foreach (var file in files)
        {
            var rel = Path.GetRelativePath(source, file);
            var dest = Path.Combine(target, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(file, dest, true);
            done += new FileInfo(file).Length;
            report?.Invoke($"Copying files... {FormatSize(done)} / {FormatSize(totalBytes)}", 6 + (int)(done * 80.0 / Math.Max(1, totalBytes)));
        }
    }

    public static void ConfigureLauncher(string target)
    {
        var launcher = Path.Combine(target, "GoConsole.exe");
        var launcherTarget = Path.Combine(target, "GoConsoleOS.exe");

        if (File.Exists(launcherTarget)) File.Delete(launcherTarget);
        if (File.Exists(launcher))
        {
            File.Move(launcher, launcherTarget);
        }
        else
        {
            Logger.Warn("GoConsole.exe not found in deployment copy; launcher was not renamed");
        }
    }

    public static void WriteInitConfig(string target)
    {
        var version = new Models.InitConfig().General.Version;
        var bootDir = Path.Combine(target, "boot");
        Directory.CreateDirectory(bootDir);
        var path = Path.Combine(bootDir, "init.cfg");

        var cfg = new StringBuilder();
        cfg.AppendLine("[general]");
        cfg.AppendLine("os_name=GoConsoleOS");
        cfg.AppendLine($"version=v{version}");
        cfg.AppendLine("splash_duration_ms=3500");
        cfg.AppendLine("auto_detect_drive=true");
        cfg.AppendLine();
        cfg.AppendLine("[boot]");
        cfg.AppendLine("boot_mode=shell");
        cfg.AppendLine("auto_launch_shell=true");
        cfg.AppendLine("shell_launch_delay_ms=0");
        cfg.AppendLine("min_ram_mb=2048");
        cfg.AppendLine("min_usb_space_mb=512");
        cfg.AppendLine();
        cfg.AppendLine("[display]");
        cfg.AppendLine("resolution=auto");
        cfg.AppendLine("fullscreen=true");
        cfg.AppendLine("refresh_rate=auto");
        cfg.AppendLine("hardware_acceleration=true");
        cfg.AppendLine("ui_scale=1.0");
        cfg.AppendLine();
        cfg.AppendLine("[services]");
        cfg.AppendLine("enabled=true");
        cfg.AppendLine("controller_poll_rate=120");
        cfg.AppendLine("xinput_enabled=true");
        cfg.AppendLine("perf_monitor_interval_ms=2000");
        cfg.AppendLine();
        cfg.AppendLine("[overlay]");
        cfg.AppendLine("enabled=true");
        cfg.AppendLine("guide_combo=guide_button");
        cfg.AppendLine("show_fps=false");
        cfg.AppendLine("show_system_stats=true");
        cfg.AppendLine("overlay_opacity=0.9");
        cfg.AppendLine();
        cfg.AppendLine("[network]");
        cfg.AppendLine("enable_networking=true");
        cfg.AppendLine("check_updates=false");
        cfg.AppendLine("cloud_server_url=https://gostudios.net/api");
        cfg.AppendLine("server_port=39210");
        cfg.AppendLine();
        cfg.AppendLine("[logging]");
        cfg.AppendLine("log_level=info");
        cfg.AppendLine("max_log_size_mb=10");
        cfg.AppendLine("log_retention_days=30");

        File.WriteAllText(path, cfg.ToString(), new UTF8Encoding(false));
    }

    public static void WriteAutorun(string target)
    {
        var path = Path.Combine(target, "autorun.inf");
        var content = new StringBuilder();
        content.AppendLine("[autorun]");
        content.AppendLine("open=GoConsoleOS.exe");
        content.AppendLine("action=Launch GoConsoleOS Gaming Console");
        content.AppendLine("icon=GoConsoleOS.exe,0");
        content.AppendLine("shell\\open=Launch GoConsoleOS");
        content.AppendLine("shell\\open\\command=GoConsoleOS.exe");

        File.WriteAllText(path, content.ToString(), new UTF8Encoding(false));
        File.SetAttributes(path, FileAttributes.Hidden | FileAttributes.System);
    }

    public static bool Verify(string target)
    {
        return File.Exists(Path.Combine(target, "GoConsoleOS.exe"))
            && File.Exists(Path.Combine(target, "boot", "init.cfg"))
            && File.Exists(Path.Combine(target, "autorun.inf"));
    }

    public static void InstallWatcher(string source, string target)
    {
        var watcherFiles = new[] { "GoConsoleWatcher.exe", "GoConsoleWatcher.dll" };
        foreach (var file in watcherFiles)
        {
            var src = Path.Combine(source, file);
            var dst = Path.Combine(target, file);
            if (File.Exists(src))
            {
                try { File.Copy(src, dst, true); }
                catch (Exception ex) { Logger.Warn($"Failed to copy watcher {file}: {ex.Message}"); }
            }
        }

        var bootDir = Path.Combine(target, "boot");
        Directory.CreateDirectory(bootDir);
        var watcherCfg = Path.Combine(bootDir, "watcher.cfg");
        File.WriteAllText(watcherCfg, "enabled=true\ndelay_ms=2000\n", new UTF8Encoding(false));
    }

    public static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes}B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024}KB";
        if (bytes < 1024L * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F1}MB";
        return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2}GB";
    }
}
