using Discord;
using Discord.Commands;

namespace GoConsoleDiscordBot;

public class AiModule : ModuleBase<SocketCommandContext>
{
    private readonly GoogleAiService _ai;
    private readonly ConsoleService _console;

    public AiModule(GoogleAiService ai, ConsoleService console)
    {
        _ai = ai;
        _console = console;
    }

    [Command("ai")]
    [Summary("Ask GoConsole AI Assistant anything")]
    public async Task AskAi([Remainder] string message)
    {
        var typingState = Context.Channel.EnterTypingState();

        try
        {
            var response = await _ai.ProcessCommandAsync(message, Context.User.Username);

            var embed = new EmbedBuilder()
                .WithTitle("GoConsole AI")
                .WithDescription(response)
                .WithColor(new Color(0x00, 0x66, 0xFF))
                .WithFooter($"Asked by {Context.User.Username}")
                .WithTimestamp(DateTimeOffset.UtcNow)
                .Build();

            await Context.Channel.SendMessageAsync(embed: embed);
        }
        catch (Exception ex)
        {
            var embed = new EmbedBuilder()
                .WithTitle("AI Error")
                .WithDescription($"Failed to get AI response: {ex.Message}")
                .WithColor(Color.Red)
                .Build();
            await Context.Channel.SendMessageAsync(embed: embed);
        }
        finally
        {
            typingState.Dispose();
        }
    }

    [Command("ask")]
    [Summary("Ask GoConsole AI Assistant (alias for !ai)")]
    public async Task Ask([Remainder] string message)
    {
        await AskAi(message);
    }

    [Command("assistant")]
    [Summary("Open Google AI Assistant settings")]
    public async Task Assistant()
    {
        var embed = new EmbedBuilder()
            .WithTitle("GoConsole AI Assistant")
            .WithDescription("Google AI powered assistant for GoConsoleOS")
            .AddField("Usage", "`!ai <message>` - Ask the AI anything\n`!ai turn on console` - Power on console\n`!ai open [game]` - Launch a game\n`!ai list games` - Show games\n`!ai status` - Console status", false)
            .AddField("Status", string.IsNullOrEmpty(_ai.ToString()) ? "⚠️ API key not configured" : "✅ Configured", true)
            .WithColor(new Color(0x00, 0x66, 0xFF))
            .WithFooter("Powered by Google AI (Gemini)")
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
        await Context.Channel.SendMessageAsync(embed: embed);
    }

    [Command("voice")]
    [Summary("Set up voice chat for cloud gaming")]
    public async Task Voice()
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
            var noChannel = new EmbedBuilder()
                .WithTitle("No Voice Channel")
                .WithDescription("Create a voice channel first, then use `!voice` again.")
                .WithColor(Color.Red)
                .Build();
            await Context.Channel.SendMessageAsync(embed: noChannel);
            return;
        }

        var invite = await voiceChannel.CreateInviteAsync(maxAge: 86400);

        var embed = new EmbedBuilder()
            .WithTitle("Cloud Gaming Voice Chat")
            .WithDescription($"Join **{voiceChannel.Name}** for voice chat while gaming!\n\n" +
                           $"🔗 [Click to Join]({invite.Url})\n\n" +
                           $"Share this QR code with your phone:")
            .WithUrl(invite.Url)
            .WithColor(new Color(0x00, 0x66, 0xFF))
            .WithFooter("Voice chat works with GoConsoleOS Cloud Gaming")
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

        var qrBytes = new QrCodeService(new BotConfig()).GenerateVoiceChatQr(invite.Url);
        var attachment = new System.IO.MemoryStream(qrBytes);
        var fileAttachment = new Discord.FileAttachment(attachment, "voice_qr.png");

        await Context.Channel.SendFilesAsync(
            new[] { fileAttachment },
            embed: embed);
    }
}
