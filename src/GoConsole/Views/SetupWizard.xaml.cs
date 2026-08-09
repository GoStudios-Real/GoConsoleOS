using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GoConsoleOS.Shared;
using GoConsoleOS.Shared.Models;

namespace GoConsoleOS.GoConsole.Views;

public partial class SetupWizard : Window
{
    private readonly ProfileManager _profileManager;
    private readonly AccountManager _accountManager;
    private readonly PerformanceManager _perfManager;
    private readonly InitConfig _config;
    private int _currentStep = 1;
    private const int TotalSteps = 6;

    private string _selectedTheme = "dark";
    private Color _selectedAccent = Color.FromRgb(0x00, 0xC9, 0xDB);
    private string _selectedPerfMode = "balanced";
    private string _selectedResolution = "auto";
    private bool _useGuest = true;
    private string? _registeredUsername;

    public bool Completed { get; private set; }

    public SetupWizard(ProfileManager profileManager, AccountManager accountManager,
                       PerformanceManager perfManager, InitConfig config)
    {
        InitializeComponent();
        _profileManager = profileManager;
        _accountManager = accountManager;
        _perfManager = perfManager;
        _config = config;
        UpdateStep();
    }

    private void UpdateStep()
    {
        Step1Dot.Fill = _currentStep >= 1 ? FindResource("BrushAccentPrimary") as Brush : FindResource("BrushBackgroundCard") as Brush;
        Step2Dot.Fill = _currentStep >= 2 ? FindResource("BrushAccentPrimary") as Brush : FindResource("BrushBackgroundCard") as Brush;
        Step3Dot.Fill = _currentStep >= 3 ? FindResource("BrushAccentPrimary") as Brush : FindResource("BrushBackgroundCard") as Brush;
        Step4Dot.Fill = _currentStep >= 4 ? FindResource("BrushAccentPrimary") as Brush : FindResource("BrushBackgroundCard") as Brush;
        Step5Dot.Fill = _currentStep >= 5 ? FindResource("BrushAccentPrimary") as Brush : FindResource("BrushBackgroundCard") as Brush;
        Step6Dot.Fill = _currentStep >= 6 ? FindResource("BrushSuccess") as Brush : FindResource("BrushBackgroundCard") as Brush;

        Step1Panel.Visibility = _currentStep == 1 ? Visibility.Visible : Visibility.Collapsed;
        Step2Panel.Visibility = _currentStep == 2 ? Visibility.Visible : Visibility.Collapsed;
        Step3Panel.Visibility = _currentStep == 3 ? Visibility.Visible : Visibility.Collapsed;
        Step4Panel.Visibility = _currentStep == 4 ? Visibility.Visible : Visibility.Collapsed;
        Step5Panel.Visibility = _currentStep == 5 ? Visibility.Visible : Visibility.Collapsed;
        Step6Panel.Visibility = _currentStep == 6 ? Visibility.Visible : Visibility.Collapsed;

        PrevBtn.Visibility = _currentStep > 1 && _currentStep < 6 ? Visibility.Visible : Visibility.Collapsed;
        NextBtnText.Text = _currentStep == 6 ? "FINISH →" : _currentStep == 1 ? "GET STARTED →" : "NEXT →";

        StepTitle.Text = _currentStep switch
        {
            1 => "WELCOME",
            2 => "YOUR ACCOUNT",
            3 => "CONTROLLER",
            4 => "DISPLAY & PERFORMANCE",
            5 => "THEME",
            6 => "COMPLETE",
            _ => ""
        };
    }

    private void PreviousStep(object sender, MouseButtonEventArgs e)
    {
        if (_currentStep > 1) _currentStep--;
        UpdateStep();
    }

    private void NextStep(object sender, MouseButtonEventArgs e)
    {
        if (_currentStep == 2) { if (!HandleAccountStep()) return; }
        if (_currentStep == 5) ApplyTheme();

        if (_currentStep < TotalSteps)
        {
            _currentStep++;
            if (_currentStep == TotalSteps) BuildSummary();
            UpdateStep();
        }
        else
        {
            FinishSetup();
        }
    }

