using Discord;
using QRCoder;

namespace GoConsoleDiscordBot;

public class QrCodeService
{
    private readonly BotConfig _config;

    public QrCodeService(BotConfig config)
    {
        _config = config;
    }

    public byte[] GenerateCloudGamingQr(string connectionUrl)
    {
        var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(connectionUrl, QRCodeGenerator.ECCLevel.M);
        var qrCode = new PngByteQRCode(qrCodeData);
        return qrCode.GetGraphic(20);
    }

    public byte[] GenerateVoiceChatQr(string voiceInviteUrl)
    {
        var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(voiceInviteUrl, QRCodeGenerator.ECCLevel.M);
        var qrCode = new PngByteQRCode(qrCodeData);
        return qrCode.GetGraphic(20);
    }

    public FileAttachment CreateQrAttachment(byte[] qrBytes, string fileName)
    {
        var stream = new MemoryStream(qrBytes);
        return new FileAttachment(stream, fileName);
    }

    public string GetCloudGamingUrl(string? localAddress = null)
    {
        if (localAddress != null)
            return localAddress;
        return _config.CloudServerUrl;
    }

    public string GetVoiceChatUrl(string guildId, string channelId)
    {
        return $"https://discord.com/channels/{guildId}/{channelId}";
    }
}
