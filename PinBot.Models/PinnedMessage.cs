namespace PinBot.Models;

public class PinnedMessage
{
    public string? MessageId { get; set; }
    public string GuildId { get; set; }
    public string ChannelId { get; set; }
    public string PinnedDate { get; set; }
    public string PinningUserId { get; set; }
    public string NewPinMessageId { get; set; }
    public string PinChannelId { get; set; }
}