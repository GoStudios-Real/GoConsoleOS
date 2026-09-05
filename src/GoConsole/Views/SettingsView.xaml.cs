using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GoConsoleOS.Shared;
using GoConsoleOS.Shared.Input;
using GoConsoleOS.Shared.Models;

namespace GoConsoleOS.GoConsole.Views;

public partial class SettingsView : UserControl
{
    private readonly PerformanceManager _perfManager;
    private readonly UserProfile? _profile;
    private readonly ControllerEngine? _controller;
    private readonly AccountManager _accountManager;
    private readonly ProfileManager _profileManager;

    public SettingsView(InitConfig config, PerformanceManager perfManager, UserProfile? profile, ControllerEngine? controller, AccountManager? accountManager = null, ProfileManager? profileManager = null)
    {
        InitializeComponent();

        _perfManager = perfManager;
        _profile = profile;
        _controller = controller;
        _accountManager = accountManager ?? new AccountManager(new ProfileManager(ConfigReader.RootPath ?? ""));
        _profileManager = profileManager ?? new ProfileManager(ConfigReader.RootPath ?? "");

        CurrentModeText.Text = $"Current: {perfManager.GetCurrentProfile()?.Name ?? "Balanced"}";
        AboutText.Text = $"{config.General.OsName} v{config.General.Version}";
        VolumeSlider.Value = config.Sound.Volume;
        VolumeText.Text = $"Volume: {(int)VolumeSlider.Value}%";
        SoundFxCheck.IsChecked = config.Sound.Enabled;

        var res = config.Display.Resolution;
        DisplayInfo.Text = $"{res} | Fullscreen: {config.Display.Fullscreen}";

        var cur = WallpaperManager.GetCurrentPath(ConfigReader.RootPath ?? "");
        WallpaperInfo.Text = System.IO.Path.GetFileNameWithoutExtension(cur).ToUpperInvariant();

        if (_controller != null)
        {
            ControllerStatus.Text = _controller.IsConnected
                ? $"Controller connected (Player {_controller.ControllerIndex + 1}) — {_controller.GetKindName()}"
                : "No controller detected";
            _controller.Connected += () =>
                Dispatcher.Invoke(() => ControllerStatus.Text = $"Controller connected (Player 1) — {_controller.GetKindName()}");
            _controller.Disconnected += () =>
                Dispatcher.Invoke(() => ControllerStatus.Text = "Controller disconnected");
        }

        VibrationCheck.IsChecked = SettingsStore.GetBool("services.vibration", true);
        FullscreenCheck.IsChecked = SettingsStore.GetBool("display.fullscreen", config.Display.Fullscreen);

        var savedKind = SettingsStore.Get("controller.kind");
        var kindIndex = savedKind switch
        {
            "Xbox" => 1,
            "PlayStation5" => 2,
            "Switch2" => 3,
            "Generic" => 4,
            _ => 0
        };
        ControllerKindCombo.SelectedIndex = kindIndex;

        LoadAccountInfo();

        try
        {
            var os = Environment.OSVersion.VersionString;
            var ram = $"RAM: {Environment.WorkingSet / 1024 / 1024} MB used";
            var cpu = $"{PlatformDetection.GetCpuName()} ({PlatformDetection.GetCpuTier()})";
            SystemInfoText.Text = $"{os} | {ram} | 64-bit\n{cpu}\n{PlatformDetection.GetCpuRecommendation()}";
        }
        catch
        {
            SystemInfoText.Text = "System info unavailable";
        }

        InitLockSettings();
    }

