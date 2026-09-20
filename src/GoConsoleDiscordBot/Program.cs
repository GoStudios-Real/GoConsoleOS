using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Text.Json;
using GoConsoleDiscordBot;

var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
if (!File.Exists(configPath))
    configPath = Path.Combine(Directory.GetCurrentDirectory(), "config.json");

var config = BotConfig.Load(configPath);

if (string.IsNullOrEmpty(config.Token))
{
    Console.WriteLine("╔══════════════════════════════════════════╗");
    Console.WriteLine("║     GoConsoleOS Discord Bot v2.2        ║");
    Console.WriteLine("╠══════════════════════════════════════════╣");
    Console.WriteLine("║  Please set your bot token in           ║");
    Console.WriteLine("║  config.json and restart.               ║");
    Console.WriteLine("║                                          ║");
    Console.WriteLine("║  Get token at:                          ║");
    Console.WriteLine("║  https://discord.com/developers/applications ║");
    Console.WriteLine("╚══════════════════════════════════════════╝");
    Console.ReadKey();
    return;
}

var services = new ServiceCollection()
    .AddSingleton(config)
    .AddSingleton<ConsoleService>()
    .AddSingleton<QrCodeService>()
    .AddSingleton<GoogleAiService>()
    .BuildServiceProvider();

var discordConfig = new DiscordSocketConfig
{
    GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMembers |
                     GatewayIntents.GuildMessages | GatewayIntents.MessageContent |
                     GatewayIntents.GuildVoiceStates | GatewayIntents.DirectMessages
};

var client = new DiscordSocketClient(discordConfig);
var commands = new CommandService(new CommandServiceConfig
{
    CaseSensitiveCommands = false,
    DefaultRunMode = RunMode.Async,
});

await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);
await client.LoginAsync(TokenType.Bot, config.Token);
await client.StartAsync();

Console.WriteLine("╔══════════════════════════════════════════╗");
Console.WriteLine("║     GoConsoleOS Discord Bot v2.2        ║");
Console.WriteLine("╠══════════════════════════════════════════╣");
Console.WriteLine($"║  Prefix: {config.Prefix,-31}║");
Console.WriteLine($"║  Cloud:  {config.CloudServerUrl,-31}║");
Console.WriteLine($"║  AI:     {(string.IsNullOrEmpty(config.GoogleAiApiKey) ? "Not configured" : "Configured"),-31}║");
Console.WriteLine("╚══════════════════════════════════════════╝");

client.Log += msg =>
{
    Console.WriteLine($"[{msg.Severity}] {msg.Message}");
    return Task.CompletedTask;
};

client.MessageReceived += async msg =>
{
    if (msg is not SocketUserMessage userMsg) return;
    if (userMsg.Author.IsBot) return;

    var argPos = 0;
    if (userMsg.HasStringPrefix(config.Prefix, ref argPos) || userMsg.HasMentionPrefix(client.CurrentUser, ref argPos))
    {
        var context = new CommandContext(client, userMsg);
        var result = await commands.ExecuteAsync(context, argPos, services);
        if (!result.IsSuccess)
        {
            Console.WriteLine($"[CMD ERROR] {result.ErrorReason}");
            if (result.Error != CommandError.UnknownCommand)
            {
                await context.Channel.SendMessageAsync($"Error: {result.ErrorReason}");
            }
        }
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

    await client.SetGameAsync(config.Activity, null, ActivityType.Playing);

    var consoleService = services.GetRequiredService<ConsoleService>();
    var connected = await consoleService.TryConnectToLocalConsole();
    if (!connected)
        connected = await consoleService.TryConnectToCloud();
    Console.WriteLine(connected
        ? $"[CONSOLE] Connected to {consoleService.ConsoleName}"
        : "[CONSOLE] No console found (use !connect to connect)");
};

Console.WriteLine("Press Ctrl+C to stop");
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    client.StopAsync().GetAwaiter().GetResult();
    Environment.Exit(0);
};

await Task.Delay(Timeout.Infinite);
