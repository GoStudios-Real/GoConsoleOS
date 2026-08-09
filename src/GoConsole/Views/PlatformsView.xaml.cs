using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GoConsoleOS.Shared;

namespace GoConsoleOS.GoConsole.Views;

public partial class PlatformsView : UserControl
{
    private string _currentPlatform = "steam";
    private bool _initialized;

    private static readonly Dictionary<string, (string Name, string Url)> Platforms = new()
    {
        ["steam"] = ("Steam", "https://store.steampowered.com"),
        ["epic"] = ("Epic Games", "https://store.epicgames.com"),
        ["xbox"] = ("Xbox", "https://www.xbox.com/en-US/games"),
        ["gog"] = ("GOG", "https://www.gog.com"),
        ["playstation"] = ("PlayStation", "https://store.playstation.com/en-us/pages/latest"),
        ["nintendo"] = ("Nintendo", "https://www.nintendo.com/us/store/"),
        ["battlenet"] = ("Battle.net", "https://us.shop.battle.net"),
        ["ea"] = ("EA App", "https://www.ea.com/games"),
        ["ubisoft"] = ("Ubisoft Connect", "https://store.ubi.com"),
        ["itch"] = ("itch.io", "https://itch.io/games"),
    };

    public PlatformsView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (_initialized) return;
        _initialized = true;

        try
        {
            var env = await WebView2Support.GetEnvironmentAsync();
            await PlatformBrowser.EnsureCoreWebView2Async(env);
            PlatformBrowser.CoreWebView2.NewWindowRequested += (_, args) =>
            {
                args.Handled = true;
                try { PlatformBrowser.Source = new Uri(args.Uri); } catch { }
            };
            LoadPlatform("steam");
        }
        catch (Exception ex)
        {
            TabTitle.Text = $"WebView init failed: {ex.Message}";
        }
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        try
        {
            PlatformBrowser.Dispose();
            Logger.Info("Platform browser disposed on unload");
        }
        catch (Exception ex)
        {
            Logger.Warn($"Platform browser dispose: {ex.Message}");
        }
        WebView2Support.KillOwnProcesses();
    }

    public void LoadUrl(string url)
    {
        try
        {
            PlatformBrowser.Source = new Uri(url);
            TabTitle.Text = url.Replace("https://", "").TrimEnd('/');
        }
        catch { }
        ResetTabHighlights();
        TabTitle.Text = url;
    }

    private void ResetTabHighlights()
    {
        var defaultBg = TryFindResource("BrushBackgroundLight") as Brush;
        var defaultText = TryFindResource("BrushTextPrimary") as Brush;
        foreach (var tab in AllTabs())
        {
            tab.Background = defaultBg;
            if (tab.Child is StackPanel sp && sp.Children[1] is TextBlock tb)
                tb.Foreground = defaultText;
        }
    }

    private IEnumerable<Border> AllTabs()
    {
        return new[] { TabSteam, TabEpic, TabXbox, TabGog, TabPlaystation, TabNintendo, TabBattlenet, TabEa, TabUbisoft, TabItch };
    }

    public void LoadPlatform(string platformId)
    {
        if (!Platforms.ContainsKey(platformId)) return;
        _currentPlatform = platformId;

        var (name, url) = Platforms[platformId];
        try
        {
            PlatformBrowser.Source = new Uri(url);
        }
        catch { }
        TabTitle.Text = url.Replace("https://", "").TrimEnd('/');

        // Update tab highlight
        var activeBg = TryFindResource("BrushAccentPrimary") as Brush ?? Brushes.Cyan;
        var darkText = new SolidColorBrush(Color.FromRgb(0x0D, 0x0D, 0x14));
        var defaultBg = TryFindResource("BrushBackgroundLight") as Brush;
        var defaultText = TryFindResource("BrushTextPrimary") as Brush;

        foreach (var tab in AllTabs())
        {
            var active = tab.Tag?.ToString() == platformId;
            tab.Background = active ? activeBg : defaultBg;
            if (tab.Child is StackPanel sp && sp.Children[1] is TextBlock tb)
                tb.Foreground = active ? darkText : defaultText;
        }
    }

    private void SwitchTab(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string id)
            LoadPlatform(id);
    }

    private void NavBack(object sender, MouseButtonEventArgs e)
    {
        if (PlatformBrowser.CoreWebView2 != null)
            PlatformBrowser.CoreWebView2.GoBack();
    }

    private void NavForward(object sender, MouseButtonEventArgs e)
    {
        if (PlatformBrowser.CoreWebView2 != null)
            PlatformBrowser.CoreWebView2.GoForward();
    }

    private void NavRefresh(object sender, MouseButtonEventArgs e)
    {
        if (PlatformBrowser.CoreWebView2 != null)
            PlatformBrowser.CoreWebView2.Reload();
    }
}
