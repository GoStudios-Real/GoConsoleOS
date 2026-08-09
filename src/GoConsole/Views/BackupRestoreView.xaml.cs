using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using GoConsoleOS.Shared;

namespace GoConsoleOS.GoConsole.Views;

public partial class BackupRestoreView : UserControl
{
    private string _backupDir = "";
    private bool _isBackingUp;

    private static readonly string[] SaveFolderNames =
        { "save", "saves", "savegame", "savegames", "userdata", "profiles", "config", "data" };

    public BackupRestoreView()
    {
        InitializeComponent();
        _backupDir = Path.Combine(ConfigReader.RootPath ?? "", "system", "backups");
        UpdateStats();
        LoadBackups();
        LoadSaveSync();
    }

    private static Dictionary<string, InstalledRecord> LoadInstalledStoreItems()
    {
        var path = Path.Combine(ConfigReader.RootPath ?? "", "system", "store", "installed.json");
        if (!File.Exists(path)) return new Dictionary<string, InstalledRecord>();
        try
        {
            return System.Text.Json.JsonSerializer
                .Deserialize<Dictionary<string, InstalledRecord>>(File.ReadAllText(path))
                ?? new Dictionary<string, InstalledRecord>();
        }
        catch { return new Dictionary<string, InstalledRecord>(); }
    }

    private void LoadSaveSync()
    {
        var root = ConfigReader.RootPath ?? "";
        var items = new List<SaveSyncItem>();
        foreach (var (id, record) in LoadInstalledStoreItems())
        {
            if (string.IsNullOrEmpty(record.Dir) || !Directory.Exists(record.Dir)) continue;
            var saveDirs = Directory.GetDirectories(record.Dir)
                .Where(d => SaveFolderNames.Contains(Path.GetFileName(d).ToLowerInvariant())).ToList();
            var backupDir = Path.Combine(root, "system", "saves", id);
            var hasBackup = Directory.Exists(backupDir) &&
                            Directory.EnumerateFileSystemEntries(backupDir, "*", SearchOption.AllDirectories).Any();
            items.Add(new SaveSyncItem
            {
                Id = id,
                Name = id,
                Detail = saveDirs.Count > 0
                    ? $"Detected {saveDirs.Count} save folder{(saveDirs.Count == 1 ? "" : "s")} {(hasBackup ? "• backed up" : "• not backed up")}"
                    : "No save folders detected",
                HasSaveFolders = saveDirs.Count > 0,
                HasBackup = hasBackup
            });
        }
        if (items.Count == 0)
            items.Add(new SaveSyncItem
            {
                Name = "No store items installed",
                Detail = "Install games or apps from the GoStudios Store to manage their saves",
                HasSaveFolders = false,
                HasBackup = false
            });
        SaveList.ItemsSource = items;
    }

