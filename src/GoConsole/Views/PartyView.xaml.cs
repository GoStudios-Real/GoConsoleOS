using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GoConsoleOS.GoConsole.Views;

public partial class PartyView : UserControl
{
    private string? _currentParty;
    private List<string> _members = new();

    public PartyView()
    {
        InitializeComponent();
        MaxMembersSlider.ValueChanged += (s, e) =>
            MaxMembersLabel.Text = $"{(int)e.NewValue} members";
    }

    private void CreateParty(object sender, RoutedEventArgs e)
    {
        var name = PartyNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ToastManager.Show("Enter a party name");
            return;
        }

        var max = (int)MaxMembersSlider.Value;
        _currentParty = name;
        _members = new List<string> { "You (Host)" };
        PartyStatus.Text = $"🎉 {name} ({max} max)";
        MemberList.ItemsSource = _members.ToList();
        LeavePartyBtn.IsEnabled = true;
        ToastManager.Show($"Party '{name}' created!");
    }

    private void LeaveParty(object sender, RoutedEventArgs e)
    {
        _currentParty = null;
        _members.Clear();
        PartyStatus.Text = "No active party";
        MemberList.ItemsSource = null;
        LeavePartyBtn.IsEnabled = false;
        ToastManager.Show("Left the party");
    }
}
