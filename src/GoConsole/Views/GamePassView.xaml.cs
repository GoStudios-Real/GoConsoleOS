using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GoConsoleOS.Shared;
using GoConsoleOS.Shared.Acc;
using GoConsoleOS.Shared.Models;

namespace GoConsoleOS.GoConsole.Views;

public partial class GamePassView : UserControl
{
    private readonly AccStore? _store;
    private readonly UserProfile? _profile;
    private AccUser? _consoleAccount;
    private string _selectedPlanId = "pro";
    private readonly Dictionary<string, string> _planColors = new()
    {
        ["free"] = "#4A4F5A",
        ["pro"] = "#2ECC71",
        ["plus"] = "#3D9BFF",
        ["premium"] = "#7C5CFF",
        ["ultimate"] = "#FFC800",
    };

    public GamePassView() : this(null, null) { }

    public GamePassView(AccStore? store, UserProfile? profile)
    {
        InitializeComponent();
        _store = store;
        _profile = profile;
        LoadPlans();
        LoadSubscription();
    }

    private void LoadPlans()
    {
        var plans = GamePassCatalog.Plans
            .Where(p => p.Id != "free")
            .Select(p => new GamePassPlanVm
            {
                Id = p.Id,
                Name = p.Name,
                Emoji = p.Emoji,
                PerksText = string.Join("\n", p.Perks),
            })
            .ToList();
        PlanList.ItemsSource = plans;
    }

    private void LoadSubscription()
    {
        try
        {
            if (_store == null)
            {
                PassStatus.Text = "Game Pass unavailable (ACC server off)";
                ActiveTierName.Text = "Game Pass Free";
                ActiveTierBadge.Text = "FREE PLAN";
                return;
            }

            _consoleAccount = _store.GetOrCreateConsoleAccount();
            var active = _consoleAccount.Subscriptions
                .FirstOrDefault(s => s.IsActive && (s.ExpiresAt == null || s.ExpiresAt >= DateTime.UtcNow));
            var plan = GamePassCatalog.Find(active?.Plan);

            ActiveTierEmoji.Text = plan.Emoji;
            ActiveTierName.Text = plan.Name;
            ActiveTierBadge.Text = plan.Id.ToUpperInvariant() + " PLAN";
            ActiveTierBadge.Foreground = (Brush)new BrushConverter().ConvertFromString(plan.Color);

            if (active?.ExpiresAt is DateTime exp)
            {
                var remaining = exp - DateTime.UtcNow;
                ActiveTierDetails.Text =
                    $"Active since {active.StartedAt:dd MMM yyyy}. Expires {exp:dd MMM yyyy} " +
                    $"({remaining.Days} day{(remaining.Days == 1 ? "" : "s")} left)";
                ActiveTierExpiry.Text = $"{remaining.Days}D LEFT";
            }
            else
            {
                ActiveTierDetails.Text = "On the free plan. Pick a tier below or redeem a gift card to unlock Game Pass.";
                ActiveTierExpiry.Text = "";
            }

            PassStatus.Text = $"Current plan: {plan.Name}";
        }
        catch (Exception ex)
        {
            Logger.Warn($"GamePass view: {ex.Message}");
            PassStatus.Text = "Game Pass unavailable";
        }
    }

    private void ChoosePlan(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string planId)
        {
            _selectedPlanId = planId;
            var plan = GamePassCatalog.Find(planId);
            ToastManager.Show($"Selected {plan.Name}. Press SUBSCRIBE to activate.");
        }
    }

    private void RedeemGift_Click(object sender, MouseButtonEventArgs e)
    {
        var code = GiftCodeBox.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(code))
        {
            RedeemStatus.Text = "Enter a gift card code first.";
            return;
        }
        if (_store == null || _consoleAccount == null)
        {
            RedeemStatus.Text = "Gift cards are unavailable (ACC server off).";
            return;
        }

        var (ok, msg, _) = _store.RedeemGiftCard(_consoleAccount, code);
        RedeemStatus.Text = msg;
        if (ok)
        {
            SoundManager.Play("install");
            GiftCodeBox.Text = "";
            LoadSubscription();
        }
        else
        {
            SoundManager.Play("error");
        }
    }

    private void SubscribeNow_Click(object sender, MouseButtonEventArgs e)
    {
        if (_store == null || _consoleAccount == null)
        {
            ToastManager.Show("Game Pass is unavailable (ACC server off)");
            return;
        }

        var amount = int.TryParse(AmountBox.Text, out var a) ? Math.Max(1, a) : 1;
        var unit = (UnitBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Days";
        var days = unit switch
        {
            "Months" => amount * 30,
            "Years" => amount * 365,
            _ => amount,
        };

        // plan is whatever the last picked plan card was
        var planId = _selectedPlanId;

        var plan = GamePassCatalog.Find(planId);
        _store.AddSubscription(_consoleAccount, planId, days, "manual");
        LoadSubscription();
        SoundManager.Play("install");
        ToastManager.Show($"Subscribed to {plan.Name} for {amount} {unit.ToLowerInvariant()}!");
    }

    public class GamePassPlanVm
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Emoji { get; set; } = "🎮";
        public string PerksText { get; set; } = "";
    }
}