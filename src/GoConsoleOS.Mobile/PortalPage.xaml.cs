namespace GoConsoleOS.Mobile.Pages;

public partial class PortalPage : ContentPage
{
    private readonly string _url;

    public PortalPage(string url)
    {
        InitializeComponent();
        _url = url;
        PortalWebView.Source = url;
    }

    private void OnBack(object sender, EventArgs e)
    {
        if (PortalWebView.CanGoBack)
            PortalWebView.GoBack();
        else
            Navigation.PopAsync();
    }

    private void OnReload(object sender, EventArgs e)
    {
        PortalWebView.Reload();
    }

    private void OnNavigating(object sender, WebNavigatingEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Url)) return;
        if (e.Url.StartsWith("http://") || e.Url.StartsWith("https://"))
        {
            var allowed = new Uri(e.Url).Host;
            if (!e.Url.Contains("localhost") && !e.Url.Contains(_url.Replace("http://", "").Replace("https://", "").Split('/')[0]))
            {
                e.Cancel = true;
            }
        }
    }
}