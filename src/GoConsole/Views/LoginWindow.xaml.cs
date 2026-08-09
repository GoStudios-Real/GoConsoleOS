using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using GoConsoleOS.Shared;
using GoConsoleOS.Shared.Models;

namespace GoConsoleOS.GoConsole.Views;

public partial class LoginWindow : Window
{
    private readonly AccountManager _accountManager;
    private readonly ProfileManager _profileManager;
    private bool _isSignUp;
    private DispatcherTimer? _autoTimer;
    private int _countdown = 8;
    public UserProfile? AuthenticatedProfile { get; private set; }
    public bool IsAuthenticated { get; private set; }

    public LoginWindow(ProfileManager profileManager, AccountManager accountManager)
    {
        InitializeComponent();
        _profileManager = profileManager;
        _accountManager = accountManager;
        ShowSignIn();

        _autoTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _autoTimer.Tick += (_, _) =>
        {
            _countdown--;
            if (_countdown <= 0)
            {
                _autoTimer.Stop();
                ContinueAsGuest(null!, null!);
                return;
            }
            CountdownText.Text = $"Continuing as guest in {_countdown}s...";
        };
        _autoTimer.Start();
    }

    private void ShowSignIn()
    {
        _isSignUp = false;
        SignInPanel.Visibility = Visibility.Visible;
        SignUpPanel.Visibility = Visibility.Collapsed;
        SignInTab.Background = TryFindResource("BrushAccentPrimary") as Brush ?? Brushes.Cyan;
        SignInTabText.Foreground = Brushes.Black;
        SignUpTab.Background = TryFindResource("BrushBackgroundCard") as Brush;
        SignUpTabText.Foreground = TryFindResource("BrushTextSecondary") as Brush;
        SubtitleText.Text = "Sign in to your account";
        StatusText.Text = "";
    }

    private void ShowSignUp()
    {
        _isSignUp = true;
        SignInPanel.Visibility = Visibility.Collapsed;
        SignUpPanel.Visibility = Visibility.Visible;
        SignUpTab.Background = TryFindResource("BrushAccentSecondary") as Brush ?? Brushes.Purple;
        SignUpTabText.Foreground = Brushes.White;
        SignInTab.Background = TryFindResource("BrushBackgroundCard") as Brush;
        SignInTabText.Foreground = TryFindResource("BrushTextSecondary") as Brush;
        SubtitleText.Text = "Create a new account";
        StatusText.Text = "";
    }

    private void SwitchToSignIn(object sender, MouseButtonEventArgs e) => ShowSignIn();
    private void SwitchToSignUp(object sender, MouseButtonEventArgs e) => ShowSignUp();

    private void DoSignIn(object sender, MouseButtonEventArgs e)
    {
        var username = LoginUsername.Text.Trim();
        var password = LoginPassword.Password;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            StatusText.Text = "Please enter both username and password.";
            StatusText.Foreground = TryFindResource("BrushError") as Brush;
            return;
        }

        var (success, message, profile) = _accountManager.Login(username, password);
        StatusText.Text = message;
        StatusText.Foreground = success
            ? TryFindResource("BrushSuccess") as Brush ?? Brushes.Green
            : TryFindResource("BrushError") as Brush ?? Brushes.Red;

        if (success && profile != null)
        {
            AuthenticatedProfile = profile;
            IsAuthenticated = true;
            Close();
        }
    }

    private void DoSignUp(object sender, MouseButtonEventArgs e)
    {
        var username = RegUsername.Text.Trim();
        var displayName = RegDisplayName.Text.Trim();
        var email = RegEmail.Text.Trim();
        var password = RegPassword.Password;
        var confirm = RegPasswordConfirm.Password;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(displayName))
        {
            StatusText.Text = "Username and display name are required.";
            StatusText.Foreground = TryFindResource("BrushError") as Brush;
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            StatusText.Text = "Password cannot be empty.";
            StatusText.Foreground = TryFindResource("BrushError") as Brush;
            return;
        }

        if (password != confirm)
        {
            StatusText.Text = "Passwords do not match.";
            StatusText.Foreground = TryFindResource("BrushError") as Brush;
            return;
        }

        if (string.IsNullOrEmpty(email))
            email = null;

        var (success, message) = _accountManager.Register(username, displayName, password, email);
        StatusText.Text = message;
        StatusText.Foreground = success
            ? TryFindResource("BrushSuccess") as Brush ?? Brushes.Green
            : TryFindResource("BrushError") as Brush ?? Brushes.Red;

        if (success)
        {
            var (_, _, profile) = _accountManager.Login(username, password);
            AuthenticatedProfile = profile;
            IsAuthenticated = true;
            Close();
        }
    }

    private void ContinueAsGuest(object sender, MouseButtonEventArgs e)
    {
        _autoTimer?.Stop();
        AuthenticatedProfile = _profileManager.GetOrCreateGuestProfile();
        IsAuthenticated = false;
        Close();
    }

    private void CloseWindow(object sender, MouseButtonEventArgs e)
    {
        _autoTimer?.Stop();
        Close();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            _autoTimer?.Stop();
            Close();
        }
        if (e.Key == Key.Enter)
        {
            if (_isSignUp) DoSignUp(null!, null!);
            else DoSignIn(null!, null!);
        }
    }
}
