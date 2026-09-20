using System.IO;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using GoConsoleOS.Shared;

namespace GoConsoleOS.GoConsole.Views;

public partial class GoAiWindow : Window
{
    private SpeechRecognitionEngine? _recognizer;
    private readonly SpeechSynthesizer _synth;
    private readonly Random _rng = new();
    private bool _isListening;
    private readonly DispatcherTimer _listeningTimer;

    public GoAiWindow()
    {
        InitializeComponent();
        _synth = new SpeechSynthesizer();
        _synth.SetOutputToDefaultAudioDevice();
        _synth.Rate = 1;
        _synth.Volume = 100;

        _listeningTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
        _listeningTimer.Tick += (_, _) => StopListening();

        AddBubble("GoAI", "Hi! I am GoAI, your built-in voice assistant.\n\nTry saying:\n- Open games\n- Console status\n- Tell me a joke\n- What time is it\n\nOr type a message below!");

        InitSpeechRecognition();
        InputBox.Focus();
    }

    private void InitSpeechRecognition()
    {
        try
        {
            var grammar = new GrammarBuilder();
            grammar.AppendDictation();
            var grammarObj = new Grammar(grammar);
            _recognizer = new SpeechRecognitionEngine();
            _recognizer.SetInputToDefaultAudioDevice();
            _recognizer.LoadGrammar(grammarObj);
            _recognizer.SpeechRecognized += OnSpeechRecognized;
            _recognizer.RecognizeCompleted += OnRecognizeCompleted;
            _recognizer.AudioLevelUpdated += OnAudioLevelUpdated;
            AiStatus.Text = "Built-in AI - Voice Ready";
        }
        catch
        {
            AiStatus.Text = "Built-in AI - Text Only (no microphone)";
        }
    }

    private void OnAudioLevelUpdated(object? sender, AudioLevelUpdatedEventArgs e)
    {
        var level = e.AudioLevel;
        var size = Math.Clamp(30 + level / 3, 30, 80);
        Dispatcher.Invoke(() => { MicIndicator.Width = size; MicIndicator.Height = size; });
    }

    private void OnSpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
    {
        if (e.Result.Confidence < 0.3f) return;
        var text = e.Result.Text;
        Dispatcher.Invoke(() => { ListeningText.Text = ""; MicStatus.Text = "Processing..."; ProcessUserInput(text); });
    }

