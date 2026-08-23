using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Threading;
using GoConsoleOS.GoConsole.Views;
using GoConsoleOS.Shared;
using GoConsoleOS.Shared.Acc;
using GoConsoleOS.Shared.Input;
using GoConsoleOS.Shared.Models;

namespace GoConsoleOS.GoConsole;

public partial class MainWindow : Window
{
    private InitConfig _config = null!;
    private LibraryScanner _scanner = null!;
    private LibraryData _library = new();
    internal ProfileManager _profileManager = null!;
    private PerformanceManager _perfManager = null!;
    private SystemMonitor _systemMonitor = null!;
    private ControllerEngine? _controller;
    private LinkHostService? _linkHost;
    private AccHostService? _accHost;

    public ControllerEngine? Controller => _controller;
    private FocusNavigator? _focusNav;
    private ScreenshotManager? _screenshot;
    private AccountManager _accountManager = null!;
    private BatteryManager? _battery;
    private OverlayWindow? _overlay;
    private GuideMenu? _guideMenu;
    private QuickAccessPanel? _quickAccess;
    private ExitMenu? _exitMenu;
    private DispatcherTimer _clockTimer;
    private DispatcherTimer _watchdogTimer = null!;
    private Process? _launchedGameProcess;
    private DispatcherTimer _statsTimer;
    private string _currentView = "home";
    private string? _browserPendingUrl;
    private GameInfo? _selectedGame;
    internal readonly List<string> _notificationHistory = new();
    private bool _isExiting;

    // ---- Lock screen ----
    private readonly List<char> _pinBuffer = new();
    private DateTime _lastActivityUtc = DateTime.UtcNow;
    private bool _isLocked;
    private DispatcherTimer _activityTimer = null!;

    // ---- USB auto-detect (WM_DEVICECHANGE) ----
    private const int WM_DEVICECHANGE = 0x0219;
    private const int DBT_DEVICEARRIVAL = 0x8000;
    private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
    private HwndSource? _hwndSource;

    public MainWindow()
    {
        InitializeComponent();

        _config = ConfigReader.ReadInitConfig();
        SettingsStore.Initialize(ConfigReader.RootPath ?? "");
        AchievementManager.Initialize(ConfigReader.RootPath ?? "");
        AchievementManager.Unlocked += def => Dispatcher.Invoke(() => ShowAchievementToast(def));
        SoundManager.Initialize(_config);
        SoundManager.Play("boot");
        ApplyVariantTheme();
        ApplyWallpaper();
        var savedAccent = SettingsStore.Get("theme.accent_primary");
        if (!string.IsNullOrEmpty(savedAccent)) ApplyAccent(savedAccent);
        ApplyVariantBranding();
        _scanner = new LibraryScanner(ConfigReader.RootPath ?? "");
        _profileManager = new ProfileManager(ConfigReader.RootPath ?? "");
        _perfManager = new PerformanceManager(ConfigReader.RootPath ?? "");
        _systemMonitor = new SystemMonitor(_config.Services.PerfMonitorIntervalMs);
        _accountManager = new AccountManager(_profileManager);

        Loaded += OnLoaded;
        Closed += OnClosed;

        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();

        _statsTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _statsTimer.Tick += (_, _) => UpdateStats();
        _statsTimer.Start();

        _activityTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
        _activityTimer.Tick += (_, _) => CheckAutoLock();
        _activityTimer.Start();
        ActivityMonitor();

        _battery = new BatteryManager(
            onUpdate: info => Dispatcher.Invoke(() => UpdateBatteryUI(info)),
            onControllerUpdate: info => Dispatcher.Invoke(() => UpdateControllerBatteryUI(info)));
    }

    private void ApplyWallpaper()
    {
        try
        {
            var path = WallpaperManager.GetCurrentPath(ConfigReader.RootPath ?? "");
            if (!File.Exists(path)) return;
            Logger.Info($"Wallpaper: {path}");

            var img = new BitmapImage();
            img.BeginInit();
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.UriSource = new Uri(path);
            img.EndInit();
            img.Freeze();
            WallpaperImage.Source = img;
        }
        catch (Exception ex)
        {
            Logger.Warn($"Wallpaper failed to load: {ex.Message}");
        }
    }

    public void RefreshWallpaper()
    {
        if (Dispatcher.CheckAccess())
            ApplyWallpaper();
        else
            Dispatcher.Invoke(ApplyWallpaper);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Keyboard.Focus(this);

        RegisterUsbDeviceChangeHook();
        StartLinkHostService();
        StartAccHostService();
        if (SettingsStore.GetBool("display.fullscreen", _config.Display.Fullscreen))
            EnterFullscreen();
        else
            SetFullscreen(false);
        Topmost = true;

        var guest = _profileManager.GetOrCreateGuestProfile();
        UpdateProfileUI(guest);

        _perfManager.ProfileChanged += mode =>
            Dispatcher.Invoke(() => StatusMode.Text = mode.ToUpper());
        StatusMode.Text = _perfManager.CurrentMode.ToUpper();

        _systemMonitor.Start();

        if (_config.Services.XinputEnabled)
            StartControllerEngine();

        var platforms = PlatformDetection.GetInstalledPlatforms();
        var hasGames = platforms.Values.Any(v => v);

        if (hasGames && _library.Games.Count == 0)
        {
            ShowScanningOverlay("SCANNING GAME LIBRARIES", "Scanning Steam, Epic, GOG, and Xbox...");
            Logger.Info("Scanning game libraries...");
            _library = await Task.Run(() => _scanner.ScanAll());
            HideScanningOverlay();
        }

        UpdateCapturesCount();

        var setupFlag = Path.Combine(ConfigReader.RootPath ?? "", "system", ".setup_complete");
        if (!File.Exists(setupFlag))
        {
            var setup = new SetupWizard(_profileManager, _accountManager, _perfManager, _config);
            setup.Owner = this;
            setup.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            setup.ShowDialog();
        }

        var login = new LoginWindow(_profileManager, _accountManager);
        login.Owner = this;
        login.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        login.Show();
        login.Closed += (_, _) =>
        {
            if (login.AuthenticatedProfile != null)
            {
                UpdateProfileUI(login.AuthenticatedProfile);
                if (!login.AuthenticatedProfile.IsGuest)
                    ShowNotification($"Signed in as {login.AuthenticatedProfile.DisplayName}", 3);
            }
        };

        NavigateTo("home");
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        UnregisterUsbDeviceChangeHook();
        _linkHost?.Stop();
        _accHost?.Stop();
        _controller?.Dispose();
        _systemMonitor.Dispose();
        _battery?.Dispose();
        _overlay?.Close();
        _guideMenu?.Close();
        Logger.Info("GoConsole shell shutting down");
        Application.Current.Shutdown();
    }

