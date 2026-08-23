using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using GoConsoleOS.Shared;
using GoConsoleOS.Shared.Input;
using GoConsoleOS.Shared.Models;

namespace GoConsoleOS.GoConsole.Views;

public partial class StoreView : UserControl
{
    private readonly string _rootPath;
    private StoreCatalog _catalog = new();
    private static readonly string InstalledDbPath = "system\\store\\installed.json";
    private static readonly string WishlistDbPath = "plugins\\store\\wishlist.json";
    private Dictionary<string, InstalledRecord> _installed = new();
    private HashSet<string> _wishlist = new();
    private bool _showWishlistOnly;

    private readonly List<CatalogItem> _navItems = new();
    private int _navIdx;
    private readonly Dictionary<string, Border> _cardBorders = new();
    private DispatcherTimer? _holdTimer;
    private ControllerEngine? _controller;

    public StoreView(string rootPath)
    {
        InitializeComponent();
        _rootPath = rootPath;
        LoadCatalog();
        ConnectController();
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        _holdTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
        _holdTimer.Tick += (_, _) =>
        {
            if (_controller == null || !_controller.IsConnected) { _holdTimer.Stop(); return; }
            var buttons = _controller.CurrentState.Buttons;
            if ((buttons & (ushort)ControllerButtons.DPadDown) != 0) { MoveSelection(1); return; }
            if ((buttons & (ushort)ControllerButtons.DPadUp) != 0) { MoveSelection(-1); return; }
            _holdTimer.Stop();
        };
        UpdateInstallButtons();
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        _holdTimer?.Stop();
        DisconnectController();
        try { PreviewWeb.Dispose(); } catch { }
        WebView2Support.KillOwnProcesses();
    }

    private void ConnectController()
    {
        _controller = (Application.Current.MainWindow as MainWindow)?.Controller;
        if (_controller == null) return;
        _controller.ButtonPressed += OnControllerButton;
        _controller.StateUpdated += OnControllerState;
    }

    private void DisconnectController()
    {
        if (_controller == null) return;
        _controller.ButtonPressed -= OnControllerButton;
        _controller.StateUpdated -= OnControllerState;
        _controller = null;
    }

