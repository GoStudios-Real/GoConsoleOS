using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GoConsoleOS.Shared;

namespace GoConsoleOS.GoConsole.Views;

public partial class TokenCreatorView : UserControl
{
    private const int DefaultPort = 53178;
    private const string ClientId = "1533307719748943942";

    private string _configPath = "";
    private MainWindow? _main;

    public TokenCreatorView()
    {
        InitializeComponent();
        _configPath = Path.Combine(ConfigReader.RootPath ?? "", "system", "discord", "config.json");
        LoadConfig();
    }

    private void LoadConfig()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(_configPath));
                if (doc.RootElement.TryGetProperty("token", out var t))
                {
                    var tok = t.GetString();
                    if (!string.IsNullOrEmpty(tok))
                    {
                        StatusText.Text = "A token is already saved";
                        StatusChip.Text = "SAVED";
                        StatusChip.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(88, 197, 101));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"Token config load: {ex.Message}");
        }
    }

    private void SaveConfig(string token, string tokenType = "user")
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);
            var json = JsonSerializer.Serialize(new { token, tokenType });
            File.WriteAllText(_configPath, json);
        }
        catch (Exception ex)
        {
            Logger.Warn($"Token config save: {ex.Message}");
        }
    }

    private void OpenAuth_Click(object sender, MouseButtonEventArgs e)
    {
        if (TokenCapture.IsPending)
        {
            StatusText.Text = "Authorization is already waiting";
            return;
        }

        if (!TokenCapture.Start(DefaultPort, token => OnTokenCaptured(token)))
        {
            StatusText.Text = $"Could not listen on port {DefaultPort}";
            return;
        }

        var url = "https://discord.com/oauth2/authorize?" +
                  $"client_id={ClientId}" +
                  $"&response_type=token" +
                  $"&redirect_uri={Uri.EscapeDataString("http://localhost:53178/")}" +
                  $"&scope=identify+guilds+guilds.join+connections+email";

        if (Window.GetWindow(this) is MainWindow main)
        {
            _main = main;
            main.NavigateToBrowser(url);
        }

        StatusText.Text = "Waiting for authorization in GoBrowser...";
        StatusChip.Text = "WAITING";
        StatusChip.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 193, 7));
    }

    private void OnTokenCaptured(string token)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            SaveConfig(token, "user");
            StatusText.Text = "Token received and saved! Connecting...";
            StatusChip.Text = "CONNECTED";
            StatusChip.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(88, 197, 101));
            GoToDiscord.Visibility = Visibility.Visible;

            if (_main != null)
            {
                _main.NavigateTo("discord");
                Dispatcher.BeginInvoke(new Action(async () =>
                {
                    await System.Threading.Tasks.Task.Delay(1500);
                    if (_main.MainContent.Content is DiscordView discord)
                        discord.AutoConnect();
                }));
            }
        }));
    }

    private void CancelAuth_Click(object sender, MouseButtonEventArgs e)
    {
        TokenCapture.Stop();
        StatusText.Text = "Cancelled";
        StatusChip.Text = "READY";
        StatusChip.Foreground = (System.Windows.Media.Brush)FindResource("BrushTextMuted");
    }

    private void GoToDiscord_Click(object sender, MouseButtonEventArgs e)
    {
        if (Window.GetWindow(this) is MainWindow main)
            main.NavigateTo("discord");
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (!TokenCapture.IsPending)
            TokenCapture.Stop();
    }
}