    private void PerfBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string mode)
        {
            _perfManager.SetProfile(mode);
            SettingsStore.Set("performance.mode", mode);
            CurrentModeText.Text = $"Current: {_perfManager.GetCurrentProfile()?.Name ?? mode}";
        }
    }

    private void Volume_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        VolumeText.Text = $"Volume: {(int)e.NewValue}%";
        SoundManager.Volume = e.NewValue / 100.0;
        SettingsStore.SetInt("sound.volume", (int)e.NewValue);
    }

    private void SoundFx_Changed(object sender, RoutedEventArgs e)
    {
        SoundManager.Enabled = SoundFxCheck.IsChecked == true;
        SettingsStore.SetBool("sound.enabled", SoundManager.Enabled);
        if (SoundManager.Enabled)
            SoundManager.Play("toggle");
        Logger.Info($"Sound effects: {SoundManager.Enabled}");
    }

    private void TestSound_Click(object sender, MouseButtonEventArgs e)
    {
        SoundManager.Play("select");
    }

    private void PreviewStore(object sender, MouseButtonEventArgs e)
    {
        (Window.GetWindow(this) as MainWindow)?.NavigateTo("store");
    }

    private void Vibration_Changed(object sender, RoutedEventArgs e)
    {
        SettingsStore.SetBool("services.vibration", VibrationCheck.IsChecked == true);
        Logger.Info($"Controller vibration: {VibrationCheck.IsChecked}");
    }

    private void Fullscreen_Changed(object sender, RoutedEventArgs e)
    {
        var fs = FullscreenCheck.IsChecked == true;
        SettingsStore.SetBool("display.fullscreen", fs);
        (Window.GetWindow(this) as MainWindow)?.SetFullscreen(fs);
        SoundManager.Play("toggle");
    }

    private void Accent_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border b && b.Tag is string hex)
        {
            (Window.GetWindow(this) as MainWindow)?.ApplyAccent(hex);
            SettingsStore.Set("theme.accent_primary", hex);
            SoundManager.Play("select");
        }
    }

    private void CustomAccent_Click(object sender, MouseButtonEventArgs e)
    {
        var picker = new CustomColorPicker { Owner = Window.GetWindow(this) };
        picker.ShowDialog();
        if (Application.Current.Resources["BrushAccentPrimary"] is SolidColorBrush accent)
            SettingsStore.Set("theme.accent_primary", $"#{accent.Color.R:X2}{accent.Color.G:X2}{accent.Color.B:X2}");
    }

    private void ControllerKind_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (ControllerKindCombo.SelectedItem is ComboBoxItem item && item.Tag is string kind)
        {
            SettingsStore.Set("controller.kind", kind);
            if (Enum.TryParse<ControllerKind>(kind, out var parsed) && _controller != null)
                _controller.SetKind(parsed);
            var name = kind switch
            {
                "Xbox" => "Xbox Controller",
                "PlayStation5" => "PlayStation 5 (DualSense)",
                "Switch2" => "Nintendo Switch 2",
                "Generic" => "Generic Gamepad",
                _ => "Auto-detect"
            };
            ControllerStatus.Text = $"{name} selected";
            Logger.Info($"Controller type set to: {name}");
            SoundManager.Play("select");
        }
    }

    private void Resolution_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (ResolutionCombo.SelectedItem is ComboBoxItem item)
        {
            var res = item.Tag?.ToString() ?? "auto";
            DisplayInfo.Text = $"{res} | Fullscreen: True";
            Logger.Info($"Display resolution set to: {res}");
        }
    }

    private void LoadAccountInfo()
    {
        var profile = _profile ?? _profileManager.CurrentProfile;
        if (profile == null || profile.IsGuest)
        {
            AccountInfoText.Text = "Signed in as Guest (no account)\nClick SIGN IN in the nav bar to create an account.";
            ChangePassBtn.Visibility = Visibility.Collapsed;
            ChangeNameBtn.Visibility = Visibility.Collapsed;
            return;
        }
        AccountInfoText.Text = $"User: {profile.Username}\n" +
                               $"Display Name: {profile.DisplayName}\n" +
                               $"Email: {profile.Email ?? "Not set"}\n" +
                               $"Account created: {profile.CreatedAt:MMM dd, yyyy}";
    }

    private void ChangeDisplayName(object sender, MouseButtonEventArgs e)
    {
        var profile = _profile ?? _profileManager.CurrentProfile;
        if (profile == null || profile.IsGuest)
        {
            MessageBox.Show("Please sign in to edit your profile.", "Account",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var keyboard = new OnScreenKeyboard();
        keyboard.Owner = Window.GetWindow(this);

        if (keyboard.ShowDialog() == true && !string.IsNullOrEmpty(keyboard.InputText))
        {
            var (success, msg) = _accountManager.UpdateDisplayName(profile.Username, keyboard.InputText.Trim());
            MessageBox.Show(msg, success ? "Updated" : "Error",
                MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Warning);
            if (success) LoadAccountInfo();
        }
    }

    private void ChangePassword(object sender, MouseButtonEventArgs e)
    {
        var profile = _profile ?? _profileManager.CurrentProfile;
        if (profile == null || profile.IsGuest || string.IsNullOrEmpty(profile.PasswordHash))
        {
            MessageBox.Show("Guest accounts cannot change passwords. Sign in to set one.",
                "Account", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var currentDlg = new OnScreenKeyboard();
        currentDlg.Owner = Window.GetWindow(this);
        if (currentDlg.ShowDialog() != true || string.IsNullOrEmpty(currentDlg.InputText)) return;

        var newDlg = new OnScreenKeyboard();
        newDlg.Owner = Window.GetWindow(this);
        if (newDlg.ShowDialog() != true || string.IsNullOrEmpty(newDlg.InputText)) return;

        var confirmDlg = new OnScreenKeyboard();
        confirmDlg.Owner = Window.GetWindow(this);
        if (confirmDlg.ShowDialog() != true || string.IsNullOrEmpty(confirmDlg.InputText)) return;

        if (newDlg.InputText != confirmDlg.InputText)
        {
            MessageBox.Show("Passwords do not match.", "Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var (success, msg) = _accountManager.ChangePassword(profile.Username, currentDlg.InputText, newDlg.InputText);
        MessageBox.Show(msg, success ? "Success" : "Error",
            MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void OpenRemapping(object sender, MouseButtonEventArgs e)
    {
        (Window.GetWindow(this) as MainWindow)?.NavigateTo("remap");
    }

    private void OpenStorage(object sender, MouseButtonEventArgs e)
    {
        (Window.GetWindow(this) as MainWindow)?.NavigateTo("storage");
    }

    private void OpenAccessibility(object sender, MouseButtonEventArgs e)
    {
        (Window.GetWindow(this) as MainWindow)?.NavigateTo("accessibility");
    }

    private void OpenUsbMaker(object sender, MouseButtonEventArgs e)
    {
        (Window.GetWindow(this) as MainWindow)?.NavigateTo("usbmaker");
    }

    private void SignOut(object sender, MouseButtonEventArgs e)
    {
        var result = MessageBox.Show("Sign out and return to login screen?", "Sign Out",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            var main = Window.GetWindow(this) as MainWindow;
            if (main != null)
            {
                var login = new LoginWindow(_profileManager, _accountManager);
                login.Owner = main;
                login.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                login.ShowDialog();
                if (login.AuthenticatedProfile != null)
                    main.UpdateProfileUI(login.AuthenticatedProfile);
            }
        }
    }

    private void Wallpaper_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border b && b.Tag is string name)
        {
            WallpaperManager.SetCurrent(ConfigReader.RootPath ?? "", name);
            SettingsStore.Set("display.wallpaper", name);
            ApplyWallpaper();
            WallpaperInfo.Text = name.ToUpperInvariant();
        }
    }

    private void SetCustomWallpaper(object sender, MouseButtonEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Images (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp",
            Title = "Choose a wallpaper image"
        };
        if (dlg.ShowDialog(Window.GetWindow(this)) == true)
        {
            WallpaperManager.SetCurrent(ConfigReader.RootPath ?? "", dlg.FileName);
            SettingsStore.Set("display.wallpaper", dlg.FileName);
            ApplyWallpaper();
            WallpaperInfo.Text = System.IO.Path.GetFileName(dlg.FileName);
        }
    }

    private void ApplyWallpaper()
    {
        (Window.GetWindow(this) as MainWindow)?.RefreshWallpaper();
    }

    // ---- Lock screen / PIN ----

    private void InitLockSettings()
    {
        var hasPin = !string.IsNullOrWhiteSpace(SettingsStore.Get("lock.pin"));
        UpdatePinStatus(hasPin);
        AutoLockCheck.IsChecked = SettingsStore.GetBool("lock.enabled", false);
        var timeout = SettingsStore.GetInt("lock.timeout_minutes", 5);
        var idx = timeout switch
        {
            1 => 0,
            5 => 1,
            15 => 2,
            30 => 3,
            60 => 4,
            _ => 1,
        };
        LockTimeoutCombo.SelectedIndex = idx;
    }

    private void UpdatePinStatus(bool hasPin)
    {
        PinStatusText.Text = hasPin
            ? "PIN set - lock screen enabled"
            : "No PIN set - lock screen disabled";
        ClearPinBtnText.Text = hasPin ? "REMOVE PIN" : "REMOVE PIN";
    }

    private void PinInput_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        e.Handled = !e.Text.All(char.IsDigit);
    }

    private void PinInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
            SetPin_Click(sender!, new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left));
    }

    private void SetPin_Click(object sender, MouseButtonEventArgs e)
    {
        var pin = PinInput.Text.Trim();
        if (pin.Length != 4 || !pin.All(char.IsDigit))
        {
            PinStatusText.Text = "PIN must be exactly 4 digits";
            PinStatusText.Foreground = TryFindResource("BrushError") as Brush;
            return;
        }
        SettingsStore.Set("lock.pin", pin);
        SettingsStore.SetBool("lock.enabled", true);
        PinStatusText.Foreground = TryFindResource("BrushTextSecondary") as Brush;
        PinStatusText.Text = "PIN set - lock screen enabled";
        PinInput.Text = "";
        AutoLockCheck.IsChecked = true;
        ShowKbNotification("PIN saved. Press Ctrl+L to test the lock screen.");
    }

    private void ClearPin_Click(object sender, MouseButtonEventArgs e)
    {
        SettingsStore.Set("lock.pin", "");
        SettingsStore.SetBool("lock.enabled", false);
        PinStatusText.Foreground = TryFindResource("BrushTextSecondary") as Brush;
        PinStatusText.Text = "No PIN set - lock screen disabled";
        AutoLockCheck.IsChecked = false;
        ShowKbNotification("Lock screen PIN removed.");
    }

    private void AutoLock_Changed(object sender, RoutedEventArgs e)
    {
        SettingsStore.SetBool("lock.enabled", AutoLockCheck.IsChecked == true);
        var hasPin = !string.IsNullOrWhiteSpace(SettingsStore.Get("lock.pin"));
        if (AutoLockCheck.IsChecked == true && !hasPin)
        {
            AutoLockCheck.IsChecked = false;
            SettingsStore.SetBool("lock.enabled", false);
            PinStatusText.Foreground = TryFindBrush("BrushError") ?? Brushes.Red;
            PinStatusText.Text = "Set a PIN first to enable auto-lock";
        }
        else
        {
            PinStatusText.Foreground = TryFindBrush("BrushTextSecondary") ?? Brushes.Gray;
            PinStatusText.Text = AutoLockCheck.IsChecked == true && hasPin
                ? "PIN set - lock screen enabled"
                : "No PIN set - lock screen disabled";
        }
    }

    private void LockTimeout_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (LockTimeoutCombo.SelectedItem is ComboBoxItem item && item.Tag is string v)
        {
            SettingsStore.SetInt("lock.timeout_minutes", int.Parse(v));
        }
    }

    private static Brush? TryFindBrush(string key)
        => System.Windows.Application.Current.TryFindResource(key) as Brush;

    private void ShowKbNotification(string message)
    {
        (System.Windows.Window.GetWindow(this) as MainWindow)?.ShowNotification(message, 3);
    }
}
