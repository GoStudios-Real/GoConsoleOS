using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace GoConsoleDiscordBot;

public class InfoModule : ModuleBase<SocketCommandContext>
{
    [Command("ping")]
    [Summary("Check bot latency")]
    public async Task Ping()
    {
        var latency = Context.Client.Latency;
        var embed = new EmbedBuilder()
            .WithTitle("Pong!")
            .WithDescription($"Latency: **{latency}ms**")
            .WithColor(Color.Green)
            .WithFooter($"Requested by {Context.User.Username}")
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
        await Context.Channel.SendMessageAsync(embed: embed);
    }

    [Command("server")]
    [Summary("Get server information")]
    public async Task Server()
    {
        var guild = Context.Guild;
        var embed = new EmbedBuilder()
            .WithTitle(guild.Name)
            .WithThumbnailUrl(guild.IconUrl)
            .AddField("Owner", guild.Owner.Mention, true)
            .AddField("Members", guild.MemberCount.ToString(), true)
            .AddField("Channels", guild.Channels.Count.ToString(), true)
            .AddField("Created", guild.CreatedAt.ToString("MMM dd, yyyy"), true)
            .AddField("Roles", guild.Roles.Count.ToString(), true)
            .AddField("Boosts", guild.PremiumSubscriptionCount.ToString(), true)
            .WithColor(Color.Blue)
            .WithFooter($"Server ID: {guild.Id}")
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
        await Context.Channel.SendMessageAsync(embed: embed);
    }

    [Command("user")]
    [Summary("Get user information")]
    public async Task User(SocketGuildUser? user = null)
    {
        user ??= Context.User as SocketGuildUser;
        if (user == null) return;

        var embed = new EmbedBuilder()
            .WithTitle(user.Username)
            .WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
            .AddField("Discriminator", user.Discriminator, true)
            .AddField("ID", user.Id.ToString(), true)
            .AddField("Joined", user.JoinedAt?.ToString("MMM dd, yyyy") ?? "Unknown", true)
            .AddField("Roles", string.Join(", ", user.Roles.Where(r => r.Name != "@everyone").Select(r => r.Mention)), false)
            .WithColor(Color.Purple)
            .WithFooter($"Requested by {Context.User.Username}")
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
        await Context.Channel.SendMessageAsync(embed: embed);
    }

    [Command("help")]
    [Summary("List all commands")]
    public async Task Help()
    {
        var embed = new EmbedBuilder()
            .WithTitle("GoConsoleOS Bot Commands")
            .WithDescription("Available commands:")
            .AddField("!ping", "Check bot latency", true)
            .AddField("!server", "Get server information", true)
            .AddField("!user [@user]", "Get user information", true)
            .AddField("!members", "Get member count", true)
            .AddField("!channels", "List all channels", true)
            .AddField("!roles", "List all roles", true)
            .AddField("!avatar [@user]", "Get user avatar", true)
            .AddField("!poll \"question\" \"options\"", "Create a poll", true)
            .AddField("!8ball question", "Ask the magic 8-ball", true)
            .AddField("!coinflip", "Flip a coin", true)
            .AddField("!dice [sides]", "Roll a dice", true)
            .WithColor(Color.Gold)
            .WithFooter($"Requested by {Context.User.Username}")
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
        await Context.Channel.SendMessageAsync(embed: embed);
    }

    [Command("members")]
    [Summary("Get member count")]
    public async Task Members()
    {
        var guild = Context.Guild;
        var embed = new EmbedBuilder()
            .WithTitle($"{guild.Name} Members")
            .WithDescription($"Total: **{guild.MemberCount}**\nHumans: **{guild.Users.Count(u => !u.IsBot)}**\nBots: **{guild.Users.Count(u => u.IsBot)}**")
            .WithColor(Color.Teal)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
        await Context.Channel.SendMessageAsync(embed: embed);
    }

    [Command("channels")]
    [Summary("List all channels")]
    public async Task Channels()
    {
        var guild = Context.Guild;
        var text = string.Join("\n", guild.TextChannels.Select(c => $"# {c.Name}"));
        var voice = string.Join("\n", guild.VoiceChannels.Select(c => $"🔊 {c.Name}"));

        var embed = new EmbedBuilder()
            .WithTitle($"{guild.Name} Channels")
            .AddField("Text Channels", text.Length > 1024 ? $"{guild.TextChannels.Count} channels" : text, false)
            .AddField("Voice Channels", voice.Length > 1024 ? $"{guild.VoiceChannels.Count} channels" : voice, false)
            .WithColor(Color.Teal)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
        await Context.Channel.SendMessageAsync(embed: embed);
    }

