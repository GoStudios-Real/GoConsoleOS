using System.Text.RegularExpressions;

namespace GoConsoleDiscordBot;

public class GoogleAiService : IDisposable
{
    private readonly ConsoleService _console;
    private readonly Random _rng = new();

    public GoogleAiService(BotConfig config, ConsoleService console)
    {
        _console = console;
    }

    public Task<string> ProcessCommandAsync(string userMessage, string userName)
    {
        var msg = userMessage.ToLower().Trim();

        if (IsGreeting(msg))
            return Task.FromResult(GetGreeting(userName));
        if (IsThanks(msg))
            return Task.FromResult(GetThanks());
        if (IsJoke(msg))
            return Task.FromResult(GetJoke());
        if (IsStatusQuery(msg))
            return Task.FromResult($"Say `!status` to check your console status, {userName}!");
        if (IsGameQuery(msg))
            return Task.FromResult("Say `!games` to see your game library, or `!play <game>` to launch one!");
        if (IsPowerOn(msg))
            return Task.FromResult("Say `!power` to connect to your console!");
        if (IsPowerOff(msg))
            return Task.FromResult("Say `!power off` to disconnect your console.");
        if (IsVoiceQuery(msg))
            return Task.FromResult("Say `!voice` to get a QR code for voice chat, or `!qr voice`!");
        if (IsUsbQuery(msg))
            return Task.FromResult("Say `!usb` to check connected USB devices.");
        if (IsHelpQuery(msg))
            return Task.FromResult(GetHelpText());
        if (IsWhoAreYou(msg))
            return Task.FromResult("I'm **GoConsole AI**, your built-in assistant for GoConsoleOS! I can help you control your console, launch games, check status, and more. Try `!ai help` to see what I can do!");
        if (IsHowAreYou(msg))
            return Task.FromResult("I'm running great! All systems operational. How can I help you today?");
        if (IsWeatherQuery(msg))
            return Task.FromResult("I don't have weather data, but your console is running smoothly! Check `!status` for details.");
        if (IsTimeQuery(msg))
            return Task.FromResult($"Current time: **{DateTime.Now:HH:mm:ss}** ({DateTime.Now:dddd, MMMM dd, yyyy})");
        if (IsMotivationQuery(msg))
            return Task.FromResult(GetMotivation());
        if (IsAboutConsole(msg))
            return Task.FromResult("GoConsoleOS is a USB-powered gaming console that turns any PC into a gaming system. It features cloud gaming, Discord integration, a built-in game library, and AI assistant support. Visit `!status` to see your console info!");
        if (IsAboutGoStudios(msg))
            return Task.FromResult("**GoStudios Corporation** is the developer behind GoConsoleOS. We build gaming tools, console software, and Discord bots. Check out our GitHub: https://github.com/GoStudios-Real");

        return Task.FromResult(GetSmartResponse(msg, userName));
    }

    public async Task<string> ExecuteActionAsync(string action, string? parameter = null)
    {
        return action.ToLower() switch
        {
            "power_on" => await PowerOnAsync(),
            "power_off" => await PowerOffAsync(),
            "launch_game" => await LaunchGameAsync(parameter),
            "list_games" => await ListGamesAsync(),
            "console_status" => await GetStatusAsync(),
            "usb_status" => await GetUsbStatusAsync(),
            "voice_chat" => "Use `!qr voice` to get a QR code for voice chat connection.",
            "help" => GetHelpText(),
            _ => $"Unknown action: {action}",
        };
    }

    private bool IsGreeting(string msg) =>
        Regex.IsMatch(msg, @"^(hi|hello|hey|howdy|sup|yo|greetings|what'?s up|hola|namaste|hiya|heya|wassup|yoo|heeey|heyy|hi there|hello there)\b");
    private string GetGreeting(string userName) => _rng.Next(4) switch
    {
        0 => $"Hey {userName}! Welcome to GoConsoleOS. Need help with your console?",
        1 => $"Hello {userName}! Ready to game? Say `!games` to see your library!",
        2 => $"What's up {userName}! Your console is waiting. Try `!status` to check it out!",
        _ => $"Hi there {userName}! How can I help you today?"
    };

