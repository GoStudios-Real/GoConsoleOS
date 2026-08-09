using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GoConsoleOS.Shared;
using Microsoft.Web.WebView2.Core;

namespace GoConsoleOS.GoConsole.Views;

public partial class BrowserView : UserControl
{
    private bool _started;
    private readonly string _initialUrl;

    public BrowserView(string? initialUrl = null)
    {
        _initialUrl = initialUrl ?? "";
        InitializeComponent();
        if (!string.IsNullOrEmpty(_initialUrl))
            UrlBar.Text = _initialUrl;
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (_started) return;
        _started = true;

        try
        {
            var dataFolder = Path.Combine(ConfigReader.RootPath ?? Path.GetTempPath(), "system", "webview2");
            var env = await WebView2Support.GetEnvironmentAsync();
            await Browser.EnsureCoreWebView2Async(env);
            Logger.Info($"Browser ready, data folder: {dataFolder}");

            Browser.CoreWebView2.NewWindowRequested += (_, args) =>
            {
                args.Handled = true;
                try { Browser.Source = new Uri(args.Uri); } catch { }
            };
            Browser.CoreWebView2.NavigationStarting += (_, args) => UrlText.Text = args.Uri;
            Browser.CoreWebView2.SourceChanged += (_, _) => UrlText.Text = Browser.Source?.ToString() ?? "";
            Browser.CoreWebView2.NavigationCompleted += (_, args) =>
            {
                if (!args.IsSuccess)
                {
                    UrlText.Text = "Failed to load page";
                    UrlText.Foreground = Brushes.Red;
                }
            };

            if (!string.IsNullOrEmpty(UrlBar.Text) && Uri.TryCreate(UrlBar.Text, UriKind.Absolute, out _))
                Browser.Source = new Uri(UrlBar.Text);
        }
        catch (Exception ex)
        {
            Logger.Warn($"Browser init failed: {ex}");
            UrlText.Text = $"Browser engine unavailable: {ex.Message}";
            UrlText.Foreground = Brushes.Red;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        try
        {
            Browser.Dispose();
            Logger.Info("Browser disposed on unload");
        }
        catch (Exception ex)
        {
            Logger.Warn($"Browser dispose: {ex.Message}");
        }
        WebView2Support.KillOwnProcesses();
    }

    private void UrlBar_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            Navigate();
    }

    private void NavigateUrl(object sender, MouseButtonEventArgs e) => Navigate();

    private void OpenKeyboard(object sender, MouseButtonEventArgs e)
    {
        UrlBar.Focus();
        (Window.GetWindow(this) as MainWindow)?.OpenOnScreenKeyboard();
    }

    private void Navigate()
    {
        var url = UrlBar.Text.Trim();
        if (string.IsNullOrEmpty(url)) return;

        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            url = "https://" + url;

        try
        {
            Browser.Source = new Uri(url);
            UrlText.Text = url;
            UrlText.Foreground = (Brush)FindResource("BrushTextSecondary");
        }
        catch
        {
            UrlText.Text = "Invalid URL";
            UrlText.Foreground = Brushes.Red;
        }
    }

    private void BrowserBack(object sender, MouseButtonEventArgs e)
    {
        if (Browser.CanGoBack)
            Browser.GoBack();
    }

    private void BrowserForward(object sender, MouseButtonEventArgs e)
    {
        if (Browser.CanGoForward)
            Browser.GoForward();
    }

    private void BrowserHome(object sender, MouseButtonEventArgs e)
    {
        Browser.Source = new Uri("https://www.google.com");
    }

    private void BrowserRefresh(object sender, MouseButtonEventArgs e)
    {
        try { Browser.Reload(); } catch { }
    }
}
