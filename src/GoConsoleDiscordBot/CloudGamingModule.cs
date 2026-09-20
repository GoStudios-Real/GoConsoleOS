using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace GoConsoleDiscordBot;

public class CloudGamingModule : ModuleBase<SocketCommandContext>
{
    private readonly ConsoleService _console;
    private readonly QrCodeService _qr;

    public CloudGamingModule(ConsoleService console, QrCodeService qr)
    {
        _console = console;
        _qr = qr;
    }

    [Command("connect")]
    [Summary("Connect to GoConsole (local or cloud)")]
    public async Task Connect(string? target = null)
    {
        var embed = new EmbedBuilder()
            .WithTitle("Connecting to GoConsole...")
            .WithDescription("Searching for console...")
            .WithColor(Color.Blue)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
        var msg = await Context.Channel.SendMessageAsync(embed: embed);

        bool connected;
        if (target?.ToLower() == "cloud")
            connected = await _console.TryConnectToCloud();
        else
        {
            connected = await _console.TryConnectToLocalConsole();
            if (!connected)
                connected = await _console.TryConnectToCloud();
        }

        if (connected)
        {
            embed = new EmbedBuilder()
                .WithTitle("Console Connected")
                .WithDescription($"Connected to **{_console.ConsoleName}**")
                .AddField("Address", _console.ConsoleAddress ?? "Unknown", true)
                .WithColor(Color.Green)
                .WithTimestamp(DateTimeOffset.UtcNow)
                .Build();
        }
        else
        {
            embed = new EmbedBuilder()
                .WithTitle("Connection Failed")
                .WithDescription("Could not find a GoConsole.\nMake sure your console is powered on and connected to the network.")
                .AddField("Try", "`!connect cloud` to connect via cloud server", false)
                .WithColor(Color.Red)
                .WithTimestamp(DateTimeOffset.UtcNow)
                .Build();
        }
        await msg.ModifyAsync(x => x.Embed = embed);
    }

    [Command("disconnect")]
    [Summary("Disconnect from console")]
    public async Task Disconnect()
    {
        _console.Disconnect();
        var embed = new EmbedBuilder()
            .WithTitle("Disconnected")
            .WithDescription("Disconnected from console.")
            .WithColor(Color.Orange)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
        await Context.Channel.SendMessageAsync(embed: embed);
    }

    [Command("qr")]
    [Summary("Generate QR code for cloud gaming or voice chat")]
    public async Task Qr(string? type = null)
    {
        string url;
        string title;
        string description;

        if (type?.ToLower() == "voice")
        {
            var voiceChannel = Context.Guild?.VoiceChannels.FirstOrDefault(v =>
                v.Name.Contains("Cloud Gaming", StringComparison.OrdinalIgnoreCase) ||
                v.Name.Contains("Gaming", StringComparison.OrdinalIgnoreCase));

            if (voiceChannel == null)
            {
                voiceChannel = Context.Guild?.VoiceChannels.FirstOrDefault();
            }

            if (voiceChannel == null)
            {
                await Context.Channel.SendMessageAsync("No voice channel found. Create a voice channel first.");
                return;
            }

            url = $"https://discord.com/channels/{Context.Guild.Id}/{voiceChannel.Id}";
            title = "Voice Chat QR Code";
            description = $"Scan to join **{voiceChannel.Name}** for voice chat with your console.\n\n" +
                         $"Or click: [Join Voice Channel]({url})";
        }
        else
        {
            url = _qr.GetCloudGamingUrl(_console.ConsoleAddress);
            title = "Cloud Gaming QR Code";
            description = $"Scan with your phone to connect to **{_console.ConsoleName ?? "GoConsoleOS"}** for cloud gaming.\n\n" +
                         $"Server: `{_console.ConsoleAddress ?? _console.ConsoleAddress ?? "Not connected"}`";
        }

        var qrBytes = _qr.GenerateCloudGamingQr(url);
        var attachment = _qr.CreateQrAttachment(qrBytes, "qr_code.png");

        var embed = new EmbedBuilder()
            .WithTitle(title)
            .WithDescription(description)
            .WithImageUrl("attachment://qr_code.png")
            .WithColor(Color.Blue)
            .WithFooter($"Requested by {Context.User.Username}")
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

        await Context.Channel.SendFilesAsync(
            new[] { attachment },
            embed: embed);
    }

    [Command("games")]
    [Summary("List available games on the console")]
    public async Task Games()
    {
        if (!_console.IsConnected)
        {
            var embed2 = new EmbedBuilder()
                .WithTitle("Not Connected")
                .WithDescription("Use `!connect` to connect to your console first.")
                .WithColor(Color.Red)
                .Build();
            await Context.Channel.SendMessageAsync(embed: embed2);
            return;
        }

        var games = await _console.GetGamesAsync();
        if (games.Count == 0)
        {
            var embed3 = new EmbedBuilder()
                .WithTitle("No Games Found")
                .WithDescription("No games found on the console.")
                .WithColor(Color.Orange)
                .Build();
            await Context.Channel.SendMessageAsync(embed: embed3);
            return;
        }

        var gameList = string.Join("\n", games.Select(g =>
            $"• **{g.Title}** ({g.Platform}){(g.IsRunning ? " 🟢" : "")}"));

        var embed = new EmbedBuilder()
            .WithTitle($"Game Library ({_console.ConsoleName})")
            .WithDescription(gameList)
            .WithColor(Color.Blue)
            .WithFooter("Use !play <game> to launch a game")
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
        await Context.Channel.SendMessageAsync(embed: embed);
    }

