using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GoConsoleOS.GoConsole.Views;

public partial class GoAiWindow : Window
{
    private static readonly HttpClient Http = new();
    private const string Endpoint = "http://localhost:39210/api/goai";

    public GoAiWindow()
    {
        InitializeComponent();
        AddBubble("GoAI", "Hi! I'm GoAI. Ask me about games, USB health, performance, or say help.");
        InputBox.Focus();
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e) => await Send();

    private async void InputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await Send();
        }
    }

    private async Task Send()
    {
        var text = InputBox.Text.Trim();
        if (string.IsNullOrEmpty(text)) return;
        InputBox.Text = "";
        AddBubble("You", text);

        try
        {
            var payload = JsonSerializer.Serialize(new { message = text });
            var resp = await Http.PostAsync(Endpoint,
                new StringContent(payload, Encoding.UTF8, "application/json"));
            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var reply = doc.RootElement.TryGetProperty("reply", out var r) ? r.GetString() : "GoAI didn't respond.";
            AddBubble("GoAI", reply ?? "");
        }
        catch (Exception ex)
        {
            AddBubble("GoAI", "I couldn't reach the local assistant server. Is the console running?\n(" + ex.Message + ")");
        }
    }

    private void AddBubble(string who, string message)
    {
        var isUser = who == "You";
        var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 10), HorizontalAlignment =
            isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left };

        var bubble = new Border
        {
            CornerRadius = new CornerRadius(14),
            Background = new SolidColorBrush(isUser
                ? Color.FromRgb(0x00, 0x50, 0x5e)
                : Color.FromRgb(0x1e, 0x1e, 0x32)),
            Padding = new Thickness(14, 10, 14, 10),
            MaxWidth = 420,
        };
        var text = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.FromRgb(0xf0, 0xf0, 0xff)),
            FontSize = 14,
        };
        bubble.Child = text;
        panel.Children.Add(bubble);
        ChatLog.Children.Add(panel);
        Dispatcher.Invoke(() => ChatScroll.ScrollToEnd());
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
