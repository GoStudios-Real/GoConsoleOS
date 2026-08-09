using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GoConsoleOS.Shared;

namespace GoConsoleOS.GoConsole.Views;

public partial class TokenCreatorView : UserControl
{
    private const int DefaultPort = 53178;

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

    private void SaveConfig(string token)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);
            var json = JsonSerializer.Serialize(new
            {
                token,
                tokenType = "user"
            });
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

        var clientId = ClientIdBox.Text.Trim();
        if (!Regex.IsMatch(clientId, @"^\d{10,}$"))
        {
            StatusText.Text = "Client ID must be numeric";
            return;
        }

        var redirect = RedirectBox.Text.Trim();
        if (!Uri.TryCreate(redirect, UriKind.Absolute, out var uri) ||
            (uri.Host != "localhost" && uri.Host != "127.0.0.1"))
        {
            StatusText.Text = "Redirect URI must be http://localhost:PORT/";
            return;
        }

        var port = uri.Port > 0 ? uri.Port : DefaultPort;

        var scopes = new List<string>();
        if (ScopeProfile.IsChecked == true) scopes.Add("identify");
        if (ScopeGuilds.IsChecked == true) scopes.Add("guilds");
        if (ScopeGuildsJoin.IsChecked == true) scopes.Add("guilds.join");
        if (ScopeGuildsMembers.IsChecked == true) scopes.Add("guilds.members.read");
        if (ScopeVoice.IsChecked == true) scopes.Add("activities.write");
        var scope = string.Join(" ", scopes);

        if (!TokenCapture.Start(port, token => OnTokenCaptured(token)))
        {
            StatusText.Text = $"Could not listen on port {port}";
            return;
        }

        var url = "https://discord.com/api/oauth2/authorize?" +
                  $"client_id={clientId}&response_type=token" +
                  $"&redirect_uri={Uri.EscapeDataString(redirect)}&scope={Uri.EscapeDataString(scope)}";

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
            SaveConfig(token);
            StatusText.Text = "Token received and saved!";
            StatusChip.Text = "SAVED";
            StatusChip.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(88, 197, 101));
            GoToDiscord.Visibility = Visibility.Visible;
            _main?.NavigateTo("tokencreator");
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
