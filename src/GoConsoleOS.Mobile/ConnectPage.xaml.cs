using GoConsoleOS.Mobile.Pages;

namespace GoConsoleOS.Mobile.Pages;

public partial class ConnectPage : ContentPage
{
    public ConnectPage()
    {
        InitializeComponent();
    }

    private async void OnConnect(object sender, EventArgs e)
    {
        var host = HostEntry.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(host))
        {
            StatusLabel.Text = "Enter the console IP address.";
            return;
        }

        if (!host.Contains("://"))
            host = "http://" + host;

        var port = PortEntry.Text?.Trim() ?? "39210";
        var url = host.EndsWith(":" + port) || host.Contains(":")
            ? host.TrimEnd('/') + "/"
            : host.TrimEnd('/') + ":" + port + "/";

        StatusLabel.Text = "Connecting...";

        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            var resp = await client.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
            {
                StatusLabel.Text = $"Server responded {resp.StatusCode}. Check the console is on.";
                return;
            }
            Preferences.Default.Set("ConsoleUrl", url);
            await Navigation.PushAsync(new PortalPage(url));
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Could not reach {url}. {ex.Message}";
        }
    }
}