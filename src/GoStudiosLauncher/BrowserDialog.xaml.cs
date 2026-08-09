using System;
using System.Windows;
using System.Windows.Input;

namespace GoStudiosLauncher;

public partial class BrowserDialog : Window
{
    public BrowserDialog()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            try { BrowserControl.Navigate("https://opencode.ai"); }
            catch { }
        };
    }

    private void NavigateGo(object sender, MouseButtonEventArgs e)
        => Navigate();

    private void GoBack(object sender, MouseButtonEventArgs e)
    {
        try { BrowserControl.GoBack(); }
        catch { }
    }

    private void UrlBar_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            Navigate();
    }

    private void Navigate()
    {
        var url = UrlBar.Text.Trim();
        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            url = "https://" + url;
        try { BrowserControl.Navigate(url); }
        catch { }
    }

    private void CloseBrowser(object sender, MouseButtonEventArgs e) => Close();

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) Close();
    }
}
