using System.Text.RegularExpressions;

namespace GoConsoleOS.Shared.Ai;

/// <summary>
/// GoAI - the gaming assistant built into GoConsole.exe and GoConsoleOS.exe.
///
/// Works fully offline. It understands intent keywords, answers from a local
/// knowledge base, recommends games, checks the library / health / stats, and
/// can trigger actions (launch a game, open USB installer, etc).
/// </summary>
public sealed class GoAiEngine
{
    public string Name => "GoAI";
    public string Version => "1.0.0";

    private readonly Func<IEnumerable<string>>? _gamesProvider;
    private readonly Func<string, bool>? _launchAction;
    private readonly Func<string>? _healthProvider;
    private readonly Func<string>? _statsProvider;

    public GoAiEngine(
        Func<IEnumerable<string>>? gamesProvider = null,
        Func<string, bool>? launchAction = null,
        Func<string>? healthProvider = null,
        Func<string>? statsProvider = null)
    {
        _gamesProvider = gamesProvider;
        _launchAction = launchAction;
        _healthProvider = healthProvider;
        _statsProvider = statsProvider;
    }

    public GoAiReply Reply(string input)
    {
        var text = (input ?? "").Trim();
        if (string.IsNullOrWhiteSpace(text))
            return new GoAiReply("Hi! I'm GoAI. Ask me about your library, USB health, performance, or say \"help\".");

        var lower = text.ToLowerInvariant();

        if (IsMatch(lower, "hello", "hi ", "hey", "yo ", "greetings"))
            return new GoAiReply("Hello! I'm GoAI, your gaming assistant. Ask me about games, USB health, performance or account.");

        if (IsMatch(lower, "help", "what can you do", "commands", "how do i use"))
            return Help();

        if (IsMatch(lower, "game", "play", "launch", "start"))
            return HandleGame(text, lower);

        if (IsMatch(lower, "usb", "health", "drive", "disk"))
            return HandleHealth();

        if (IsMatch(lower, "performance", "fps", "ram", "memory", "cpu", "gpu", "stats", "system"))
            return HandleStats();

        if (IsMatch(lower, "account", "acc", "login", "sign in", "register", "subscription", "points"))
            return new GoAiReply("You can manage your GoConsoleOS account (ACC) from Settings > Account, or open the account portal. Use \"help\" for the full list.");

        if (IsMatch(lower, "cast", "screen mirror", "mirror"))
            return new GoAiReply("GoConsoleOS Cast lets you mirror your console to a TV or device. Open it from the home screen.");

        if (IsMatch(lower, "thank", "thanks", "cool", "nice", "awesome"))
            return new GoAiReply("You're welcome! Anything else you want to know about GoConsoleOS?");

        if (IsMatch(lower, "who are you", "what are you", "who made you", "creator"))
            return new GoAiReply("I'm GoAI, the GoConsoleOS gaming assistant, built by GoStudios. I run locally on your console - no cloud needed.");

        if (IsMatch(lower, "version", "firmware", "software"))
            return new GoAiReply("GoConsoleOS is on version 1.8.0 with GoAI " + Version + ". You can update from Settings > System Update.");

        if (IsMatch(lower, "recommend", "suggestion", "suggest", "what should i play", "new game", "popular"))
            return HandleRecommend(text);

        if (IsMatch(lower, "screenshot", "capture", "record"))
            return new GoAiReply("Press the capture button (or the screenshot key on the home screen) to grab a screenshot. Recording is available from the Game Recording view.");

        if (IsMatch(lower, "joke", "funny", "humor"))
            return new GoAiReply("Why did the gamer break up with the console? Too much controller drama. Want game recommendations instead?");

        if (IsMatch(lower, "installer", "usb installer", "make usb", "portable usb"))
            return HandleInstaller();

        return new GoAiReply(
            "I didn't catch that. I can help with games, USB health, performance, accounts, Cast, and more. Try \"help\".",
            new[] { "help", "games", "usb health", "performance", "account" });
    }

