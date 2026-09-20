using System.Text.Json;

namespace GoConsoleDiscordBot;

public class BotConfig
{
    public string Token { get; set; } = "";
    public string Prefix { get; set; } = "!";
    public string Status { get; set; } = "GoConsoleOS Bot";
    public string Activity { get; set; } = "with GoConsoleOS";
    public string CloudServerUrl { get; set; } = "https://gostudios.net/api";
    public int ConsoleApiPort { get; set; } = 39210;
    public string GoogleAiApiKey { get; set; } = "";
    public string GoogleAiModel { get; set; } = "gemini-2.0-flash";
    public List<string> AllowedUserIds { get; set; } = new();
    public List<string> AllowedRoleNames { get; set; } = new() { "Admin", "GoConsole Admin" };
    public int QrCodeSize { get; set; } = 300;
    public string VoiceChannelName { get; set; } = "Cloud Gaming";

    public static BotConfig Load(string path)
    {
        var json = File.ReadAllText(path);
        var doc = JsonSerializer.Deserialize<JsonElement>(json);
        return new BotConfig
        {
            Token = doc.GetProperty("token").GetString() ?? "",
            Prefix = doc.GetProperty("prefix").GetString() ?? "!",
            Status = doc.GetProperty("status").GetString() ?? "GoConsoleOS Bot",
            Activity = doc.GetProperty("activity").GetString() ?? "with GoConsoleOS",
            CloudServerUrl = doc.GetProperty("cloudServerUrl").GetString() ?? "https://gostudios.net/api",
            ConsoleApiPort = doc.GetProperty("consoleApiPort").GetInt32(),
            GoogleAiApiKey = doc.GetProperty("googleAiApiKey").GetString() ?? "",
            GoogleAiModel = doc.GetProperty("googleAiModel").GetString() ?? "gemini-2.0-flash",
            AllowedUserIds = doc.TryGetProperty("allowedUserIds", out var ids)
                ? ids.EnumerateArray().Select(x => x.GetString() ?? "").ToList()
                : new List<string>(),
            AllowedRoleNames = doc.TryGetProperty("allowedRoleNames", out var roles)
                ? roles.EnumerateArray().Select(x => x.GetString() ?? "").ToList()
                : new List<string> { "Admin", "GoConsole Admin" },
            QrCodeSize = doc.TryGetProperty("qrCodeSize", out var sz) ? sz.GetInt32() : 300,
            VoiceChannelName = doc.TryGetProperty("voiceChannelName", out var vc) ? vc.GetString() ?? "Cloud Gaming" : "Cloud Gaming",
        };
    }
}