    private void RegisterUsbDeviceChangeHook()
    {
        try
        {
            var source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            if (source == null) return;
            _hwndSource = source;
            source.AddHook(WndProc);
            Logger.Info("USB device-change monitoring enabled (WM_DEVICECHANGE)");
        }
        catch (Exception ex)
        {
            Logger.Warn($"USB device-change hook failed: {ex.Message}");
        }
    }

    private void UnregisterUsbDeviceChangeHook()
    {
        try
        {
            if (_hwndSource == null) return;
            _hwndSource.RemoveHook(WndProc);
            _hwndSource = null;
        }
        catch (Exception ex)
        {
            Logger.Warn($"USB device-change unregister failed: {ex.Message}");
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_DEVICECHANGE)
        {
            switch (wParam.ToInt32())
            {
                case DBT_DEVICEARRIVAL:
                    handled = true;
                    OnUsbDeviceEvent(true, lParam);
                    break;
                case DBT_DEVICEREMOVECOMPLETE:
                    handled = true;
                    OnUsbDeviceEvent(false, lParam);
                    break;
            }
        }
        return IntPtr.Zero;
    }

    private void OnUsbDeviceEvent(bool pluggedIn, IntPtr lParam)
    {
        var volume = DescribeVolume(lParam);
        var text = pluggedIn
            ? (volume != null ? $"USB storage plugged in: {volume}" : "USB storage plugged in")
            : (volume != null ? $"USB storage removed: {volume}" : "USB storage removed");
        Logger.Info($"USB device event: {text}");

        Dispatcher.BeginInvoke(new Action(() =>
        {
            ShowNotification(text, 3);
            RefreshUsbHealthViewIfVisible();
        }));
    }

    private void RefreshUsbHealthViewIfVisible()
    {
        if (_currentView != "usbhealth") return;
        if (MainContent.Content is Views.UsbDeviceHealthView healthView)
            healthView.RefreshNow();
    }

    private static string? DescribeVolume(IntPtr lParam)
    {
        // DEV_BROADCAST_VOLUME: dbcv_unitmask at offset 12 (bit 0 = A:)
        const int volumeOffset = 12;
        const int deviceTypeOffset = 4;
        const uint DBT_DEVTYP_VOLUME = 0x00000002;
        try
        {
            if (lParam == IntPtr.Zero) return null;
            var deviceType = (uint)Marshal.ReadInt32(lParam, deviceTypeOffset);
            if (deviceType != DBT_DEVTYP_VOLUME) return null;
            var mask = Marshal.ReadInt32(lParam, volumeOffset);
            var letters = new List<char>();
            for (var i = 0; i < 26; i++)
            {
                if ((mask & (1 << i)) != 0)
                    letters.Add((char)('A' + i));
            }
            return letters.Count == 0 ? null : string.Join(", ", letters) + ":";
        }
        catch
        {
            return null;
        }
    }

    private void StartLinkHostService()
    {
        try
        {
            if (SettingsStore.GetBool("link.enabled", true))
            {
                _linkHost = new LinkHostService(
                    _scanner,
                    title =>
                    {
                        var game = _library.Games.FirstOrDefault(g =>
                            g.Title.Equals(title, StringComparison.OrdinalIgnoreCase));
                        if (game != null)
                        {
                            LaunchGameWithWatchdog(game);
                            return true;
                        }
                        Logger.Warn($"LinkServer: unknown game '{title}'");
                        return false;
                    },
                    view => Dispatcher.Invoke(() => OpenHostTool(view)));
                _linkHost.Start();
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"LinkHost init failed: {ex.Message}");
        }
    }

    private void StartAccHostService()
    {
        try
        {
            if (!SettingsStore.GetBool("acc.enabled", true)) return;
            _accHost = new AccHostService(_scanner, title =>
            {
                var game = _library.Games.FirstOrDefault(g =>
                    g.Title.Equals(title, StringComparison.OrdinalIgnoreCase));
                if (game != null)
                {
                    Dispatcher.Invoke(() => LaunchGameWithWatchdog(game));
                    return true;
                }
                Logger.Warn($"ACC/GoAI: unknown game '{title}'");
                return false;
            });
            _accHost.OnLogin += (user, display) =>
                Dispatcher.Invoke(() => ShowNotification($"Welcome back, {display}!", 3));
            _accHost.Start();
        }
        catch (Exception ex)
        {
            Logger.Warn($"ACC host init failed: {ex.Message}");
        }
    }

    private void OpenHostTool(string tool)
    {
        switch (tool)
        {
            case "usb-health":
                NavigateTo("usbhealth");
                break;
            case "cast":
                NavigateTo("remoteplay");
                break;
            case "goai":
                new Views.GoAiWindow { Owner = this }.ShowDialog();
                break;
            case "store":
                NavigateTo("store");
                break;
            case "screenshot":
                TakeScreenshot();
                break;
        }
    }

    private void TakeScreenshot()
    {
        var path = _screenshot?.CaptureScreenshot();
        if (path != null)
        {
            AchievementManager.AddProgress("screenshots", "screenshot_master");
            var name = Path.GetFileName(path);
            ShowNotification($"Screenshot saved: {name}", 3);
            ActivityFeed.AddScreenshot(name);
            UpdateCapturesCount();
        }
        else
        {
            ShowNotification("Screenshot failed", 2);
        }
    }