    private GoAiReply Help()
    {
        return new GoAiReply(
            "Here's what I can do:\n" +
            "  • \"play <game>\" or \"launch <game>\" - start a game\n" +
            "  • \"recommend a game\" - pick something from your library\n" +
            "  • \"usb health\" - check your drives\n" +
            "  • \"performance\" / \"stats\" - CPU, RAM, FPS\n" +
            "  • \"account\" - manage your ACC account\n" +
            "  • \"cast\" - mirror to a TV\n" +
            "  • \"version\" - console software info",
            new[] { "play", "recommend", "usb health", "performance", "account", "cast", "version" });
    }

    private GoAiReply HandleGame(string text, string lower)
    {
        var games = _gamesProvider?.Invoke().ToList() ?? new List<string>();
        if (games.Count == 0)
            return new GoAiReply("Your library looks empty. Open the Store to grab some games, then ask me to play one.");

        // try to find a named game
        var named = ExtractNameAfter(lower, new[] { "play", "launch", "start ", "open " });
        var game = games.FirstOrDefault(g => g.ToLowerInvariant().Contains(named, StringComparison.OrdinalIgnoreCase));

        if (game == null)
            return new GoAiReply($"I couldn't find \"{named}\" in your library. You have: {string.Join(", ", games.Take(12))}");

        var launched = _launchAction?.Invoke(game) ?? false;
        return new GoAiReply(launched
            ? $"Launching {game}! Enjoy your session."
            : $"I found {game} but couldn't start it right now.");
    }

    private GoAiReply HandleRecommend(string text)
    {
        var games = _gamesProvider?.Invoke().ToList() ?? new List<string>();
        if (games.Count == 0)
            return new GoAiReply("Your library is empty, so I can't recommend anything yet. Try the Store first!");

        var rnd = new Random();
        var pick = games[rnd.Next(games.Count)];
        return new GoAiReply($"How about {pick}? Say \"play {pick}\" and I'll start it. Want another suggestion?");
    }

    private GoAiReply HandleHealth()
    {
        if (_healthProvider == null)
            return new GoAiReply("USB health checks are available on this console via USB Health in the home menu.");
        return new GoAiReply(_healthProvider());
    }

    private GoAiReply HandleStats()
    {
        if (_statsProvider == null)
            return new GoAiReply("Performance monitoring is available from Settings > System > Performance.");
        return new GoAiReply(_statsProvider());
    }

    private GoAiReply HandleInstaller()
    {
        try
        {
            var exe = Path.Combine(ConfigReader.RootPath ?? Directory.GetCurrentDirectory(), "GoUsbMaker.exe");
            if (File.Exists(exe))
            {
                var psi = new System.Diagnostics.ProcessStartInfo(exe) { UseShellExecute = true };
                System.Diagnostics.Process.Start(psi);
                return new GoAiReply("Opening GoUsbMaker so you can build a Portable USB Gaming Console.");
            }
            return new GoAiReply("GoUsbMaker.exe wasn't found next to the console.");
        }
        catch (Exception ex)
        {
            return new GoAiReply("Couldn't open the USB installer: " + ex.Message);
        }
    }

    private static bool IsMatch(string lower, params string[] needles)
        => needles.Any(n => lower.Contains(n, StringComparison.OrdinalIgnoreCase));

    private static string ExtractNameAfter(string lower, string[] prefixes)
    {
        var m = Regex.Match(lower, @"(play|launch|start|open)\s+(.+)");
        if (!m.Success) return "";
        var name = m.Groups[2].Value.Trim();
        // strip trailing words that are not part of a title
        foreach (var suffix in new[] { " please", " now", " thanks", " for me" })
            if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                name = name[..^suffix.Length].Trim();
        return name;
    }
}

/// <summary>A structured GoAI answer.</summary>
public sealed class GoAiReply
{
    public GoAiReply(string message, string[]? suggestions = null)
    {
        Message = message;
        Suggestions = suggestions ?? Array.Empty<string>();
    }

    public string Message { get; set; }
    public string[] Suggestions { get; set; }
}