    private void BackupSaves_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string id)
            BackupSavesFor(id);
    }

    private void BackupSavesFor(string id)
    {
        try
        {
            var root = ConfigReader.RootPath ?? "";
            var installed = LoadInstalledStoreItems();
            if (!installed.TryGetValue(id, out var record) || string.IsNullOrEmpty(record.Dir) || !Directory.Exists(record.Dir))
            {
                ToastManager.Show("No install directory found for " + id);
                return;
            }

            var saveDirs = Directory.GetDirectories(record.Dir)
                .Where(d => SaveFolderNames.Contains(Path.GetFileName(d).ToLowerInvariant())).ToList();
            if (saveDirs.Count == 0)
            {
                ToastManager.Show("No save folders detected for " + id);
                return;
            }

            var dst = Path.Combine(root, "system", "saves", id);
            if (Directory.Exists(dst)) Directory.Delete(dst, true);
            Directory.CreateDirectory(dst);
            foreach (var d in saveDirs)
                CopyDirectory(d, Path.Combine(dst, Path.GetFileName(d)));

            SoundManager.Play("success");
            ToastManager.Show($"Saves backed up for {id}");
            Logger.Info($"Game saves backed up: {id}");
            LoadSaveSync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Save backup failed: {ex.Message}", "Game Saves", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RestoreSaves_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string id)
            RestoreSavesFor(id);
    }

    private void RestoreSavesFor(string id)
    {
        try
        {
            var root = ConfigReader.RootPath ?? "";
            var backupDir = Path.Combine(root, "system", "saves", id);
            if (!Directory.Exists(backupDir) ||
                !Directory.EnumerateFileSystemEntries(backupDir, "*", SearchOption.AllDirectories).Any())
            {
                MessageBox.Show("No save backup found for this item.", "Game Saves",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Restore saved data for '{id}'?\n\nThis overwrites its current save folders.",
                "Restore Saves", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            var installed = LoadInstalledStoreItems();
            if (!installed.TryGetValue(id, out var record) || string.IsNullOrEmpty(record.Dir) || !Directory.Exists(record.Dir))
            {
                ToastManager.Show("No install directory found for " + id);
                return;
            }

            foreach (var d in Directory.GetDirectories(backupDir))
            {
                var target = Path.Combine(record.Dir, Path.GetFileName(d));
                if (Directory.Exists(target)) Directory.Delete(target, true);
                CopyDirectory(d, target);
            }

            SoundManager.Play("success");
            ToastManager.Show($"Saves restored for {id}");
            Logger.Info($"Game saves restored: {id}");
            LoadSaveSync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Save restore failed: {ex.Message}", "Game Saves", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static void CopyDirectory(string source, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(dest, Path.GetFileName(file)), true);
        foreach (var dir in Directory.GetDirectories(source))
            CopyDirectory(dir, Path.Combine(dest, Path.GetFileName(dir)));
    }


    private void UpdateStats()
    {
        var root = ConfigReader.RootPath ?? "";
        long total = 0;
        foreach (var dir in new[] { "profiles", "launcher\\library", "system\\screenshots", "system\\wishlist.json", "system\\settings.json" })
        {
            var path = Path.Combine(root, dir);
            if (Directory.Exists(path))
                total += GetDirSize(path);
            else if (File.Exists(path))
                total += new FileInfo(path).Length;
        }
        BackupSizeText.Text = total > 0 ? $"{FormatSize(total)} of data" : "No data yet";

        var last = Directory.Exists(_backupDir) ? Directory.GetFiles(_backupDir, "*.zip").OrderByDescending(f => File.GetLastWriteTime(f)).FirstOrDefault() : null;
        LastBackupText.Text = last != null ? $"Last: {File.GetLastWriteTime(last):MMM dd, yyyy HH:mm}" : "Never backed up";
    }

    private void LoadBackups()
    {
        var items = new List<BackupItem>();
        if (Directory.Exists(_backupDir))
        {
            foreach (var file in Directory.GetFiles(_backupDir, "*.zip").OrderByDescending(f => File.GetLastWriteTime(f)))
            {
                items.Add(new BackupItem
                {
                    Name = Path.GetFileNameWithoutExtension(file),
                    Detail = $"{File.GetLastWriteTime(file):MMM dd, yyyy HH:mm} \u2022 {FormatSize(new FileInfo(file).Length)}",
                    Path = file
                });
            }
        }
        if (items.Count == 0)
            items.Add(new BackupItem { Name = "No backups found", Detail = "Create a backup to see it here", Path = "" });
        BackupList.ItemsSource = items;
    }

    private async void CreateBackup(object sender, RoutedEventArgs e)
    {
        if (_isBackingUp) return;
        _isBackingUp = true;
        BackupBtn.IsEnabled = false;
        BackupProgress.Visibility = Visibility.Visible;
        BackupLog.Text = "Collecting console data...";

        try
        {
            Directory.CreateDirectory(_backupDir);
            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var zipPath = Path.Combine(_backupDir, $"goconsole_backup_{stamp}.zip");
            var root = ConfigReader.RootPath ?? "";

            await Task.Delay(300);
            BackupProgress.Value = 25;
            BackupLog.Text = "Compressing profiles and saves...";
            await Task.Delay(300);

            using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                var folders = new[] { "profiles", "launcher\\library", "system\\screenshots", "system\\themes" };
                foreach (var folder in folders)
                {
                    var path = Path.Combine(root, folder);
                    if (!Directory.Exists(path)) continue;
                    foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                    {
                        var rel = Path.GetRelativePath(root, file);
                        zip.CreateEntryFromFile(file, rel);
                    }
                }

                var files = new[] { "system\\wishlist.json", "system\\settings.json", "system\\.setup_complete" };
                foreach (var f in files)
                {
                    var path = Path.Combine(root, f);
                    if (File.Exists(path))
                        zip.CreateEntryFromFile(path, f);
                }
            }

            BackupProgress.Value = 100;
            BackupLog.Text = $"Backup created: {Path.GetFileName(zipPath)}";
            ToastManager.Show("Backup created successfully");
            UpdateStats();
            LoadBackups();
        }
        catch (Exception ex)
        {
            BackupLog.Text = $"Backup failed: {ex.Message}";
        }
        finally
        {
            _isBackingUp = false;
            BackupBtn.IsEnabled = true;
        }
    }

    private void RestoreBackup(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string path && File.Exists(path))
        {
            var result = MessageBox.Show(
                $"Restore backup '{Path.GetFileNameWithoutExtension(path)}'?\n\nThis will overwrite current profiles, saves, and settings.",
                "Restore Backup", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var root = ConfigReader.RootPath ?? "";
                using var zip = ZipFile.OpenRead(path);
                foreach (var entry in zip.Entries)
                {
                    var dest = Path.Combine(root, entry.FullName);
                    var dir = Path.GetDirectoryName(dest);
                    if (dir != null) Directory.CreateDirectory(dir);
                    if (entry.Length > 0)
                        entry.ExtractToFile(dest, overwrite: true);
                }
                ToastManager.Show("Backup restored successfully");
                BackupStatus.Text = "Backup restored. Some settings apply after restart.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Restore failed: {ex.Message}", "Restore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private static long GetDirSize(string path)
    {
        long size = 0;
        try
        {
            foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                size += new FileInfo(file).Length;
        }
        catch { }
        return size;
    }

    private static string FormatSize(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB" };
        double value = bytes;
        int unit = 0;
        while (value >= 1024 && unit < units.Length - 1) { value /= 1024; unit++; }
        return $"{value:0.#} {units[unit]}";
    }

    public class BackupItem
    {
        public string Name { get; set; } = "";
        public string Detail { get; set; } = "";
        public string Path { get; set; } = "";
    }

    public class SaveSyncItem
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Detail { get; set; } = "";
        public bool HasSaveFolders { get; set; }
        public bool HasBackup { get; set; }
    }
}
