using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace GoConsoleDiscordBot;

public class GoogleAiService : IDisposable
{
    private readonly BotConfig _config;
    private readonly HttpClient _http;
    private readonly ConsoleService _console;

    public GoogleAiService(BotConfig config, ConsoleService console)
    {
        _config = config;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _console = console;
    }

    public async Task<string> ProcessCommandAsync(string userMessage, string userName)
    {
        if (string.IsNullOrEmpty(_config.GoogleAiApiKey))
            return "Google AI API key is not configured. Set `googleAiApiKey` in config.json.";

        var systemPrompt = $@"You are GoConsole AI, the assistant for GoConsoleOS gaming console.
You can help users control their console, launch games, check status, and manage settings.

Available actions (respond with JSON when an action is needed):
- power_on: Turn on the console
- power_off: Turn off the console
- launch_game: Launch a game (requires game name)
- list_games: List available games
- console_status: Get console status
- usb_status: Check USB console status
- voice_chat: Help set up voice chat
- help: Show available commands

Current console state: {(_console.IsConnected ? $"Connected to {_console.ConsoleName}" : "Not connected")}
User: {userName}

Respond naturally to conversational messages. Only respond with JSON action when the user clearly wants to perform an action.
For regular chat, respond normally as GoConsole AI.";

        try
        {
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = $"{systemPrompt}\n\nUser: {userMessage}" }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = 1024,
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync(
                $"https://generativelanguage.googleapis.com/v1beta/models/{_config.GoogleAiModel}:generateContent?key={_config.GoogleAiApiKey}",
                content);

            if (!response.IsSuccessStatusCode)
                return $"AI request failed: {response.StatusCode}";

            var responseJson = await response.Content.ReadAsStringAsync();
            var doc = JsonSerializer.Deserialize<JsonElement>(responseJson);

            var text = doc
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return text ?? "No response from AI.";
        }
        catch (Exception ex)
        {
            return $"AI error: {ex.Message}";
        }
    }

    public async Task<string> ExecuteActionAsync(string action, string? parameter = null)
    {
        return action.ToLower() switch
        {
            "voice_chat" => "Use `!qr voice` to get a QR code for voice chat connection.",
            "help" => GetHelpText(),
            _ => await ExecuteActionInnerAsync(action, parameter),
        };
    }

    private async Task<string> ExecuteActionInnerAsync(string action, string? parameter)
    {
        return action.ToLower() switch
        {
            "power_on" => await PowerOnAsync(),
            "power_off" => await PowerOffAsync(),
            "launch_game" => await LaunchGameAsync(parameter),
            "list_games" => await ListGamesAsync(),
            "console_status" => await GetStatusAsync(),
            "usb_status" => await GetUsbStatusAsync(),
            _ => $"Unknown action: {action}",
        };
    }

    private async Task<string> PowerOnAsync()
    {
        if (!_console.IsConnected)
        {
            var connected = await _console.TryConnectToLocalConsole();
            if (!connected)
                connected = await _console.TryConnectToCloud();
            if (!connected)
                return "Console not found. Make sure your GoConsoleOS console is powered on and connected.";
        }
        return $"Console **{_console.ConsoleName}** is already online and connected.";
    }

    private Task<string> PowerOffAsync()
    {
        if (!_console.IsConnected)
            return Task.FromResult("No console is currently connected.");
        _console.Disconnect();
        return Task.FromResult("Console disconnected.");
    }

    private async Task<string> LaunchGameAsync(string? gameName)
    {
        if (string.IsNullOrEmpty(gameName))
            return "Please specify a game name. Use `!games` to see available games.";
        if (!_console.IsConnected)
            return "No console connected. Use `!connect` first.";

        var success = await _console.LaunchGameAsync(gameName);
        return success
            ? $"Launching **{gameName}** on {_console.ConsoleName}..."
            : $"Failed to launch **{gameName}**. Check if the game is installed.";
    }

    private async Task<string> ListGamesAsync()
    {
        if (!_console.IsConnected)
            return "No console connected. Use `!connect` first.";

        var games = await _console.GetGamesAsync();
        if (games.Count == 0)
            return "No games found on the console.";

        var list = string.Join("\n", games.Select(g => $"• **{g.Title}** ({g.Platform}){(g.IsRunning ? " 🟢 Running" : "")}"));
        return $"**Games on {_console.ConsoleName}:**\n{list}";
    }

    private async Task<string> GetStatusAsync()
    {
        if (!_console.IsConnected)
            return "No console connected.";

        var state = await _console.GetConsoleStateAsync();
        if (!state.IsOnline)
            return "Console is offline.";

        return $"**{state.Name}** v{state.Version}\n" +
               $"CPU: {state.CpuUsage:F1}% | RAM: {state.RamUsageMb:F0}MB\n" +
               $"Uptime: {state.Uptime}";
    }

    private async Task<string> GetUsbStatusAsync()
    {
        var devices = await _console.GetUsbDevicesAsync();
        if (devices.Count == 0)
            return "No USB drives detected.";

        var list = string.Join("\n", devices.Select(d =>
            $"• **{d.DriveLetter}** {d.Label} ({d.TotalSizeGb:F1}GB)" +
            (d.IsGoConsole ? $" ✅ GoConsoleOS ({d.OsName})" : "")));
        return $"**USB Devices:**\n{list}";
    }

    private string GetHelpText()
    {
        return @"**GoConsole AI Commands:**
• `!ai turn on console` - Power on GoConsole
• `!ai turn off console` - Power off GoConsole
• `!ai open [game name]` - Launch a game
• `!ai list games` - Show game library
• `!ai console status` - Check console status
• `!ai usb status` - Check USB devices
• `!ai help` - Show AI commands

You can also chat naturally with the AI!";
    }

    public void Dispose()
    {
        _http?.Dispose();
    }
}
