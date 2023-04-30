using PinBot.DataLayer;
using PinBot.Models;

namespace PinBot.BusinessLayer;

public class PinBusinessLayer : IPinBusinessLayer
{
    private readonly IPinDataLayer _pinDataLayer;

    public PinBusinessLayer(IPinDataLayer pinDataLayer)
    {
        _pinDataLayer = pinDataLayer;
    }

    public Task<bool> SavePin(
        string messageId,
        string guildId,
        string channelId,
        string userId,
        DateTime datePinned,
        string newPinMessageId,
        string pinChannelId)
    {
        return _pinDataLayer.SavePin(messageId, guildId, channelId, userId, datePinned, newPinMessageId, pinChannelId);
    }

    public Task<bool> DeleteByMessageId(string messageId, string guildId)
    {
        return _pinDataLayer.DeleteByMessageId(messageId, guildId);
    }

    public Task<bool> DeleteByPinId(string pinMessageId, string guildId)
    {
        return _pinDataLayer.DeleteByPinId(pinMessageId, guildId);
    }

    public Task<PinnedMessage?> GetPinnedMessage(string messageId, string guildId)
    {
        return _pinDataLayer.GetPinnedMessage(messageId, guildId);
    }

    public Task<PinnedMessage?> GetPinnedMessageByPinId(string pinId, string guildId)
    {
        return _pinDataLayer.GetPinnedMessageByPinId(pinId, guildId);
    }

    public Task<bool> SetWebhook(string webhookId, string guildId, string token, string channelId)
    {
        return _pinDataLayer.SetWebhook(webhookId, guildId, token, channelId);
    }

    public Task<Webhook?> GetWebhook(string guildId)
    {
        return _pinDataLayer.GetWebhook(guildId);
    }

    public Task<Settings?> GetSettings(string guildId)
    {
        return _pinDataLayer.GetSettings(guildId);
    }

    public Task<bool> SaveSettings(string guildId, bool enableAutoMode)
    {
        return _pinDataLayer.SaveSettings(guildId, enableAutoMode);
    }
}