    private void OnControllerState(ControllerState state)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (_controller == null || !_controller.IsConnected) return;
            if (PreviewOverlay.Visibility == Visibility.Visible) return;
            var x = state.ThumbLX;
            var y = state.ThumbLY;
            if (Math.Abs(x) < 15000 && Math.Abs(y) < 15000) return;
            if (Math.Abs(x) > Math.Abs(y))
                MoveSelection(x > 0 ? 1 : -1);
            else
                MoveSelection(y > 0 ? -4 : 4);
        }));
    }

    private void OnControllerButton(ControllerButtons button)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (PreviewOverlay.Visibility == Visibility.Visible)
            {
                if (button == ControllerButtons.B || button == ControllerButtons.Guide)
                    ClosePreview();
                return;
            }
            switch (button)
            {
                case ControllerButtons.DPadUp: MoveSelection(-4); _holdTimer?.Start(); break;
                case ControllerButtons.DPadDown: MoveSelection(4); _holdTimer?.Start(); break;
                case ControllerButtons.DPadLeft: MoveSelection(-1); break;
                case ControllerButtons.DPadRight: MoveSelection(1); break;
                case ControllerButtons.A:
                    if (_navItems.Count > 0)
                        ActivateItem(_navItems[Math.Clamp(_navIdx, 0, _navItems.Count - 1)]);
                    break;
                case ControllerButtons.X:
                    if (_navItems.Count > 0)
                        ToggleWishlistFor(_navItems[Math.Clamp(_navIdx, 0, _navItems.Count - 1)]);
                    break;
                case ControllerButtons.Y:
                    if (_navItems.Count > 0)
                        OpenPreview(_navItems[Math.Clamp(_navIdx, 0, _navItems.Count - 1)]);
                    break;
                case ControllerButtons.B:
                    (Application.Current.MainWindow as MainWindow)?.NavigateTo("home");
                    break;
            }
        }));
    }

    private void LoadCatalog()
    {
        try
        {
            var catalogPath = Path.Combine(_rootPath, "plugins", "store", "catalog.json");
            if (!File.Exists(catalogPath))
            {
                CatalogStatus.Text = "Store catalog not found. Check your USB installation.";
                return;
            }

            var json = File.ReadAllText(catalogPath);
            _catalog = JsonSerializer.Deserialize<StoreCatalog>(json) ?? new();

            if (_catalog.Items.Count == 0)
            {
                CatalogStatus.Text = "Catalog is empty. No items available.";
                return;
            }

            _installed = LoadInstalledItems();
            _wishlist = LoadWishlist();
            foreach (var item in _catalog.Items)
            {
                RegisterBundled(item);
                item.IsInstalled = _installed.ContainsKey(item.Id);
            }

            CatalogStatus.Text = $"Browse {_catalog.Items.Count} free apps & games in the GoStudios Corporation Store";

            AppsList.ItemsSource = _catalog.Items.Where(i => i.Type == "app").ToList();
            GamesList.ItemsSource = _catalog.Items.Where(i => i.Type == "game").ToList();

            RebuildNavItems();
        }
        catch (Exception ex)
        {
            CatalogStatus.Text = $"Error loading catalog: {ex.Message}";
            Logger.Error($"Store catalog error: {ex.Message}");
        }
    }

    private void RebuildNavItems()
    {
        _navItems.Clear();
        foreach (var item in _catalog.Items)
            if (item.Type is "app" or "game" && (!_showWishlistOnly || _wishlist.Contains(item.Id)))
                _navItems.Add(item);
        _navIdx = 0;
        _cardBorders.Clear();
        UpdateSelectionVisual();
    }

    private void MoveSelection(int delta)
    {
        if (_navItems.Count == 0) return;
        var oldIdx = _navIdx;
        _navIdx = Math.Clamp(_navIdx + delta, 0, _navItems.Count - 1);
        if (_navIdx == oldIdx) return;
        UpdateSelectionVisual();
        SoundManager.Play("nav");
    }

    private void UpdateSelectionVisual()
    {
        var accent = TryFindResource("BrushAccentPrimary") as Brush ?? Brushes.Cyan;
        var selectedId = _navItems.Count > 0 ? _navItems[Math.Clamp(_navIdx, 0, _navItems.Count - 1)].Id : null;
        foreach (var kvp in _cardBorders)
        {
            var isSel = kvp.Key == selectedId;
            kvp.Value.BorderBrush = isSel ? accent : Brushes.Transparent;
            kvp.Value.BorderThickness = new Thickness(isSel ? 3 : 1);
        }

        if (selectedId == null) return;
        var row = Math.Clamp(_navIdx / 4 - 1, 0, 10);
        CatalogScroll.ScrollToVerticalOffset(row * 262.0);
    }

    private Border? FindCard(CatalogItem item)
    {
        if (_cardBorders.TryGetValue(item.Id, out var cached)) return cached;
        var control = item.Type == "app" ? AppsList : GamesList;
        var container = control.ItemContainerGenerator.ContainerFromItem(item);
        if (container is ContentPresenter cp)
        {
            var card = FindVisualChild<Border>(cp);
            if (card != null) _cardBorders[item.Id] = card;
            return card;
        }
        return null;
    }

    private void UpdateInstallButtons()
    {
        foreach (var item in _catalog.Items)
        {
            if (item.Type is not ("app" or "game")) continue;
            if (item.IsInstalled)
            {
                item.StatusText = _installed[item.Id].IsInstaller ? "RUN SETUP" : "RUN";
            }
            else
            {
                item.StatusText = string.IsNullOrEmpty(item.DownloadUrl) ? "UNAVAILABLE" : "INSTALL";
            }
            RefreshStatus(item);
        }
    }

    private void RegisterBundled(CatalogItem item)
    {
        if (string.IsNullOrWhiteSpace(item.BundledExe)) return;

        var resolved = Path.IsPathRooted(item.BundledExe)
            ? item.BundledExe
            : Path.Combine(_rootPath, item.BundledExe);
        if (!File.Exists(resolved))
        {
            if (string.Equals(item.DownloadUrl, "bundled", StringComparison.OrdinalIgnoreCase))
                item.DownloadUrl = "";
            return;
        }

        _installed[item.Id] = new InstalledRecord
        {
            Exe = resolved,
            Dir = Path.GetDirectoryName(resolved) ?? "",
            IsInstaller = false
        };

        if (string.IsNullOrEmpty(item.DownloadUrl))
            item.DownloadUrl = "bundled";
    }

    private Dictionary<string, InstalledRecord> LoadInstalledItems()
    {
        var path = Path.Combine(_rootPath, InstalledDbPath);
        if (!File.Exists(path)) return new Dictionary<string, InstalledRecord>();
        try
        {
            var text = File.ReadAllText(path);
            var dict = JsonSerializer.Deserialize<Dictionary<string, InstalledRecord>>(text);
            if (dict != null) return dict;
            var legacy = JsonSerializer.Deserialize<HashSet<string>>(text);
            if (legacy != null)
                return legacy.ToDictionary(id => id, _ => new InstalledRecord());
        }
        catch
        {
        }
        return new Dictionary<string, InstalledRecord>();
    }

    private void SaveInstalledItems()
    {
        var path = Path.Combine(_rootPath, InstalledDbPath);
        var dir = Path.GetDirectoryName(path);
        if (dir != null) Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(_installed, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    private void Card_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string itemId)
        {
            var item = _catalog.Items.FirstOrDefault(i => i.Id == itemId);
            if (item != null) OpenPreview(item);
        }
        e.Handled = true;
    }

    private void ActivateItem(CatalogItem item)
    {
        if (item.IsInstalled)
            LaunchItem(item);
        else if (string.Equals(item.DownloadUrl, "bundled", StringComparison.OrdinalIgnoreCase))
        {
            SoundManager.Play("error");
            ShowNotification($"{item.Name} is bundled with this console and already on this drive", 3);
        }
        else if (string.IsNullOrEmpty(item.DownloadUrl))
        {
            SoundManager.Play("error");
            ShowNotification($"{item.Name} is not available for download yet", 3);
        }
        else
        {
            _ = DownloadAndInstall(item);
        }
    }

    private void InstallItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string itemId)
        {
            var item = _catalog.Items.FirstOrDefault(i => i.Id == itemId);
            if (item != null) ActivateItem(item);
        }
        e.Handled = true;
    }

    private async Task DownloadAndInstall(CatalogItem item)
    {
        item.StatusText = "DOWNLOADING";
        RefreshStatus(item);

        var downloadsDir = Path.Combine(_rootPath, "system", "store", "downloads");
        var installDir = Path.Combine(_rootPath, "system", "store", "installed", item.Id);
        try
        {
            Directory.CreateDirectory(downloadsDir);
            var fileName = Path.GetFileName(new Uri(item.DownloadUrl).AbsolutePath);
            if (string.IsNullOrEmpty(fileName) || !fileName.Contains('.')) fileName = item.Id + ".bin";

            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
            using var resp = await client.GetAsync(item.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
            resp.EnsureSuccessStatusCode();
            var total = resp.Content.Headers.ContentLength ?? 0;

            var finalName = Path.GetFileName(resp.RequestMessage?.RequestUri?.AbsolutePath ?? "");
            if (string.IsNullOrEmpty(finalName) || !finalName.Contains('.'))
                finalName = fileName;
            var dlPath = Path.Combine(downloadsDir, item.Id + "_" + finalName);
            await using var stream = await resp.Content.ReadAsStreamAsync();
            await using var fs = File.Create(dlPath);
            var buf = new byte[81920];
            long received = 0;
            int n;
            while ((n = await stream.ReadAsync(buf)) > 0)
            {
                await fs.WriteAsync(buf.AsMemory(0, n));
                received += n;
                if (total > 0)
                    item.StatusText = $"{(int)(received * 100 / total)}%";
                else
                    item.StatusText = $"{received / 1024 / 1024} MB";
                RefreshStatus(item);
            }

            item.StatusText = "INSTALLING";
            RefreshStatus(item);

            Directory.CreateDirectory(installDir);
            if (Path.GetExtension(dlPath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                ZipFile.ExtractToDirectory(dlPath, installDir, true);
                File.Delete(dlPath);
            }
            else
            {
                var target = Path.Combine(installDir, finalName);
                if (File.Exists(target)) File.Delete(target);
                File.Move(dlPath, target);
            }

            var record = ResolveLaunchExe(item, installDir);
            _installed[item.Id] = record;
            item.IsInstalled = true;
            SaveInstalledItems();
            UpdateInstallButtons();
            RefreshStatus(item);

            SoundManager.Play("install");
            ShowNotification($"Installed {item.Name}", 3);
            Logger.Info($"Store installed: {item.Name} -> {installDir}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Store install failed for {item.Name}: {ex.Message}");
            item.StatusText = "RETRY";
            RefreshStatus(item);
            SoundManager.Play("error");
            ShowNotification($"Install failed: {ex.Message}", 4);
        }
    }

    private InstalledRecord ResolveLaunchExe(CatalogItem item, string installDir)
    {
        var isInstaller = !Path.GetExtension(item.DownloadUrl).Equals(".zip", StringComparison.OrdinalIgnoreCase);
        var record = new InstalledRecord { Dir = installDir, IsInstaller = isInstaller };

        if (isInstaller)
        {
            record.Exe = Directory.GetFiles(installDir, "*.exe").FirstOrDefault() ?? "";
            record.Exe = Directory.GetFiles(installDir, "*.msi").FirstOrDefault() ?? record.Exe;
            return record;
        }

        if (!string.IsNullOrEmpty(item.LaunchExe))
        {
            var exact = Directory.GetFiles(installDir, item.LaunchExe, SearchOption.AllDirectories)
                .FirstOrDefault();
            if (exact != null) { record.Exe = exact; return record; }
        }

        var exe = Directory.GetFiles(installDir, "*.exe", SearchOption.AllDirectories)
            .Where(f =>
            {
                var name = Path.GetFileNameWithoutExtension(f).ToLowerInvariant();
                return !name.Contains("uninstall") && !name.Contains("setup") && !name.Contains("crash");
            })
            .FirstOrDefault() ?? "";
        record.Exe = exe;
        return record;
    }

    private void LaunchItem(CatalogItem item)
    {
        if (!_installed.TryGetValue(item.Id, out var record)) return;
        var exe = record.Exe;
        if (string.IsNullOrEmpty(exe) || !File.Exists(exe))
        {
            SoundManager.Play("error");
            ShowNotification($"Executable for {item.Name} not found", 3);
            return;
        }
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                WorkingDirectory = Path.GetDirectoryName(exe) ?? "",
                UseShellExecute = true
            });
            SoundManager.Play("launch");
            ActivityFeed.AddCustom($"Launched {item.Name}", "");
            ShowNotification($"Launched {item.Name}", 2);
        }
        catch (Exception ex)
        {
            SoundManager.Play("error");
            ShowNotification($"Failed to launch: {ex.Message}", 3);
        }
    }

    private void UninstallItem(CatalogItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.BundledExe))
        {
            SoundManager.Play("error");
            ShowNotification($"{item.Name} is built into the system and cannot be uninstalled", 3);
            return;
        }

        try
        {
            if (_installed.TryGetValue(item.Id, out var record) && record.Dir != null &&
                Directory.Exists(record.Dir))
                Directory.Delete(record.Dir, true);
            _installed.Remove(item.Id);
            item.IsInstalled = false;
            SaveInstalledItems();
            UpdateInstallButtons();
            RefreshStatus(item);
            SoundManager.Play("uninstall");
            ShowNotification($"Uninstalled {item.Name}", 2);
        }
        catch (Exception ex)
        {
            SoundManager.Play("error");
            ShowNotification($"Uninstall failed: {ex.Message}", 3);
        }
    }

    private void RefreshStatus(CatalogItem item)
    {
        Dispatcher.Invoke(() =>
        {
            var control = item.Type == "app" ? AppsList : GamesList;
            var container = control.ItemContainerGenerator.ContainerFromItem(item);
            if (container is ContentPresenter cp)
            {
                foreach (var child in FindVisualChildren<TextBlock>(cp))
                {
                    if (child.Name == "InstallBtnText")
                        child.Text = item.StatusText;
                }
            }
        });
    }

    private static T? FindVisualChild<T>(DependencyObject root) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) return match;
            var sub = FindVisualChild<T>(child);
            if (sub != null) return sub;
        }
        return null;
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) yield return match;
            foreach (var sub in FindVisualChildren<T>(child))
                yield return sub;
        }
    }

    private void PreviewItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string itemId)
        {
            var item = _catalog.Items.FirstOrDefault(i => i.Id == itemId);
            if (item != null) OpenPreview(item);
        }
        e.Handled = true;
    }

    private async void OpenPreview(CatalogItem item)
    {
        if (string.IsNullOrEmpty(item.WebsiteUrl))
        {
            SoundManager.Play("error");
            ShowNotification($"{item.Name} has no web preview", 3);
            return;
        }
        SoundManager.Play("select");
        PreviewTitle.Text = $"{item.Name}  (v{item.Version})";
        PreviewUrl.Text = item.WebsiteUrl;
        PreviewOverlay.Visibility = Visibility.Visible;
        try
        {
            var env = await WebView2Support.GetEnvironmentAsync();
            await PreviewWeb.EnsureCoreWebView2Async(env);
            PreviewWeb.Source = new Uri(item.WebsiteUrl);
        }
        catch (Exception ex)
        {
            Logger.Warn($"Store preview: {ex.Message}");
            ShowNotification($"Preview failed: {ex.Message}", 3);
        }
    }

    private void ClosePreview_Click(object sender, MouseButtonEventArgs e)
    {
        ClosePreview();
        e.Handled = true;
    }

    private void ClosePreview()
    {
        SoundManager.Play("back");
        PreviewOverlay.Visibility = Visibility.Collapsed;
        try
        {
            PreviewWeb.CoreWebView2?.Navigate("about:blank");
        }
        catch { }
    }

    private HashSet<string> LoadWishlist()
    {
        var path = Path.Combine(_rootPath, WishlistDbPath);
        if (!File.Exists(path)) return new HashSet<string>();
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<HashSet<string>>(json) ?? new HashSet<string>();
        }
        catch { return new HashSet<string>(); }
    }

    private void SaveWishlist()
    {
        var path = Path.Combine(_rootPath, WishlistDbPath);
        var dir = Path.GetDirectoryName(path);
        if (dir != null) Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(_wishlist, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    private void ToggleWishlist_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string itemId)
        {
            var item = _catalog.Items.FirstOrDefault(i => i.Id == itemId);
            if (item != null) ToggleWishlistFor(item);
        }
        e.Handled = true;
    }

    private void ToggleWishlistFor(CatalogItem item)
    {
        if (_wishlist.Contains(item.Id))
        {
            _wishlist.Remove(item.Id);
            ShowNotification($"{item.Name} removed from wishlist", 2);
        }
        else
        {
            _wishlist.Add(item.Id);
            AchievementManager.AddProgress("wishlist", "store_shopper");
            ActivityFeed.AddCustom($"Added to wishlist: {item.Name}", "");
            ShowNotification($"{item.Name} added to wishlist", 2);
        }
        SoundManager.Play("toggle");
        SaveWishlist();
        if (_showWishlistOnly) RebuildNavItems();
    }

    private void ShowNotification(string message, int seconds)
    {
        var main = Window.GetWindow(this) as MainWindow;
        main?.ShowNotification(message, seconds);
    }

    private void ToggleWishlistFilter(object sender, MouseButtonEventArgs e)
    {
        _showWishlistOnly = !_showWishlistOnly;
        WishlistFilterText.Text = _showWishlistOnly ? "★ WISHLIST (ON)" : "★ WISHLIST";
        if (_showWishlistOnly)
        {
            WishlistFilterBtn.Background = TryFindResource("BrushWarning") as Brush ?? Brushes.Goldenrod;
            WishlistFilterText.Foreground = Brushes.Black;
        }
        else
        {
            WishlistFilterBtn.Background = TryFindResource("BrushBackgroundCard") as Brush;
            WishlistFilterText.Foreground = TryFindResource("BrushTextSecondary") as Brush;
        }
        SoundManager.Play("toggle");
        RebuildNavItems();
        RefreshLists();
    }

    private void RefreshLists()
    {
        AppsList.ItemsSource = _showWishlistOnly
            ? _catalog.Items.Where(i => i.Type == "app" && _wishlist.Contains(i.Id)).ToList()
            : _catalog.Items.Where(i => i.Type == "app").ToList();
        GamesList.ItemsSource = _showWishlistOnly
            ? _catalog.Items.Where(i => i.Type == "game" && _wishlist.Contains(i.Id)).ToList()
            : _catalog.Items.Where(i => i.Type == "game").ToList();

        var total = _showWishlistOnly ? _wishlist.Count : _catalog.Items.Count;
        CatalogStatus.Text = _showWishlistOnly
            ? $"Showing {total} wishlisted item{(total == 1 ? "" : "s")}"
            : $"Browse {total} free apps & games in the GoStudios Corporation Store";
    }
}

public class InstalledRecord
{
    public string? Exe { get; set; }
    public string? Dir { get; set; }
    public bool IsInstaller { get; set; }
}
