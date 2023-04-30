namespace PinBot.Models;

public class Webhook
{
    public string? WebhookId { get; set; }
    public string? Token { get; set; }
    public string GuildId { get; set; }
    public string ChannelId { get; set; }
}