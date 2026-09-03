using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Text.Json;

var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
var configData = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(configPath));
var token = configData.GetProperty("token").GetString() ?? "";
var prefix = configData.GetProperty("prefix").GetString() ?? "!";
var status = configData.GetProperty("status").GetString() ?? "GoConsoleOS Bot";
var activity = configData.GetProperty("activity").GetString() ?? "with GoConsoleOS";

if (string.IsNullOrEmpty(token))
{
    Console.WriteLine("Please set your bot token in config.json");
    Console.WriteLine("Press any key to exit...");
    Console.ReadKey();
    return;
}

var discordConfig = new DiscordSocketConfig
{
    GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMembers |
                     GatewayIntents.GuildMessages | GatewayIntents.MessageContent |
                     GatewayIntents.GuildVoiceStates | GatewayIntents.DirectMessages
};

var client = new DiscordSocketClient(discordConfig);
var commands = new CommandService();
var services = new ServiceCollection().BuildServiceProvider();

await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);
await client.LoginAsync(TokenType.Bot, token);
await client.StartAsync();

client.Log += msg =>
{
    Console.WriteLine($"[LOG] {msg.Message}");
    return Task.CompletedTask;
};

client.MessageReceived += async msg =>
{
    if (msg is not SocketUserMessage userMsg) return;
    if (userMsg.Author.IsBot) return;

    var argPos = 0;
    if (userMsg.HasStringPrefix(prefix, ref argPos) || userMsg.HasMentionPrefix(client.CurrentUser, ref argPos))
    {
        var context = new CommandContext(client, userMsg);
        var result = await commands.ExecuteAsync(context, argPos, services);
        if (!result.IsSuccess)
            Console.WriteLine($"[CMD ERROR] {result.ErrorReason}");
    }
};

client.Ready += async () =>
{
    Console.WriteLine($"[READY] {client.CurrentUser.Username} is online!");
    Console.WriteLine($"[INFO] Serving {client.Guilds.Count} server(s)");

    foreach (var guild in client.Guilds)
    {
        Console.WriteLine($"[GUILD] {guild.Name} ({guild.MemberCount} members)");
    }

    await client.SetGameAsync(activity, null, ActivityType.Playing);
};

Console.WriteLine("GoConsoleOS Discord Bot v1.0");
Console.WriteLine($"Prefix: {prefix}");
Console.WriteLine("Press Ctrl+C to stop");
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    client.StopAsync().GetAwaiter().GetResult();
    Environment.Exit(0);
};

await Task.Delay(Timeout.Infinite);
