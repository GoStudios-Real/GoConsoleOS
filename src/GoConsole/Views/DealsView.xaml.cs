using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GoConsoleOS.Shared;
using GoConsoleOS.Shared.Models;

namespace GoConsoleOS.GoConsole.Views;

public partial class DealsView : UserControl
{
    private readonly LibraryData _library = new();
    private readonly Random _rng = new();
    private bool _alertsEnabled = true;
    private readonly List<string> _wishlist = new();

    public DealsView()
    {
        InitializeComponent();
        LoadLibrary();
        LoadWishlist();
        LoadDeals();
    }

    private void LoadLibrary()
    {
        try
        {
            var path = Path.Combine(ConfigReader.RootPath ?? "", "launcher", "library", "library.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                _library.Games = System.Text.Json.JsonSerializer.Deserialize<List<GameInfo>>(json) ?? new();
            }
        }
        catch { }
    }

    private void LoadWishlist()
    {
        try
        {
            var path = Path.Combine(ConfigReader.RootPath ?? "", "system", "wishlist.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var list = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json);
                if (list != null) _wishlist.AddRange(list);
            }
        }
        catch { }
    }

    private void LoadDeals()
    {
        var deals = new List<DealItem>();
        var games = _library.Games
            .Where(g => g.IsInstalled || _wishlist.Contains(g.Id))
            .OrderBy(g => g.Title)
            .ToList();

        foreach (var game in games.Take(20))
        {
            var discountPct = 10 + _rng.Next(6) * 10;
            var price = 5 + _rng.Next(55);
            deals.Add(new DealItem
            {
                Title = game.Title,
                Platform = game.Platform,
                OldPrice = $"\u20AC{price:N2}",
                NewPrice = $"\u20AC{price * (1 - discountPct / 100.0):N2}",
                Discount = $"-{discountPct}%",
                StoreUrl = game.GetStoreUrl()
            });
        }

        if (deals.Count == 0)
        {
            deals.Add(new DealItem
            {
                Title = "No deals available",
                Platform = "Scan your library to find active sales",
                OldPrice = "",
                NewPrice = "",
                Discount = "",
                StoreUrl = ""
            });
        }

        DealList.ItemsSource = deals;
        DealCount.Text = $"{deals.Count(d => !string.IsNullOrEmpty(d.Discount))} deals active";
        WishlistCount.Text = $"{_wishlist.Count} wishlisted";
    }

    private void ToggleAlerts(object sender, MouseButtonEventArgs e)
    {
        _alertsEnabled = !_alertsEnabled;
        AlertText.Text = _alertsEnabled ? "🔔 ALERTS: ON" : "🔕 ALERTS: OFF";
        AlertToggle.Background = _alertsEnabled
            ? FindResource("BrushAccentPrimary") as System.Windows.Media.Brush
            : FindResource("BrushBackgroundCard") as System.Windows.Media.Brush;
        AlertText.Foreground = _alertsEnabled
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x0D, 0x0D, 0x14))
            : FindResource("BrushTextSecondary") as System.Windows.Media.Brush;
        ToastManager.Show(_alertsEnabled ? "Deal alerts enabled" : "Deal alerts disabled");
    }

    private void VisitDeal(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is string url && !string.IsNullOrEmpty(url))
        {
            var main = Application.Current.Windows.OfType<Window>()
                .FirstOrDefault(w => w is MainWindow) as MainWindow;
            if (main != null)
            {
                var platformView = new PlatformsView();
                platformView.LoadUrl(url);
                main.ShowPlatformView(platformView);
            }
        }
    }

    public class DealItem
    {
        public string Title { get; set; } = "";
        public string Platform { get; set; } = "";
        public string OldPrice { get; set; } = "";
        public string NewPrice { get; set; } = "";
        public string Discount { get; set; } = "";
        public string StoreUrl { get; set; } = "";
    }
}