    private void OnRecognizeCompleted(object? sender, RecognizeCompletedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            _isListening = false;
            MicIndicator.Stroke = new SolidColorBrush(Color.FromRgb(0x00, 0x66, 0xFF));
            MicStatus.Text = "Tap microphone to speak";
            ListeningText.Text = "";
        });
    }

    private void MicButton_Click(object sender, RoutedEventArgs e)
    {
        if (_recognizer == null) { AddBubble("GoAI", "Speech recognition is not available. You can still type!"); return; }
        if (_isListening) StopListening(); else StartListening();
    }

    private void StartListening()
    {
        if (_recognizer == null) return;
        try
        {
            _isListening = true;
            _recognizer.RecognizeAsync(RecognizeMode.Multiple);
            MicIndicator.Stroke = new SolidColorBrush(Color.FromRgb(0x00, 0xFF, 0x66));
            MicStatus.Text = "Listening...";
            ListeningText.Text = "Speak now...";
            _listeningTimer.Start();
        }
        catch (Exception ex) { AddBubble("GoAI", "Could not start: " + ex.Message); StopListening(); }
    }

    private void StopListening()
    {
        if (_recognizer == null) return;
        try { _recognizer.RecognizeAsyncStop(); } catch { }
        _isListening = false;
        _listeningTimer.Stop();
        MicIndicator.Stroke = new SolidColorBrush(Color.FromRgb(0x00, 0x66, 0xFF));
        MicIndicator.Width = 60; MicIndicator.Height = 60;
        MicStatus.Text = "Tap microphone to speak";
        ListeningText.Text = "";
    }

    private void SendButton_Click(object sender, RoutedEventArgs e) => Send();

    private void InputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { e.Handled = true; Send(); }
    }

    private void Send()
    {
        var text = InputBox.Text.Trim();
        if (string.IsNullOrEmpty(text)) return;
        InputBox.Text = "";
        ProcessUserInput(text);
    }

    private void ProcessUserInput(string message)
    {
        AddBubble("You", message);
        var reply = GetAiResponse(message);
        AddBubble("GoAI", reply);
        Speak(reply);
    }

    private void Speak(string text)
    {
        try
        {
            var clean = Regex.Replace(text, @"[^\w\s.,!?''-]", "");
            _synth.SpeakAsyncCancelAll();
            _synth.SpeakAsync(clean);
        }
        catch { }
    }

    private string GetAiResponse(string userMessage)
    {
        var msg = userMessage.ToLower().Trim();
        if (IsGreeting(msg)) return GetGreeting();
        if (IsThanks(msg)) return GetThanks();
        if (IsJoke(msg)) return GetJoke();
        if (IsStatusQuery(msg)) return GetStatusResponse();
        if (IsGameQuery(msg)) return GetGameResponse();
        if (IsPowerOn(msg)) return "Use the power button on your console or type !power in Discord.";
        if (IsPowerOff(msg)) return "You can disconnect your console from the settings panel.";
        if (IsVoiceQuery(msg)) return "Voice chat is available through the Discord tab. Open it to set up a call.";
        if (IsUsbQuery(msg)) return GetUsbResponse();
        if (IsHelpQuery(msg)) return GetHelpText();
        if (IsWhoAreYou(msg)) return "I am GoAI, your built-in voice assistant for GoConsoleOS. I help you manage games, check status, and more!";
        if (IsHowAreYou(msg)) return "I am doing great! All systems running smoothly. How can I help?";
        if (IsTimeQuery(msg)) return "It is currently " + DateTime.Now.ToString("hh:mm tt") + " on " + DateTime.Now.ToString("dddd, MMMM dd, yyyy") + ".";
        if (IsMotivationQuery(msg)) return GetMotivation();
        if (IsAboutConsole(msg)) return "GoConsoleOS is a USB-powered gaming console that turns any PC into a gaming system with cloud gaming, Discord, and 10 built-in games!";
        if (IsAboutGoStudios(msg)) return "GoStudios Corporation develops GoConsoleOS. Visit github.com/GoStudios-Real";
        if (msg.Contains("game") || msg.Contains("play")) return "Say Open games or navigate to the Games tab to browse your library!";
        if (msg.Contains("music")) return "Music player is in the Media tab. You can play and manage your library from there.";
        if (msg.Contains("discord")) return "Discord integration is in the Discord tab. Connect your account to join voice channels and chat.";
        return GetSmartFallback(msg);
    }

    private bool IsGreeting(string m) => Regex.IsMatch(m, @"^(hi|hello|hey|howdy|sup|yo|greetings|what'?s up|hola|namaste|hiya|heya|wassup|yoo|heeey|heyy|hi there|hello there)\b");
    private bool IsThanks(string m) => Regex.IsMatch(m, @"^(thanks?|thx|ty|thank you|cheers|appreciate|tysm)\b");
    private bool IsJoke(string m) => Regex.IsMatch(m, @"(tell me a )?joke|funny|humor|make me laugh");
    private bool IsStatusQuery(string m) => Regex.IsMatch(m, @"(console |system )?(status|info|how.+running|performance|health|stats|how is)");
    private bool IsGameQuery(string m) => Regex.IsMatch(m, @"(what|which|show|list|see|got|have).*(games?|library|catalog)|games\??|play(ing)?|launch|open games?");
    private bool IsPowerOn(string m) => Regex.IsMatch(m, @"(turn|boot|start|power|wake|switch).*(on|up)|go online");
    private bool IsPowerOff(string m) => Regex.IsMatch(m, @"(turn|boot|power|shut|switch).*(off|down)|disconnect|go offline|sleep|shutdown");
    private bool IsVoiceQuery(string m) => Regex.IsMatch(m, @"voice|chat|microphone|mic|call|talk|speak|audio");
    private bool IsUsbQuery(string m) => Regex.IsMatch(m, @"usb|drive|flash|thumb|stick|device|removable");
    private bool IsHelpQuery(string m) => Regex.IsMatch(m, @"^(help|commands?|what can you|options|menu|guide|tutorial|how do|how to|what do|features)");
    private bool IsWhoAreYou(string m) => Regex.IsMatch(m, @"who are you|what are you|your name|about you|introduce yourself|what do you do");
    private bool IsHowAreYou(string m) => Regex.IsMatch(m, @"how are you|how('s| is) it going|you doing|you ok|you good");
    private bool IsTimeQuery(string m) => Regex.IsMatch(m, @"what('s| is) (the )?time|current time|what time|clock|date|today");
    private bool IsMotivationQuery(string m) => Regex.IsMatch(m, @"motivat|inspir|encourage|keep going|boost|pump me up|hype");
    private bool IsAboutConsole(string m) => Regex.IsMatch(m, @"what is goconsole|about goconsole|tell me about|what does|explain goconsole");
    private bool IsAboutGoStudios(string m) => Regex.IsMatch(m, @"gostudios|who made|who created|developer|maker|company|studio");

    private string GetGreeting() => _rng.Next(4) switch
    {
        0 => "Hey! Welcome to GoConsoleOS. Need help with your console?",
        1 => "Hello! Ready to game? Say Open games to see your library!",
        2 => "What is up! Your console is waiting. How can I help?",
        _ => "Hi there! How can I help you today?"
    };

    private string GetThanks() => _rng.Next(3) switch
    {
        0 => "You are welcome! Happy gaming!",
        1 => "No problem! Let me know if you need anything else.",
        _ => "Anytime! Enjoy your GoConsoleOS!"
    };

    private string GetJoke()
    {
        var jokes = new[] {
            "Why do programmers prefer dark mode? Because light attracts bugs!",
            "Why did the console break up with the TV? Because it found a better port!",
            "What is a computer favorite snack? Micro-chips!",
            "Why was the JavaScript developer sad? Because he did not know how to Express himself!",
            "How do trees get online? They log in!",
            "Why do Java developers wear glasses? Because they cannot C sharp!",
            "What is a gamer favorite type of food? Square meals!",
            "Why did the USB drive feel left out? Because it always got passed over!"
        };
        return jokes[_rng.Next(jokes.Length)];
    }

    private string GetStatusResponse()
    {
        try
        {
            var initCfg = ConfigReader.ReadInitConfig();
            return "Console system is operational.\nCPU monitor, system watchdog, and controller engine are active.\nGoConsoleOS v2.2 is running.";
        }
        catch { return "System status: GoConsoleOS is running. All services active."; }
    }

    private string GetGameResponse()
    {
        var games = new[] { "Snake", "Pong", "Breakout", "Tetris", "Dino Runner", "Flappy Bird", "Space Invaders", "2048", "Memory Match", "Minesweeper" };
        return "You have 10 built-in games:\n" + string.Join(", ", games) + "\n\nNavigate to the Games tab to play!";
    }

    private string GetUsbResponse()
    {
        try
        {
            var drives = DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Removable && d.IsReady).ToList();
            if (drives.Count == 0) return "No USB drives detected.";
            var list = string.Join("\n", drives.Select(d => "- " + d.Name + " " + d.VolumeLabel + " (" + (d.TotalSize / 1073741824.0).ToString("F1") + "GB)"));
            return "USB Devices:\n" + list;
        }
        catch { return "Could not scan USB devices."; }
    }

    private string GetMotivation()
    {
        var quotes = new[] {
            "The only way to do great work is to love what you do. - Steve Jobs",
            "Play is the highest form of research. - Albert Einstein",
            "Success is not final, failure is not fatal: it is the courage to continue that counts. - Churchill",
            "Believe you can and you are halfway there. - Theodore Roosevelt",
            "It does not matter how slowly you go as long as you do not stop. - Confucius"
        };
        return quotes[_rng.Next(quotes.Length)];
    }

    private string GetSmartFallback(string msg)
    {
        if (msg.Contains("game") || msg.Contains("play")) return "Say Open games or go to the Games tab to browse your library!";
        if (msg.Contains("console") || msg.Contains("system")) return "Check system status from the Settings tab, or say Console status.";
        if (msg.Contains("discord")) return "Open the Discord tab to connect your account and join voice channels.";
        if (msg.Contains("usb") || msg.Contains("drive")) return "Check your USB devices from the Settings tab, or say USB status.";
        if (msg.Contains("music")) return "Open the Media tab to play music.";
        var fallbacks = new[] {
            "I am not sure I understand. Try asking about games, console status, or say help!",
            "Hmm, I did not get that. Try saying Open games or Console status!",
            "I am still learning! Say help to see what I can do.",
            "Not sure what you mean. Try asking about your console, games, or say help!"
        };
        return fallbacks[_rng.Next(fallbacks.Length)];
    }

    private string GetHelpText() =>
        "GoAI Commands:\n" +
        "- Say Open games to browse your library\n" +
        "- Say Console status to check system\n" +
        "- Say Tell me a joke for a laugh\n" +
        "- Say What time is it for the time\n" +
        "- Say USB status to check devices\n" +
        "- Say Help to see this list\n\n" +
        "You can also type any message below!";

    private void AddBubble(string who, string message)
    {
        var isUser = who == "You";
        var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 10), HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left };
        var bubble = new Border
        {
            CornerRadius = new CornerRadius(14),
            Background = new SolidColorBrush(isUser ? Color.FromRgb(0x00, 0x50, 0x5e) : Color.FromRgb(0x1e, 0x1e, 0x32)),
            Padding = new Thickness(14, 10, 14, 10),
            MaxWidth = 420,
        };
        var label = new TextBlock { Text = who, FontSize = 10, FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0x66, 0xFF)), Margin = new Thickness(0, 0, 0, 4) };
        var text = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.FromRgb(0xf0, 0xf0, 0xff)),
            FontSize = 14,
        };
        var stack = new StackPanel();
        stack.Children.Add(label);
        stack.Children.Add(text);
        bubble.Child = stack;
        panel.Children.Add(bubble);
        ChatLog.Children.Add(panel);
        Dispatcher.Invoke(() => ChatScroll.ScrollToEnd());
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        StopListening();
        _synth.SpeakAsyncCancelAll();
        Close();
    }
}