    public void SetFullscreen(bool fullscreen)
    {
        if (fullscreen)
        {
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
            Topmost = true;
        }
        else
        {
            Topmost = false;
            WindowStyle = WindowStyle.SingleBorderWindow;
            ResizeMode = ResizeMode.CanResize;
            WindowState = WindowState.Normal;
            Width = 1280;
            Height = 720;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }

    public void EnterFullscreen() => SetFullscreen(true);

    public void ApplyAccent(string hex)
    {
        try
        {
            var color = (System.Windows.Media.Color)ColorConverter.ConvertFromString(hex);
            Application.Current.Resources["BrushAccentPrimary"] = new SolidColorBrush(color);
            Application.Current.Resources["BrushFocusGlow"] = new SolidColorBrush(color);
            InvalidateVisual();
        }
        catch { }
    }

    private void ApplyVariantTheme()
    {
        var theme = _config.Theme;
        if (string.IsNullOrEmpty(theme.AccentPrimary) &&
            string.IsNullOrEmpty(theme.BackgroundDark) &&
            string.IsNullOrEmpty(theme.BackgroundMedium)) return;

        try
        {
            if (!string.IsNullOrEmpty(theme.BackgroundDark)) SetVariantBrush("BrushBackgroundDark", theme.BackgroundDark);
            if (!string.IsNullOrEmpty(theme.BackgroundMedium)) SetVariantBrush("BrushBackgroundMedium", theme.BackgroundMedium);
            if (!string.IsNullOrEmpty(theme.BackgroundLight)) SetVariantBrush("BrushBackgroundLight", theme.BackgroundLight);
            if (!string.IsNullOrEmpty(theme.BackgroundCard)) SetVariantBrush("BrushBackgroundCard", theme.BackgroundCard);
            if (!string.IsNullOrEmpty(theme.AccentPrimary)) SetVariantBrush("BrushAccentPrimary", theme.AccentPrimary);
            if (!string.IsNullOrEmpty(theme.AccentSecondary)) SetVariantBrush("BrushAccentSecondary", theme.AccentSecondary);
            if (!string.IsNullOrEmpty(theme.AccentTertiary)) SetVariantBrush("BrushAccentTertiary", theme.AccentTertiary);
            InvalidateVisual();
        }
        catch (Exception ex)
        {
            Logger.Warn($"Variant theme apply failed: {ex.Message}");
        }
    }

    private static void SetVariantBrush(string key, string hex)
    {
        try
        {
            var color = (System.Windows.Media.Color)ColorConverter.ConvertFromString(hex);
            Application.Current.Resources[key] = new SolidColorBrush(color);
        }
        catch { }
    }

    private void ApplyVariantBranding()
    {
        var osName = _config.General.OsName;
        if (string.IsNullOrWhiteSpace(osName)) return;
        Title = osName;
        OsLogoText.Text = osName;
    }

    public void ShowAchievementToast(AchievementDefinition def)
    {
        SoundManager.Play("achievement");
        AchievementIcon.Text = def.Icon;
        AchievementName.Text = def.Name;
        AchievementReward.Text = $"REWARD: {def.Reward}";
        AchievementToast.Visibility = Visibility.Visible;

        var anim = TryFindResource("NotificationIn") as System.Windows.Media.Animation.Storyboard;
        if (anim != null)
        {
            var copy = anim.Clone();
            copy.Begin(AchievementToast);
        }

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
        timer.Tick += (_, _) =>
        {
            AchievementToast.Visibility = Visibility.Collapsed;
            timer.Stop();
        };
        timer.Start();
    }

    public void ShowNotification(string message, int seconds = 3)
    {
        SoundManager.Play("notify");
        var timestamp = DateTime.Now.ToString("HH:mm");
        var entry = $"[{timestamp}] {message}";
        _notificationHistory.Insert(0, entry);
        if (_notificationHistory.Count > 50)
            _notificationHistory.RemoveRange(50, _notificationHistory.Count - 50);

        NotificationText.Text = message;
        NotificationOverlay.Visibility = Visibility.Visible;
        var anim = TryFindResource("NotificationIn") as System.Windows.Media.Animation.Storyboard;
        if (anim != null)
        {
            var copy = anim.Clone();
            copy.Begin(NotificationOverlay);
        }
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(seconds) };
        timer.Tick += (_, _) =>
        {
            NotificationOverlay.Visibility = Visibility.Collapsed;
            timer.Stop();
        };
        timer.Start();

        if (NotificationHistoryList != null)
        {
            NotificationHistoryList.ItemsSource = null;
            NotificationHistoryList.ItemsSource = _notificationHistory;
            NotifBadge.Text = _notificationHistory.Count.ToString();
        }
    }

    private void WatchdogTick(object? sender, EventArgs e)
    {
        if (_launchedGameProcess != null)
        {
            try
            {
                if (_launchedGameProcess.HasExited)
                {
                    _launchedGameProcess = null;
                    Dispatcher.Invoke(() =>
                    {
                        if (WindowState == WindowState.Minimized)
                        {
                            WindowState = WindowState.Maximized;
                            Topmost = true;
                            Activate();
                            ShowNotification("Game closed — back to GoConsoleOS", 4);
                        }
                    });
                }
            }
            catch
            {
                _launchedGameProcess = null;
            }
        }
    }

    private void ShowScanningOverlay(string title, string detail)
    {
        ScanningTitle.Text = title;
        ScanningDetail.Text = detail;
        ScanningBar.Width = 0;
        ScanningOverlay.Visibility = Visibility.Visible;
    }

    private void HideScanningOverlay()
    {
        ScanningBar.Width = 100;
        ScanningOverlay.Visibility = Visibility.Collapsed;
    }

    private void StartControllerEngine()
    {
        try
        {
            var kind = ControllerEngine.DetectControllerKind();
            var saved = SettingsStore.Get("controller.kind");
            if (!string.IsNullOrEmpty(saved) && Enum.TryParse<ControllerKind>(saved, out var parsed))
                kind = parsed;
            _controller = new ControllerEngine(0, _config.Services.ControllerPollRate, kind);
            _controller.ButtonPressed += OnControllerButton;
            _controller.StateUpdated += OnControllerState;
            _focusNav = new FocusNavigator(this);
            _controller.Start();
            Logger.Info($"Controller engine active in shell ({_controller.GetKindName()})");
        }
        catch (Exception ex)
        {
            Logger.Warn($"Controller init failed: {ex.Message}");
        }
    }

    private void OnControllerButton(ControllerButtons button)
    {
        Dispatcher.Invoke(() =>
        {
            switch (button)
            {
                case ControllerButtons.Guide:
                    OpenQuickAccess();
                    break;
                case ControllerButtons.Start:
                    OpenGuideMenu();
                    SoundManager.Play("select");
                    break;
                case ControllerButtons.Y:
                    ShowSearch();
                    break;
                case ControllerButtons.Back:
                    GoBack();
                    SoundManager.Play("back");
                    break;
                case ControllerButtons.A:
                    SimulateClick();
                    SoundManager.Play("select");
                    break;
                case ControllerButtons.DPadUp:
                case ControllerButtons.DPadDown:
                case ControllerButtons.DPadLeft:
                case ControllerButtons.DPadRight:
                    HandleDpadScroll(button);
                    _focusNav?.HandleDpad(button);
                    SoundManager.Play("nav");
                    break;
            }
        });
    }