    [Command("roles")]
    [Summary("List all roles")]
    public async Task Roles()
    {
        var guild = Context.Guild;
        var roles = string.Join("\n", guild.Roles.Where(r => r.Name != "@everyone").OrderByDescending(r => r.Position).Select(r => r.Mention));

        var embed = new EmbedBuilder()
            .WithTitle($"{guild.Name} Roles")
            .WithDescription(roles.Length > 2048 ? $"{guild.Roles.Count} roles" : roles)
            .WithColor(Color.Teal)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
        await Context.Channel.SendMessageAsync(embed: embed);
    }

    [Command("avatar")]
    [Summary("Get user avatar")]
    public async Task Avatar(SocketGuildUser? user = null)
    {
        user ??= Context.User as SocketGuildUser;
        if (user == null) return;

        var embed = new EmbedBuilder()
            .WithTitle($"{user.Username}'s Avatar")
            .WithImageUrl(user.GetAvatarUrl(size: 1024) ?? user.GetDefaultAvatarUrl())
            .WithColor(Color.Gold)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
        await Context.Channel.SendMessageAsync(embed: embed);
    }

    [Command("8ball")]
    [Summary("Ask the magic 8-ball")]
    public async Task EightBall([Remainder] string question)
    {
        var responses = new[]
        {
            "It is certain.", "It is decidedly so.", "Without a doubt.",
            "Yes - definitely.", "You may rely on it.", "As I see it, yes.",
            "Most likely.", "Outlook good.", "Yes.", "Signs point to yes.",
            "Reply hazy, try again.", "Ask again later.", "Better not tell you now.",
            "Cannot predict now.", "Concentrate and ask again.",
            "Don't count on it.", "My reply is no.", "My sources say no.",
            "Outlook not so good.", "Very doubtful."
        };

        var embed = new EmbedBuilder()
            .WithTitle("8-Ball")
            .AddField("Question", question)
            .AddField("Answer", responses[new Random().Next(responses.Length)])
            .WithColor(Color.DarkGrey)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
        await Context.Channel.SendMessageAsync(embed: embed);
    }

    [Command("coinflip")]
    [Summary("Flip a coin")]
    public async Task CoinFlip()
    {
        var result = new Random().Next(2) == 0 ? "Heads" : "Tails";
        var embed = new EmbedBuilder()
            .WithTitle("Coin Flip")
            .WithDescription($"🪙 **{result}**!")
            .WithColor(Color.Gold)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
        await Context.Channel.SendMessageAsync(embed: embed);
    }

    [Command("dice")]
    [Summary("Roll a dice")]
    public async Task Dice(int sides = 6)
    {
        if (sides < 2) sides = 2;
        if (sides > 100) sides = 100;
        var result = new Random().Next(1, sides + 1);
        var embed = new EmbedBuilder()
            .WithTitle("Dice Roll")
            .WithDescription($"🎲 Rolled a **{result}** (d{sides})")
            .WithColor(Color.Red)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();
        await Context.Channel.SendMessageAsync(embed: embed);
    }

    [Command("poll")]
    [Summary("Create a poll")]
    public async Task Poll(string question, [Remainder] string options)
    {
        var optionList = options.Split('|').Select(o => o.Trim()).ToList();
        if (optionList.Count < 2)
        {
            await Context.Channel.SendMessageAsync("Usage: `!poll \"question\" \"option1 | option2 | option3\"`");
            return;
        }

        var emojis = new[] { "1️⃣", "2️⃣", "3️⃣", "4️⃣", "5️⃣", "6️⃣", "7️⃣", "8️⃣", "9️⃣", "🔟" };
        var description = string.Join("\n\n", optionList.Take(10).Select((o, i) => $"{emojis[i]} {o}"));

        var embed = new EmbedBuilder()
            .WithTitle($"📊 {question}")
            .WithDescription(description)
            .WithColor(Color.Blue)
            .WithFooter($"Poll by {Context.User.Username}")
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

        var msg = await Context.Channel.SendMessageAsync(embed: embed);
        for (var i = 0; i < Math.Min(optionList.Count, 10); i++)
        {
            await msg.AddReactionAsync(new Emoji(emojis[i]));
        }
    }
}