    [Command("play")]
    [Summary("Launch a game on the console")]
    public async Task Play([Remainder] string gameName)
    {
        if (!_console.IsConnected)
        {
            var embed2 = new EmbedBuilder()
                .WithTitle("Not Connected")
                .WithDescription("Use `!connect` to connect to your console first.")
                .WithColor(Color.Red)
                .Build();
            await Context.Channel.SendMessageAsync(embed: embed2);
            return;
        }

        var success = await _console.LaunchGameAsync(gameName);
        var embed = new EmbedBuilder()
            .WithTitle(success ? "Launching Game" : "Launch Failed")
            .WithDescription(success
                ? $"Starting **{gameName}** on {_console.ConsoleName}..."
                : $"Could not launch **{gameName}**. Check if the game is installed.")
            .WithColor(success ? Color.Green : Color.Red)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
        await Context.Channel.SendMessageAsync(embed: embed);
    }

    [Command("status")]
    [Summary("Check console status")]
    public async Task Status()
    {
        if (!_console.IsConnected)
        {
            var embed2 = new EmbedBuilder()
                .WithTitle("Not Connected")
                .WithDescription("Use `!connect` to connect to your console first.")
                .WithColor(Color.Red)
                .Build();
            await Context.Channel.SendMessageAsync(embed: embed2);
            return;
        }

        var state = await _console.GetConsoleStateAsync();
        var embed = new EmbedBuilder()
            .WithTitle(state.IsOnline ? "Console Online" : "Console Offline")
            .WithDescription(state.IsOnline
                ? $"**{state.Name}** v{state.Version}"
                : "Console is not responding.")
            .WithColor(state.IsOnline ? Color.Green : Color.Red);

        if (state.IsOnline)
        {
            embed.AddField("CPU", $"{state.CpuUsage:F1}%", true)
                 .AddField("RAM", $"{state.RamUsageMb:F0} MB", true)
                 .AddField("Uptime", state.Uptime, true);
        }

        embed.WithTimestamp(DateTimeOffset.UtcNow);
        await Context.Channel.SendMessageAsync(embed: embed.Build());
    }

    [Command("usb")]
    [Summary("Check connected USB devices")]
    public async Task Usb()
    {
        var devices = await _console.GetUsbDevicesAsync();
        if (devices.Count == 0)
        {
            var embed2 = new EmbedBuilder()
                .WithTitle("No USB Devices")
                .WithDescription("No USB drives detected.")
                .WithColor(Color.Orange)
                .Build();
            await Context.Channel.SendMessageAsync(embed: embed2);
            return;
        }

        var deviceList = string.Join("\n", devices.Select(d =>
            $"**{d.DriveLetter}** {d.Label} ({d.TotalSizeGb:F1}GB free: {d.FreeSpaceGb:F1}GB)" +
            (d.IsGoConsole ? $"\n  ✅ GoConsoleOS - {d.OsName}" : "")));

        var embed = new EmbedBuilder()
            .WithTitle("USB Devices")
            .WithDescription(deviceList)
            .WithColor(Color.Teal)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
        await Context.Channel.SendMessageAsync(embed: embed);
    }

    [Command("power")]
    [Summary("Power on/off the console")]
    public async Task Power(string? action = null)
    {
        if (action?.ToLower() == "off")
        {
            _console.Disconnect();
            var embed3 = new EmbedBuilder()
                .WithTitle("Console Disconnected")
                .WithDescription("Console has been disconnected.")
                .WithColor(Color.Orange)
                .Build();
            await Context.Channel.SendMessageAsync(embed: embed3);
            return;
        }

        if (!_console.IsConnected)
        {
            var embed2 = new EmbedBuilder()
                .WithTitle("Connecting...")
                .WithDescription("Attempting to connect to console...")
                .WithColor(Color.Blue)
                .Build();
            var msg = await Context.Channel.SendMessageAsync(embed: embed2);

            var connected = await _console.TryConnectToLocalConsole();
            if (!connected)
                connected = await _console.TryConnectToCloud();

            if (connected)
            {
                embed2 = new EmbedBuilder()
                    .WithTitle("Console Online")
                    .WithDescription($"Connected to **{_console.ConsoleName}**")
                    .WithColor(Color.Green)
                    .Build();
            }
            else
            {
                embed2 = new EmbedBuilder()
                    .WithTitle("Console Offline")
                    .WithDescription("Could not find a GoConsole.\nMake sure it's powered on.")
                    .WithColor(Color.Red)
                    .Build();
            }
            await msg.ModifyAsync(x => x.Embed = embed2);
        }
        else
        {
            var embed = new EmbedBuilder()
                .WithTitle("Console Already Online")
                .WithDescription($"Connected to **{_console.ConsoleName}**")
                .WithColor(Color.Green)
                .WithTimestamp(DateTimeOffset.UtcNow)
                .Build();
            await Context.Channel.SendMessageAsync(embed: embed);
        }
    }
}