    private bool IsThanks(string msg) =>
        Regex.IsMatch(msg, @"^(thanks?|thx|ty|thank you|cheers|appreciate|tysm|tys)\b");
    private string GetThanks() => _rng.Next(3) switch
    {
        0 => "You're welcome! Happy gaming!",
        1 => "No problem! Let me know if you need anything else.",
        _ => "Anytime! Enjoy your GoConsoleOS!"
    };

    private bool IsJoke(string msg) =>
        Regex.IsMatch(msg, @"(tell me a )?joke|funny|humor|make me laugh");
    private string GetJoke()
    {
        var jokes = new[] {
            "Why do programmers prefer dark mode? Because light attracts bugs!",
            "Why did the console break up with the TV? Because it found a better port!",
            "What's a computer's favorite snack? Micro-chips!",
            "Why was the JavaScript developer sad? Because he didn't Node how to Express himself!",
            "How do trees get online? They log in!",
            "Why do Java developers wear glasses? Because they can't C#!",
            "What's a gamer's favorite type of food? Square meals!",
            "Why did the USB drive feel left out? Because it always got passed over!"
        };
        return jokes[_rng.Next(jokes.Length)];
    }

    private bool IsStatusQuery(string msg) =>
        Regex.IsMatch(msg, @"(console |system )?(status|info|how.+running|performance|health|stats)");
    private bool IsGameQuery(string msg) =>
        Regex.IsMatch(msg, @"(what|which|show|list|see|got|have).*(games?|library|catalog|collection)|games\??|play(ing)?|launch");
    private bool IsPowerOn(string msg) =>
        Regex.IsMatch(msg, @"(turn|boot|start|power|wake|switch).*(on|up)|connect( to)?|go online|bring online");
    private bool IsPowerOff(string msg) =>
        Regex.IsMatch(msg, @"(turn|boot|power|shut|switch).*(off|down)|disconnect|go offline|sleep|shutdown");
    private bool IsVoiceQuery(string msg) =>
        Regex.IsMatch(msg, @"voice|chat|microphone|mic|discord.*(voice|call)|call|talk|speak|audio");
    private bool IsUsbQuery(string msg) =>
        Regex.IsMatch(msg, @"usb|drive|flash|thumb|stick|port|device|removable");
    private bool IsHelpQuery(string msg) =>
        Regex.IsMatch(msg, @"^(help|commands?|what can you|options|menu|guide|tutorial|how do|how to|what do|features)");
    private bool IsWhoAreYou(string msg) =>
        Regex.IsMatch(msg, @"who are you|what are you|your name|about you|introduce yourself|what do you do");
    private bool IsHowAreYou(string msg) =>
        Regex.IsMatch(msg, @"how are you|how('s| is) it going|how do you do|you doing|you ok|you good|how you");
    private bool IsWeatherQuery(string msg) =>
        Regex.IsMatch(msg, @"weather|temperature|rain|sunny|cold|hot|forecast|climate");
    private bool IsTimeQuery(string msg) =>
        Regex.IsMatch(msg, @"what('s| is) (the )?time|current time|what time|clock|date|today");
    private bool IsMotivationQuery(string msg) =>
        Regex.IsMatch(msg, @"motivat|inspir|encourage|keep going|give me strength|boost|pump me up|hype");
    private bool IsAboutConsole(string msg) =>
        Regex.IsMatch(msg, @"what is goconsole|about goconsole|tell me about|what does|explain goconsole|console info|about this");
    private bool IsAboutGoStudios(string msg) =>
        Regex.IsMatch(msg, @"gostudios|who made|who created|developer|maker|company|studio");

    private string GetMotivation()
    {
        var quotes = new[] {
            "The only way to do great work is to love what you do. - Steve Jobs",
            "Play is the highest form of research. - Albert Einstein",
            "Success is not final, failure is not fatal: it is the courage to continue that counts. - Churchill",
            "The future belongs to those who believe in the beauty of their dreams. - Eleanor Roosevelt",
            "Don't watch the clock; do what it does. Keep going. - Sam Levenson",
            "Believe you can and you're halfway there. - Theodore Roosevelt",
            "It does not matter how slowly you go as long as you do not stop. - Confucius"
        };
        return quotes[_rng.Next(quotes.Length)];
    }

