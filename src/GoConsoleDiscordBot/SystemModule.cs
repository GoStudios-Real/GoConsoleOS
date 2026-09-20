using Discord;
using Discord.Commands;

namespace GoConsoleDiscordBot;

public class SystemModule : ModuleBase<SocketCommandContext>
{
    private readonly ConsoleService _console;
    private readonly BotConfig _config;

    public SystemModule(ConsoleService console, BotConfig config)
    {
        _console = console;
        _config = config;
    }

    [Command("sys")]
    [Summary("System information and controls")]
    public async Task SystemInfo(string? subcommand = null)
    {
        if (subcommand?.ToLower() == "usb")
        {
            await UsbAsync();
            return;
        }

        var state = await _console.GetConsoleStateAsync();
        var embed = new EmbedBuilder()
            .WithTitle("GoConsoleOS System")
            .WithColor(new Color(0x00, 0x66, 0xFF));

        if (_console.IsConnected && state.IsOnline)
        {
            embed.WithDescription("Console is **online**")
                 .AddField("Name", state.Name, true)
                 .AddField("Version", state.Version, true)
                 .AddField("CPU", $"{state.CpuUsage:F1}%", true)
                 .AddField("RAM", $"{state.RamUsageMb:F0} MB", true)
                 .AddField("Uptime", state.Uptime, true)
                 .AddField("Address", _console.ConsoleAddress ?? "Unknown", true);
        }
        else
        {
            embed.WithDescription("Console is **offline**")
                 .WithColor(Color.Red);
        }

        var usbDevices = await _console.GetUsbDevicesAsync();
        if (usbDevices.Count > 0)
        {
            var usbList = string.Join("\n", usbDevices.Select(d =>
                $"**{d.DriveLetter}** {d.Label}" +
                (d.IsGoConsole ? " ✅ GoConsoleOS" : "")));
            embed.AddField("USB Devices", usbList, false);
        }

        embed.WithTimestamp(DateTimeOffset.UtcNow);
        await Context.Channel.SendMessageAsync(embed: embed.Build());
    }

    private async Task UsbAsync()
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

        var fields = devices.Select(d => new EmbedFieldBuilder
        {
            Name = $"{d.DriveLetter} {d.Label}",
            Value = $"Size: {d.TotalSizeGb:F1}GB\nFree: {d.FreeSpaceGb:F1}GB" +
                    (d.IsGoConsole ? $"\n✅ GoConsoleOS ({d.OsName})" : "\nStandard USB"),
            IsInline = true
        }).ToList();

        var embed = new EmbedBuilder()
            .WithTitle("USB Devices")
            .WithFields(fields)
            .WithColor(Color.Teal)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
        await Context.Channel.SendMessageAsync(embed: embed);
    }

    [Command("invite")]
    [Summary("Get bot invite link")]
    public async Task Invite()
    {
        var embed = new EmbedBuilder()
            .WithTitle("Invite GoConsoleOS Bot")
            .WithDescription($"[Click here to invite the bot to your server]({_console.GetInviteUrl(Context.Client.CurrentUser.Id)})")
            .WithColor(new Color(0x00, 0x66, 0xFF))
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
        await Context.Channel.SendMessageAsync(embed: embed);
    }

    [Command("commands")]
    [Summary("List all GoConsole commands")]
    public async Task CommandsList()
    {
        var embed = new EmbedBuilder()
            .WithTitle("GoConsoleOS Bot Commands")
            .WithDescription("All available commands:")
            .AddField("Cloud Gaming", "`!connect` `!disconnect` `!qr` `!games` `!play` `!status` `!power`", false)
            .AddField("AI Assistant", "`!ai <message>` `!ask <message>` `!assistant`", false)
            .AddField("Voice Chat", "`!voice` - Generate QR code for voice chat", false)
            .AddField("System", "`!sys` `!sys usb` `!usb` `!invite`", false)
            .AddField("Utilities", "`!ping` `!server` `!user` `!help` `!commands`", false)
            .AddField("Fun", "`!8ball` `!coinflip` `!dice` `!poll`", false)
            .WithColor(new Color(0x00, 0x66, 0xFF))
            .WithFooter("GoConsoleOS v2.2 | Discord Bot")
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
        await Context.Channel.SendMessageAsync(embed: embed);
    }
}