    private bool HandleAccountStep()
    {
        var username = SetupUsername.Text.Trim();
        var displayName = SetupDisplayName.Text.Trim();
        var password = SetupPassword.Password;

        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
        {
            if (string.IsNullOrEmpty(displayName)) displayName = username;
            var (success, msg) = _accountManager.Register(username, displayName, password);
            SetupAccountStatus.Text = msg;
            SetupAccountStatus.Foreground = success
                ? FindResource("BrushSuccess") as Brush ?? Brushes.Green
                : FindResource("BrushError") as Brush ?? Brushes.Red;
            if (!success) return false;
            _registeredUsername = username;
            _useGuest = false;
            _accountManager.Login(username, password);
        }
        else
        {
            _useGuest = true;
            _profileManager.GetOrCreateGuestProfile();
        }
        return true;
    }

    private void SkipToGuest(object sender, MouseButtonEventArgs e)
    {
        _useGuest = true;
        _profileManager.GetOrCreateGuestProfile();
        _currentStep++;
        UpdateStep();
    }

    private void SelectPerfMode(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string mode)
        {
            _selectedPerfMode = mode;
            var accent = FindResource("BrushAccentPrimary") as Brush;
            var bg = FindResource("BrushBackgroundCardHover") as Brush;
            var text = FindResource("BrushTextPrimary") as Brush;
            SetupPerfQuiet.Background = mode == "quiet" ? accent : bg;
            SetupPerfBalanced.Background = mode == "balanced" ? accent : bg;
            SetupPerfTurbo.Background = mode == "turbo" ? accent : bg;
            foreach (var tb in new[] { (TextBlock)((Border)SetupPerfQuiet).Child, (TextBlock)((Border)SetupPerfBalanced).Child, (TextBlock)((Border)SetupPerfTurbo).Child })
                tb.Foreground = mode == tb.Name.Replace("SetupPerf", "").ToLower() ? Brushes.Black : text;
        }
    }

    private void SelectTheme(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string theme)
        {
            _selectedTheme = theme;
            var border = FindResource("BrushAccentPrimary") as Brush;
            var none = new SolidColorBrush(Colors.Transparent);
            ThemeDark.BorderBrush = theme == "dark" ? border : none;
            ThemeDarker.BorderBrush = theme == "darker" ? border : none;
            ThemeAmber.BorderBrush = theme == "amber" ? border : none;
        }
    }

    private void SelectAccent(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string hex)
        {
            _selectedAccent = (Color)ColorConverter.ConvertFromString(hex);
            foreach (var acc in new[] { AccentCyan, AccentPurple, AccentPink, AccentGreen, AccentOrange, AccentRed, AccentBlue })
                acc.BorderBrush = acc.Tag?.ToString() == hex ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Transparent);
        }
    }

    private void ApplyTheme()
    {
        var main = Application.Current.Windows.OfType<Window>()
            .FirstOrDefault(w => w.GetType().Name == "MainWindow") as MainWindow;
        main?.UpdateThemeColors(_selectedAccent, _selectedTheme);
    }

    private void BuildSummary()
    {
        SummaryAccount.Text = _useGuest
            ? "Account: Guest (no password)"
            : $"Account: {_registeredUsername ?? SetupUsername.Text}";
        SummaryController.Text = $"Controller: {(SetupMouseEmulation.IsChecked == true ? "Mouse emulation ON" : "Mouse emulation OFF")} | Vibration: {(SetupVibration.IsChecked == true ? "ON" : "OFF")}";
        SummaryDisplay.Text = $"Performance: {_selectedPerfMode.ToUpper()} | Resolution: {_selectedResolution}";
        SummaryTheme.Text = $"Theme: {_selectedTheme.ToUpper()} | Accent: {_selectedAccent.ToString()}";
    }

    private void FinishSetup()
    {
        try
        {
            var flagDir = ConfigReader.ResolvePath("system");
            Directory.CreateDirectory(flagDir);
            File.WriteAllText(Path.Combine(flagDir, ".setup_complete"), DateTime.UtcNow.ToString("o"));
        }
        catch { }

        _perfManager.SetProfile(_selectedPerfMode);
        Completed = true;
        DialogResult = true;
        Close();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            if (MessageBox.Show("Skip setup and continue as guest?", "Skip Setup",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _profileManager.GetOrCreateGuestProfile();
                Completed = true;
                DialogResult = true;
                Close();
            }
        }
    }
}
