using PinBot.DataLayer;
using PinBot.Models;

namespace PinBot.BusinessLayer;

public class PinBusinessLayer(IPinDataLayer pinDataLayer) : IPinBusinessLayer
{
    public Task<bool> SavePin(
        string messageId,
        string guildId,
        string channelId,
        string userId,
        DateTime datePinned,
        string newPinMessageId,
        string pinChannelId)
    {
        return pinDataLayer.SavePin(messageId, guildId, channelId, userId, datePinned, newPinMessageId, pinChannelId);
    }

    public Task<bool> DeleteByMessageId(string messageId, string guildId)
    {
        return pinDataLayer.DeleteByMessageId(messageId, guildId);
    }

    public Task<bool> DeleteByPinId(string pinMessageId, string guildId)
    {
        return pinDataLayer.DeleteByPinId(pinMessageId, guildId);
    }

    public Task<PinnedMessage?> GetPinnedMessage(string messageId, string guildId)
    {
        return pinDataLayer.GetPinnedMessage(messageId, guildId);
    }

    public Task<PinnedMessage?> GetPinnedMessageByPinId(string pinId, string guildId)
    {
        return pinDataLayer.GetPinnedMessageByPinId(pinId, guildId);
    }

    public Task<bool> SetWebhook(string webhookId, string guildId, string token, string channelId)
    {
        return pinDataLayer.SetWebhook(webhookId, guildId, token, channelId);
    }

    public Task<Webhook?> GetWebhook(string guildId)
    {
        return pinDataLayer.GetWebhook(guildId);
    }

    public Task<Settings?> GetSettings(string guildId)
    {
        return pinDataLayer.GetSettings(guildId);
    }

    public Task<bool> SaveSettings(string guildId, bool enableAutoMode)
    {
        return pinDataLayer.SaveSettings(guildId, enableAutoMode);
    }

    public Task<bool> SetPinVoteCount(string guildId, int pinVoteCount)
    {
        return pinDataLayer.SetPinVoteCount(guildId, pinVoteCount);
    }
}