    private static void SimulateClick()
    {
        var focused = FocusManager.GetFocusedElement(FocusManager.GetFocusScope(Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsVisible))) as UIElement;
        if (focused is Button btn)
        {
            btn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }
    }

    private void HandleDpadScroll(ControllerButtons button)
    {
        if (button is not (ControllerButtons.DPadUp or ControllerButtons.DPadDown)) return;
        if (MainContent.Content is not FrameworkElement page) return;

        var scroller = FindScrollViewer(page);
        if (scroller == null || scroller.Scroller == null || scroller.Scroller.ScrollableHeight <= 0) return;

        var focused = FocusManager.GetFocusedElement(MainContent) as FrameworkElement;
        if (focused != null && FocusNavigator.IsFocusable(focused)) return;

        if (button == ControllerButtons.DPadDown) scroller.ScrollDown(90);
        else scroller.ScrollUp(90);
    }

    private static Controls.OnScreenScrollViewer? FindScrollViewer(DependencyObject node)
    {
        if (node == null) return null;
        if (node is Controls.OnScreenScrollViewer viewer) return viewer;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
        {
            var child = FindScrollViewer(VisualTreeHelper.GetChild(node, i));
            if (child != null) return child;
        }
        return null;
    }

    // ---- Pen / Stylus support ----

    private bool _penActive;
    private Point _penLastPos;

    private void Window_StylusDown(object sender, StylusDownEventArgs e)
    {
        if (!SettingsStore.GetBool("input.pen", true)) return;
        _penActive = true;
        _penLastPos = e.GetPosition(this);
        Logger.Debug($"Pen down at {_penLastPos}");
    }

    private void Window_StylusMove(object sender, StylusEventArgs e)
    {
        if (!_penActive || !SettingsStore.GetBool("input.pen", true)) return;
        var pos = e.GetPosition(this);
        if (pos != _penLastPos)
        {
            _penLastPos = pos;
            var el = InputHitTest(pos) as UIElement;
            if (el != null) el.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount) { RoutedEvent = Mouse.MouseMoveEvent });
        }
    }

    private void Window_StylusUp(object sender, StylusEventArgs e)
    {
        if (!_penActive) return;
        _penActive = false;
        if (!SettingsStore.GetBool("input.pen", true)) return;
        var pos = e.GetPosition(this);
        var el = InputHitTest(pos) as UIElement;
        if (el != null)
        {
            el.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
            {
                RoutedEvent = Mouse.MouseDownEvent,
                Source = el
            });
            el.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
            {
                RoutedEvent = Mouse.MouseUpEvent,
                Source = el
            });
        }
        Logger.Debug("Pen up");
    }

    // ---- Touchscreen support ----

    private bool _touchActive;
    private Point _touchLastPos;

    private void Window_TouchDown(object sender, TouchEventArgs e)
    {
        if (!SettingsStore.GetBool("input.touch", true)) return;
        _touchActive = true;
        _touchLastPos = e.GetTouchPoint(this).Position;
    }

    private void Window_TouchMove(object sender, TouchEventArgs e)
    {
        if (!_touchActive || !SettingsStore.GetBool("input.touch", true)) return;
        _touchLastPos = e.GetTouchPoint(this).Position;
    }

    private void Window_TouchUp(object sender, TouchEventArgs e)
    {
        if (!_touchActive) return;
        _touchActive = false;
        if (!SettingsStore.GetBool("input.touch", true)) return;
        var pos = e.GetTouchPoint(this).Position;
        var el = InputHitTest(pos) as UIElement;
        if (el != null)
        {
            el.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
            {
                RoutedEvent = Mouse.MouseDownEvent,
                Source = el
            });
            el.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
            {
                RoutedEvent = Mouse.MouseUpEvent,
                Source = el
            });
        }
    }

    private void Window_TouchEnter(object sender, TouchEventArgs e)
    {
        if (!SettingsStore.GetBool("input.touch", true)) return;
    }

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    private const byte VK_ESCAPE = 0x1B;
    private const byte VK_RETURN = 0x0D;
    private const uint KEYEVENTF_KEYDOWN = 0x0000;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    private static void SendKey(byte key)
    {
        keybd_event(key, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
        keybd_event(key, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    private void OnControllerState(ControllerState state)
    {
        _focusNav?.HandleStick(state.ThumbLX, state.ThumbLY);

        if (_config.Services.MouseEmulation)
        {
            var dx = (int)(state.RightStickX * 8);
            var dy = (int)(state.RightStickY * 8);
            if (Math.Abs(dx) > 2 || Math.Abs(dy) > 2)
            {
                if (GetCursorPos(out var pos))
                    SetCursorPos(pos.X + dx, pos.Y + dy);
            }

            if (state.RightTrigger > 50)
                SendKey(VK_ESCAPE);
            if (state.LeftTrigger > 50)
                SendKey(VK_RETURN);
        }
    }

    public void NavigateToBrowser(string url)
    {
        _browserPendingUrl = url;
        NavigateTo("browser");
        _browserPendingUrl = null;
    }

    public void NavigateTo(string view)
    {
        _currentView = view;
        AchievementManager.RecordVisit(view);
        UserControl? page = view switch
        {
            "home" => new HomeView(_library, _profileManager.CurrentProfile, _perfManager, _config, NavigateToGame),
            "library" => new LibraryView(_library, _scanner, NavigateToGame),
            "store" => new StoreView(ConfigReader.RootPath ?? ""),
            "friends" => new FriendsView(_profileManager, _library),
            "guides" => new GuidesView(_library),
            "browser" => new BrowserView(_browserPendingUrl),
            "captures" => new CapturesView(),
            "remap" => new ControllerRemappingView(),
            "storage" => new StorageView(),
            "accessibility" => new AccessibilityView(),
            "downloads" => new DownloadManagerView(),
            "music" => new MusicPlayerView(),
            "dynamicbg" => new DynamicBackgroundView(),
            "network" => new NetworkTestView(),
            "achievements" => new AchievementCenterView(),
            "share" => new ScreenshotShareView(),
            "gamepass" => new GamePassView(_accHost?.Store, _profileManager.CurrentProfile),
            "party" => new PartyView(),
            "quickresume" => new QuickResumeView(),
            "recording" => new GameRecordingView(),
            "rewards" => new RewardsView(),
            "gamestats" => new GameStatsView(),
            "parental" => new ParentalControlsView(),
            "games" => new GamesView(),
            "platforms" => new PlatformsView(),
            "themes" => new ThemeSettingsView(),
            "update" => new SystemUpdateView(),
            "cloudsaves" => new CloudSaveView(),
            "remoteplay" => new RemotePlayView(),
            "compatibility" => new CompatibilityView(),
            "controllerprofiles" => new ControllerProfilesView(),
            "gamehubs" => new GameHubsView(),
            "deals" => new DealsView(),
            "backup" => new BackupRestoreView(),
            "gamesettings" => new GameSettingsView(),
            "usbmaker" => new UsbGamingConsoleMakerView(),
            "usbhealth" => new UsbDeviceHealthView(),
            "controller" => new ControllerSelectionView(),
            "discord" => new DiscordView(),
            "whatsnew" => new WhatsNewView(),
            "tokencreator" => new TokenCreatorView(),
            "playtree" => new PlayTreeView(),
            "details" when _selectedGame != null => new GameDetailsView(_selectedGame, _library, _scanner, _perfManager),
            "settings" => new SettingsView(_config, _perfManager, _profileManager.CurrentProfile, _controller, _accountManager, _profileManager),
            _ => new HomeView(_library, _profileManager.CurrentProfile, _perfManager, _config)
        };

        MainContent.Content = page;

        SoundManager.Play("nav");

        var fadeIn = TryFindResource("SlideInRight") as System.Windows.Media.Animation.Storyboard;
        if (fadeIn != null)
        {
            var copy = fadeIn.Clone();
            copy.Begin(MainContent);
        }

        foreach (var btn in new[] { NavHome, NavLibrary, NavStore, NavFriends, NavGuides, NavSettings })
        {
            btn.IsEnabled = btn.Tag?.ToString() != view;
            btn.Foreground = btn.Tag?.ToString() == view
                ? FindResource("BrushAccentPrimary") as System.Windows.Media.Brush
                : FindResource("BrushTextSecondary") as System.Windows.Media.Brush;
        }

        Logger.Info($"Navigated to: {view}");
    }

    public class SearchResult
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Type { get; set; } = "";
        public string NavigateView { get; set; } = "";
    }

    public void NavigateToGame(GameInfo game)
    {
        _selectedGame = game;
        NavigateTo("details");
    }

    public void OpenGameInBrowser(GameInfo game)
    {
        var url = game.GetStoreUrl();
        _currentView = "platforms";
        var platformView = new PlatformsView();
        platformView.LoadUrl(url);
        MainContent.Content = platformView;
    }

    public void ShowPlatformView(PlatformsView platformView)
    {
        _currentView = "platforms";
        MainContent.Content = platformView;
    }

    public void LaunchGameWithWatchdog(GameInfo game, Action? onLaunchSuccess = null)
    {
        AchievementManager.Unlock("first_launch");
        ActivityFeed.AddGameLaunch(game.Title, game.Platform);
        GameLauncher.Launch(game, onLaunchSuccess, process =>
        {
            _launchedGameProcess = process;
            if (process != null && !process.HasExited)
            {
                Logger.Info($"Watchdog tracking process {process.Id} for {game.Title}");
            }
        });
    }

    private void NavButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag)
            NavigateTo(tag);
    }

    private void GoAiButton_Click(object sender, RoutedEventArgs e)
    {
        var win = new Views.GoAiWindow { Owner = this };
        win.ShowDialog();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        OpenExitMenu();
    }

    public void OpenExitMenu()
    {
        if (_exitMenu != null && _exitMenu.IsVisible)
        {
            _exitMenu.Close();
            _exitMenu = null;
            return;
        }

        _exitMenu = new ExitMenu(_perfManager, _controller);
        _exitMenu.Closed += (_, _) => _exitMenu = null;
        _exitMenu.Show();
    }

    private void ToggleOverlay()
    {
        if (_overlay != null && _overlay.IsVisible)
        {
            _overlay.Hide();
            return;
        }

        if (_overlay == null)
        {
            _overlay = new OverlayWindow(_config, _perfManager, _systemMonitor, _controller);
            _overlay.Closed += (_, _) => _overlay = null;
        }

        _overlay.Show();
        _overlay.Activate();
    }

    private void OpenQuickAccess()
    {
        if (_quickAccess != null && _quickAccess.IsVisible)
        {
            _quickAccess.Close();
            _quickAccess = null;
            return;
        }

        _quickAccess = new QuickAccessPanel(_perfManager, _screenshot);
        _quickAccess.Closed += (_, _) => _quickAccess = null;
        _quickAccess.Show();
    }

    private void OpenGuideMenu()
    {
        if (_guideMenu != null && _guideMenu.IsVisible)
        {
            _guideMenu.Close();
            _guideMenu = null;
            return;
        }

        _guideMenu = new GuideMenu(_perfManager, _controller);
        _guideMenu.Closed += (_, _) => _guideMenu = null;
        _guideMenu.Show();
    }

    private void ShowSearch()
    {
        var keyboard = new OnScreenKeyboard();
        keyboard.Owner = this;

        if (keyboard.ShowDialog() == true && !string.IsNullOrEmpty(keyboard.InputText))
        {
            var search = keyboard.InputText.ToLowerInvariant();
            var results = _library.Games
                .Where(g => g.Title.ToLowerInvariant().Contains(search))
                .ToList();

            if (results.Count > 0)
            {
                NavigateTo("library");
                var msg = $"Found {results.Count} game{(results.Count == 1 ? "" : "s")} matching \"{keyboard.InputText}\":\n\n" +
                          string.Join("\n", results.Take(10).Select(g => $"  \u2022 {g.Title} ({g.Platform})"));
                if (results.Count > 10) msg += $"\n  ... and {results.Count - 10} more";
                MessageBox.Show(msg, "Search Results", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"No games found matching \"{keyboard.InputText}\".",
                    "Search", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    private void GoBack()
    {
        if (_currentView != "home")
        _screenshot = new ScreenshotManager();

        _watchdogTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _watchdogTimer.Tick += WatchdogTick;
        _watchdogTimer.Start();

        NavigateTo("home");
    }

    private void UpdateClock()
    {
        ClockText.Text = DateTime.Now.ToString("HH:mm:ss");
        StatusClock.Text = DateTime.Now.ToString("ddd, MMM dd  HH:mm:ss");
        if (_isLocked) UpdateLockClock();
    }

    private void UpdateStats()
    {
        try
        {
            if (_systemMonitor.CurrentStats != null)
            {
                var fps = _systemMonitor.CurrentStats.Fps;
                StatusFps.Text = fps > 0 ? $"{fps} FPS" : "";
            }

            var ni = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(n => n.OperationalStatus == OperationalStatus.Up);
            if (ni != null)
            {
                StatusNetworkIcon.Text = "\u25CF";
                StatusNetworkIcon.Foreground = FindResource("BrushSuccess") as System.Windows.Media.Brush;
                StatusNetwork.Text = "Online";
            }
            else
            {
                StatusNetworkIcon.Text = "\u25CB";
                StatusNetworkIcon.Foreground = FindResource("BrushTextMuted") as System.Windows.Media.Brush;
                StatusNetwork.Text = "Offline";
            }

            StatusVolume.Text = "75%";
        }
        catch { }
    }

    private void UpdateBatteryUI(BatteryInfo info)
    {
        if (!info.IsPresent)
        {
            BatteryText.Text = "N/A";
            BatteryIcon.Text = "🔌";
            return;
        }
        BatteryText.Text = $"{info.Percent}%";
        BatteryIcon.Text = info.IsCharging ? "⚡" : info.Percent <= 20 ? "🪫" : "🔋";
        BatteryIcon.Foreground = info.Percent <= 20
            ? FindResource("BrushError") as System.Windows.Media.Brush
            : info.IsCharging
                ? FindResource("BrushSuccess") as System.Windows.Media.Brush
                : FindResource("BrushTextSecondary") as System.Windows.Media.Brush;
    }

    private void UpdateControllerBatteryUI(ControllerBatteryInfo info)
    {
        if (!info.IsConnected)
        {
            ControllerBatteryText.Text = "No controller";
            ControllerBatteryIcon.Text = "🎮";
            ControllerBatteryIcon.Foreground = FindResource("BrushTextMuted") as System.Windows.Media.Brush;
            return;
        }
        ControllerBatteryText.Text = $"{info.Percent}%";
        ControllerBatteryIcon.Text = info.Percent <= 20 ? "🪫" : "🎮";
        ControllerBatteryIcon.Foreground = info.Percent <= 20
            ? FindResource("BrushWarning") as System.Windows.Media.Brush
            : FindResource("BrushSuccess") as System.Windows.Media.Brush;
    }

    public void OpenSearchKeyboard()
    {
        var osk = new OnScreenKeyboard(_controller);
        osk.Owner = this;
        osk.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        if (osk.ShowDialog() == true && !string.IsNullOrEmpty(osk.InputText))
            PerformSearch(osk.InputText);
    }

    public void OpenOnScreenKeyboard()
    {
        var focused = Keyboard.FocusedElement as UIElement;
        var initial = "";
        var isPassword = false;
        if (focused is TextBox tb) initial = tb.Text;
        else if (focused is PasswordBox pb) { initial = pb.Password; isPassword = true; }

        var osk = new OnScreenKeyboard(_controller)
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        osk.SetInitialText(initial);
        if (osk.ShowDialog() == true)
        {
            var result = osk.InputText;
            if (!isPassword && focused is TextBox t)
            {
                t.Text = result;
                t.CaretIndex = result.Length;
                t.Focus();
            }
            else if (isPassword && focused is PasswordBox p)
            {
                p.Password = result;
                p.Focus();
            }
        }
    }

    private void OpenKeyboard(object sender, MouseButtonEventArgs e)
    {
        OpenOnScreenKeyboard();
    }

    private void OpenSearchKeyboard(object sender, MouseButtonEventArgs e)
    {
        OpenSearchKeyboard();
    }

    private void PerformSearch(string query)
    {
        SearchPlaceholder.Visibility = Visibility.Collapsed;
        SearchQueryText.Visibility = Visibility.Visible;
        SearchQueryText.Text = query;

        var results = new System.Collections.Generic.List<SearchResult>();

        // Search games
        foreach (var game in _library.Games)
        {
            if (game.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
                results.Add(new SearchResult
                {
                    Id = game.Id,
                    Title = game.Title,
                    Description = $"{game.Platform} \u2022 {game.PlaytimeMinutes / 60}h played",
                    Icon = "🎮",
                    Type = "Game",
                    NavigateView = "details"
                });
        }

        // Search settings views
        var settings = new[] {
            ("themes", "Theme Customizer", "Change colors, presets, and backgrounds", "🎨"),
            ("remap", "Controller Remapping", "Reassign button mappings", "🎮"),
            ("storage", "Storage Management", "Manage disk usage and cache", "💾"),
            ("accessibility", "Accessibility Settings", "High contrast, narrator, color filters", "♿"),
            ("downloads", "Download Manager", "View and manage downloads", "📥"),
            ("music", "Music Player", "Play local music files", "🎵"),
            ("network", "Network Test", "Test ping and connection speed", "🌐"),
            ("achievements", "Achievement Center", "View all achievements", "🏆"),
            ("share", "Screenshot Share", "Export and share screenshots", "📷"),
            ("gamepass", "Game Pass", "Browse subscription catalog", "⭐"),
            ("party", "Party System", "Create and join parties", "👥"),
            ("quickresume", "Quick Resume", "Resume suspended games", "⏸"),
            ("recording", "Game Recording", "Record gameplay clips", "🎬"),
            ("rewards", "Rewards & Points", "Earn points for activities", "💰"),
            ("gamestats", "Game Stats Hub", "Track playtime and progress", "📊"),
            ("parental", "Parental Controls", "Family and content restrictions", "🔒"),
            ("update", "System Update", "Check for GoConsoleOS updates", "🔃"),
            ("cloudsaves", "Cloud Saves", "Sync game saves to the cloud", "☁"),
            ("remoteplay", "Remote Play", "Stream games to other devices", "📡"),
            ("compatibility", "Game Compatibility", "Verified-style ratings for your library", "✓"),
            ("controllerprofiles", "Controller Profiles", "Per-game controller configurations", "🎮"),
            ("gamehubs", "Game Hubs", "News, DLC, trophies, and community", "🏟"),
            ("deals", "Deals & Sales", "Wishlist price-drop tracker", "💰"),
            ("backup", "Backup & Restore", "Export and restore console data", "📦"),
            ("gamesettings", "Game Settings", "Per-game presets and launch options", "⚙"),
            ("usbmaker", "USB Gaming Console Maker", "Install GoConsoleOS onto any USB drive", "💾"),
            ("discord", "Discord", "Chat and voice calls for your servers", "💬"),
            ("tokencreator", "Discord Token Creator", "Create your own Discord token by signing in", "🎫"),
        };

        foreach (var (id, name, desc, icon) in settings)
        {
            if (name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                desc.Contains(query, StringComparison.OrdinalIgnoreCase))
                results.Add(new SearchResult
                {
                    Id = id,
                    Title = name,
                    Description = desc,
                    Icon = icon,
                    Type = "Settings",
                    NavigateView = id
                });
        }

        // Search platforms
        if ("steam epic xbox gog playstation nintendo battlenet ea ubisoft itch browser platforms".Contains(query, StringComparison.OrdinalIgnoreCase))
            results.Add(new SearchResult
            {
                Id = "platforms",
                Title = "Platform Stores",
                Description = "Browse Steam, Epic, Xbox, GOG, PlayStation, Nintendo, Battle.net, EA, Ubisoft, and itch.io stores",
                Icon = "🌐",
                Type = "Feature",
                NavigateView = "platforms"
            });

        if ("store gostore gostudios apps games install download".Contains(query, StringComparison.OrdinalIgnoreCase))
            results.Add(new SearchResult
            {
                Id = "store",
                Title = "GoStudios Store",
                Description = "Install free apps and games (7-Zip, VLC, OBS, OpenRA, SuperTux...) with web previews",
                Icon = "🛒",
                Type = "Feature",
                NavigateView = "store"
            });

        if ("games snake pong breakout tetris dino".Contains(query, StringComparison.OrdinalIgnoreCase))
            results.Add(new SearchResult
            {
                Id = "games",
                Title = "Built-in Games",
                Description = "Play Snake, Pong, Breakout, Tetris, and Dino Runner",
                Icon = "🎮",
                Type = "Feature",
                NavigateView = "games"
            });

        // Show results
        SearchResultsList.ItemsSource = results;
        SearchResultCount.Text = $"{results.Count} result{(results.Count == 1 ? "" : "s")}";
        SearchResultsOverlay.Visibility = results.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        if (results.Count == 0)
            ToastManager.Show($"No results for \"{query}\"");
    }

    private void SearchResultClicked(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string id)
        {
            SearchResultsOverlay.Visibility = Visibility.Collapsed;
            SearchPlaceholder.Visibility = Visibility.Visible;
            SearchQueryText.Visibility = Visibility.Collapsed;
            SearchQueryText.Text = "";
            NavigateTo(id);
        }
    }

    private void CloseSearchResults(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is Border b && b.Name == "SearchResultsOverlay")
        {
            SearchResultsOverlay.Visibility = Visibility.Collapsed;
            SearchPlaceholder.Visibility = Visibility.Visible;
            SearchQueryText.Visibility = Visibility.Collapsed;
        }
    }

    public void UpdateProfileUI(UserProfile profile)
    {
        ProfileNameText.Text = profile.DisplayName;
        StatusProfileName.Text = profile.DisplayName;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                GoBack();
                break;
            case Key.F1:
                NavigateTo("home");
                break;
            case Key.F2:
                NavigateTo("library");
                break;
            case Key.F3:
                NavigateTo("store");
                break;
            case Key.F4:
                if (e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Alt))
                    ShowExitDialog();
                break;
            case Key.F5:
                NavigateTo("settings");
                break;
            case Key.F6:
                new CustomColorPicker().ShowDialog();
                break;
            case Key.F7:
                NavigateTo("browser");
                break;
            case Key.F8:
                NavigateTo("captures");
                break;
            case Key.F9:
                ToggleOverlay();
                break;
            case Key.F11:
                _perfManager.CycleProfile();
                StatusMode.Text = _perfManager.CurrentMode.ToUpper();
                break;
            case Key.F12:
                TakeScreenshot();
                break;
            case Key.F10:
                NavigateTo("downloads");
                break;
        }

        if (e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control))
        {
            switch (e.Key)
            {
                case Key.D1: NavigateTo("music"); break;
                case Key.D2: NavigateTo("achievements"); break;
                case Key.D3: NavigateTo("gamepass"); break;
                case Key.D4: NavigateTo("party"); break;
                case Key.D5: NavigateTo("network"); break;
                case Key.D6: NavigateTo("share"); break;
                case Key.D7: NavigateTo("dynamicbg"); break;
                case Key.D8: NavigateTo("quickresume"); break;
                case Key.D9: NavigateTo("recording"); break;
                case Key.D0: NavigateTo("rewards"); break;
            }
        }

        if (e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control) &&
            e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            switch (e.Key)
            {
                case Key.R: NavigateTo("recording"); break;
                case Key.P: NavigateTo("parental"); break;
                case Key.S: NavigateTo("gamestats"); break;
                case Key.U: NavigateTo("update"); break;
                case Key.C: NavigateTo("cloudsaves"); break;
                case Key.M: NavigateTo("remoteplay"); break;
                case Key.V: NavigateTo("compatibility"); break;
                case Key.F: NavigateTo("controllerprofiles"); break;
                case Key.H: NavigateTo("gamehubs"); break;
                case Key.D: NavigateTo("deals"); break;
                case Key.B: NavigateTo("backup"); break;
                case Key.O: NavigateTo("gamesettings"); break;
                case Key.W: NavigateTo("usbmaker"); break;
                case Key.I: NavigateTo("discord"); break;
                case Key.T: NavigateTo("tokencreator"); break;
            }
        }

        if (e.Key == Key.G && e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control))
            NavigateTo("games");
        if (e.Key == Key.E && e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control))
            NavigateTo("platforms");
        if (e.Key == Key.K && e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control))
            OpenOnScreenKeyboard();
        if (e.Key == Key.T && e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control))
            NavigateTo("themes");
        if (e.Key == Key.L && e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control))
            LockConsole_Click(this, new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left));
    }

    private void Window_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Topmost = false;
        }
        else if (WindowState == WindowState.Normal || WindowState == WindowState.Maximized)
        {
            Topmost = true;
            Activate();
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (!_isExiting)
        {
            e.Cancel = true;
            ShowExitDialog();
        }
        base.OnClosing(e);
    }

    private void ShowExitDialog()
    {
        OpenExitMenu();
    }

    public void ExitToDesktop()
    {
        _isExiting = true;
        WindowState = WindowState.Minimized;
        Topmost = false;
        Hide();
    }

    public void ShutdownGoConsoleOS()
    {
        _isExiting = true;
        Close();
    }

    private void ShowAccountMenu(object sender, MouseButtonEventArgs e)
    {
        var result = MessageBox.Show(
            $"Current profile: {_profileManager.CurrentProfile?.DisplayName ?? "Guest"}\n\n" +
            "Choose an option:\n" +
            "Yes = Sign out & switch account\n" +
            "No = Open ACC account portal (web)\n" +
            "Cancel = Stay signed in",
            "Account",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            var login = new LoginWindow(_profileManager, _accountManager);
            login.Owner = this;
            login.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            login.ShowDialog();
            if (login.AuthenticatedProfile != null)
                UpdateProfileUI(login.AuthenticatedProfile);
        }
        else if (result == MessageBoxResult.No)
        {
            OpenAccountPortal();
        }
    }

    private void OpenAccountPortal()
    {
        try
        {
            var url = $"http://localhost:{GoConsoleServer.DefaultPort}/";
            var psi = new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true };
            System.Diagnostics.Process.Start(psi);
        }
        catch (Exception ex)
        {
            Logger.Warn($"Open ACC portal failed: {ex.Message}");
        }
    }

    private void OpenBrowser(object sender, MouseButtonEventArgs e)
    {
        NavigateTo("browser");
    }

    private void CapturesIndicator_Click(object sender, MouseButtonEventArgs e)
    {
        NavigateTo("captures");
    }

    public void ToggleNotificationPanel()
    {
        NotificationHistoryPanel.Visibility =
            NotificationHistoryPanel.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
    }

    public void NotifBell_Click(object sender, MouseButtonEventArgs e)
    {
        NotificationHistoryPanel.Visibility =
            NotificationHistoryPanel.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
    }

    private void ClearNotifications_Click(object sender, MouseButtonEventArgs e)
    {
        _notificationHistory.Clear();
        NotificationHistoryList.ItemsSource = null;
        NotificationHistoryList.ItemsSource = _notificationHistory;
        NotifBadge.Text = "";
        NotificationHistoryPanel.Visibility = Visibility.Collapsed;
    }

    private void UpdateCapturesCount()
    {
        try
        {
            var dir = Path.Combine(ConfigReader.RootPath ?? "", "system", "screenshots");
            if (Directory.Exists(dir))
                StatusCaptures.Text = Directory.GetFiles(dir, "*.png").Length.ToString();
            else
                StatusCaptures.Text = "0";
        }
        catch { StatusCaptures.Text = "0"; }
    }

    public void SetNightMode(bool enabled)
    {
        NightModeOverlay.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
        if (enabled) AchievementManager.Unlock("night_owl");
        ShowNotification(enabled ? "Night Mode enabled" : "Night Mode disabled", 2);
    }

    public void UpdateThemeColors(Color accent, string theme)
    {
        try
        {
            var bgColor = theme switch
            {
                "darker" => Color.FromRgb(0x05, 0x05, 0x08),
                "amber" => Color.FromRgb(0x1A, 0x15, 0x08),
                _ => Color.FromRgb(0x0D, 0x0D, 0x14)
            };

            Application.Current.Resources["BrushAccentPrimary"] = new SolidColorBrush(accent);
            Application.Current.Resources["BrushFocusGlow"] = new SolidColorBrush(accent);
            Application.Current.Resources["BrushBackgroundDark"] = new SolidColorBrush(bgColor);

            Background = new SolidColorBrush(bgColor);
            InvalidateVisual();
        }
        catch { }
    }

    // ---- Lock screen ----

    private void ActivityMonitor()
    {
        PreviewMouseDown += (_, _) => RegisterActivity();
        PreviewMouseMove += (_, _) => RegisterActivity();
        PreviewKeyDown += (_, _) => RegisterActivity();
        PreviewStylusDown += (_, _) => RegisterActivity();
        PreviewTouchDown += (_, _) => RegisterActivity();
        MouseMove += (_, _) => RegisterActivity();
        KeyDown += (_, _) => RegisterActivity();
    }

    private void RegisterActivity()
    {
        _lastActivityUtc = DateTime.UtcNow;
    }

    private void CheckAutoLock()
    {
        if (_isLocked) return;
        var timeoutMin = SettingsStore.GetInt("lock.timeout_minutes", 0);
        if (timeoutMin <= 0) return;
        if (!SettingsStore.GetBool("lock.enabled", false)) return;
        if (string.IsNullOrWhiteSpace(SettingsStore.Get("lock.pin"))) return;
        if (DateTime.UtcNow - _lastActivityUtc >= TimeSpan.FromMinutes(timeoutMin))
            LockNow();
    }

    public void LockNow()
    {
        if (_isLocked) return;
        _pinBuffer.Clear();
        UpdatePinDots();
        LockErrorText.Text = "";
        var pin = SettingsStore.Get("lock.pin");
        NoPinUnlockBtn.Visibility = string.IsNullOrWhiteSpace(pin) ? Visibility.Visible : Visibility.Collapsed;
        LockOverlay.Visibility = Visibility.Visible;
        LockOverlay.Focus();
        _isLocked = true;
        UpdateLockClock();
        Keyboard.Focus(LockOverlay);
    }

    public void UnlockConsole()
    {
        if (!_isLocked) return;
        _isLocked = false;
        LockOverlay.Visibility = Visibility.Collapsed;
        _pinBuffer.Clear();
        RegisterActivity();
    }

    private void UpdateLockClock()
    {
        if (LockClock == null || LockDate == null) return;
        LockClock.Text = DateTime.Now.ToString("HH:mm");
        LockDate.Text = DateTime.Now.ToString("ddd, MMM d");
    }

    private void UpdatePinDots()
    {
        if (PinDots == null) return;
        PinDots.Text = new string('\u2022', _pinBuffer.Count).PadRight(4, '\u00B7');
    }

    private string GetPin() => new string(_pinBuffer.ToArray());

    private void LockConsole_Click(object sender, MouseButtonEventArgs e)
    {
        if (!SettingsStore.GetBool("lock.enabled", false) && string.IsNullOrWhiteSpace(SettingsStore.Get("lock.pin")))
        {
            ShowNotification("Set a PIN in Settings > Security to enable lock screen", 3);
            NavigateTo("settings");
            return;
        }
        LockNow();
    }

    private void PinDigit_Click(object sender, RoutedEventArgs e)
    {
        if (_pinBuffer.Count >= 4) return;
        _pinBuffer.Add((sender as Button)?.Tag?.ToString()?[0] ?? '0');
        UpdatePinDots();
        LockErrorText.Text = "";
        if (_pinBuffer.Count == 4) CheckPin();
    }

    private void PinBackspace_Click(object sender, RoutedEventArgs e)
    {
        if (_pinBuffer.Count > 0) _pinBuffer.RemoveAt(_pinBuffer.Count - 1);
        UpdatePinDots();
        LockErrorText.Text = "";
    }

    private void CheckPin()
    {
        var expected = SettingsStore.Get("lock.pin");
        if (!string.IsNullOrEmpty(expected) && GetPin() == expected)
        {
            UnlockConsole();
            ShowNotification("Console unlocked", 2);
        }
        else if (string.IsNullOrEmpty(expected))
        {
            UnlockConsole();
        }
        else
        {
            _pinBuffer.Clear();
            UpdatePinDots();
            LockErrorText.Text = "Wrong PIN - try again";
        }
    }

    private void PinUnlock_Click(object sender, RoutedEventArgs e) => CheckPin();

    private void SkipLock_Click(object sender, RoutedEventArgs e) => UnlockConsole();

    private void LockOverlay_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key >= Key.D0 && e.Key <= Key.D9)
        {
            var digit = (char)('0' + (e.Key - Key.D0));
            if (_pinBuffer.Count < 4)
            {
                _pinBuffer.Add(digit);
                UpdatePinDots();
                LockErrorText.Text = "";
                if (_pinBuffer.Count == 4) CheckPin();
            }
            e.Handled = true;
        }
        else if (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9)
        {
            var digit = (char)('0' + (e.Key - Key.NumPad0));
            if (_pinBuffer.Count < 4)
            {
                _pinBuffer.Add(digit);
                UpdatePinDots();
                LockErrorText.Text = "";
                if (_pinBuffer.Count == 4) CheckPin();
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Back)
        {
            PinBackspace_Click(this, new RoutedEventArgs());
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            PinUnlock_Click(this, new RoutedEventArgs());
            e.Handled = true;
        }
    }
}
