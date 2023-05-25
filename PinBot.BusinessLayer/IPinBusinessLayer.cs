using PinBot.Models;

namespace PinBot.BusinessLayer;

public interface IPinBusinessLayer
{
    Task<bool> SavePin(
        string messageId,
        string guildId,
        string channelId,
        string userId,
        DateTime datePinned,
        string newPinMessageId,
        string pinChannelId);

    Task<bool> DeleteByMessageId(string messageId, string guildId);
    Task<bool> DeleteByPinId(string pinMessageId, string guildId);
    Task<PinnedMessage?> GetPinnedMessage(string messageId, string guildId);
    Task<PinnedMessage?> GetPinnedMessageByPinId(string pinId, string guildId);
    Task<bool> SetWebhook(string webhookId, string guildId, string token, string channelId);
    Task<Webhook?> GetWebhook(string guildId);
    Task<Settings?> GetSettings(string guildId);
    Task<bool> SaveSettings(string guildId, bool enableAutoMode);
    Task<bool> SetPinVoteCount(string guildId, int pinVoteCount);
}