    private string GetSmartResponse(string msg, string userName)
    {
        if (msg.Contains("game") || msg.Contains("play"))
            return $"I can help with games! Try:\n- `!games` -- See your game library\n- `!play <game>` -- Launch a game";
        if (msg.Contains("console") || msg.Contains("system"))
            return $"Console commands:\n- `!status` -- Check system status\n- `!power` -- Connect to console\n- `!power off` -- Disconnect";
        if (msg.Contains("music") || msg.Contains("song"))
            return "I don't control music directly, but you can use the music player in GoConsoleOS! Say `!ai help` for more.";
        if (msg.Contains("discord") || msg.Contains("server"))
            return "Need help with Discord? Try:\n- `!invite` -- Get bot invite link\n- `!voice` -- Set up voice chat\n- `!qr voice` -- QR code for mobile voice";
        if (msg.Contains("usb") || msg.Contains("drive"))
            return "Check your USB devices with `!usb`! I'll show all connected drives and highlight any GoConsoleOS USBs.";

        var fallbacks = new[] {
            $"I'm not sure I understand, {userName}. Try `!ai help` to see what I can do!",
            $"Hmm, I didn't quite get that. Try asking me about your console, games, or USB devices!",
            $"I'm still learning! Try `!ai help` for a list of things I understand.",
            $"Not sure what you mean. Try `!ai help` or ask me something about GoConsoleOS!"
        };
        return fallbacks[_rng.Next(fallbacks.Length)];
    }

    private async Task<string> PowerOnAsync()
    {
        if (!_console.IsConnected)
        {
            var connected = await _console.TryConnectToLocalConsole();
            if (!connected) connected = await _console.TryConnectToCloud();
            if (!connected) return "Console not found. Make sure your GoConsoleOS console is powered on and connected.";
        }
        return $"Console **{_console.ConsoleName}** is already online and connected.";
    }

    private Task<string> PowerOffAsync()
    {
        if (!_console.IsConnected) return Task.FromResult("No console is currently connected.");
        _console.Disconnect();
        return Task.FromResult("Console disconnected.");
    }

    private async Task<string> LaunchGameAsync(string? gameName)
    {
        if (string.IsNullOrEmpty(gameName)) return "Please specify a game name. Use `!games` to see available games.";
        if (!_console.IsConnected) return "No console connected. Use `!connect` first.";
        var success = await _console.LaunchGameAsync(gameName);
        return success ? $"Launching **{gameName}** on {_console.ConsoleName}..." : $"Failed to launch **{gameName}**. Check if the game is installed.";
    }

    private async Task<string> ListGamesAsync()
    {
        if (!_console.IsConnected) return "No console connected. Use `!connect` first.";
        var games = await _console.GetGamesAsync();
        if (games.Count == 0) return "No games found on the console.";
        var list = string.Join("\n", games.Select(g => $"- **{g.Title}** ({g.Platform})"));
        return $"**Games on {_console.ConsoleName}:**\n{list}";
    }

    private async Task<string> GetStatusAsync()
    {
        if (!_console.IsConnected) return "No console connected.";
        var state = await _console.GetConsoleStateAsync();
        if (!state.IsOnline) return "Console is offline.";
        return $"**{state.Name}** v{state.Version}\nCPU: {state.CpuUsage:F1}% | RAM: {state.RamUsageMb:F0}MB\nUptime: {state.Uptime}";
    }

    private async Task<string> GetUsbStatusAsync()
    {
        var devices = await _console.GetUsbDevicesAsync();
        if (devices.Count == 0) return "No USB drives detected.";
        var list = string.Join("\n", devices.Select(d => $"- **{d.DriveLetter}** {d.Label} ({d.TotalSizeGb:F1}GB)" + (d.IsGoConsole ? $" GoConsoleOS ({d.OsName})" : "")));
        return $"**USB Devices:**\n{list}";
    }

    private string GetHelpText() =>
@"**GoConsole AI Commands:**
- `!ai turn on console` -- Power on GoConsole
- `!ai turn off console` -- Power off GoConsole
- `!ai open [game name]` -- Launch a game
- `!ai list games` -- Show game library
- `!ai console status` -- Check console status
- `!ai usb status` -- Check USB devices
- `!ai help` -- Show AI commands

You can also chat naturally with me!";

    public void Dispose() { }
}
