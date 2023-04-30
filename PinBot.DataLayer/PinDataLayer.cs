using MongoDB.Driver;
using PinBot.DataLayer.SchemaModels;
using PinBot.Models;

namespace PinBot.DataLayer;

public class PinDataLayer : IPinDataLayer
{
    private readonly IMongoCollection<PinnedMessageEntity> _pinnedMessageCollection;
    private readonly IMongoCollection<SettingsEntity> _settingsCollection;
    private readonly IMongoCollection<WebhookEntity> _webhookCollection;

    public PinDataLayer(DatabaseSettings databaseSettings)
    {
        var connectionString = $"mongodb+srv://{databaseSettings.User}:{databaseSettings.Password}@{databaseSettings.Cluster}.mongodb.net/{databaseSettings.Name}?w=majority";
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseSettings.Name);
        _pinnedMessageCollection = database.GetCollection<PinnedMessageEntity>("messages");
        _settingsCollection = database.GetCollection<SettingsEntity>("settings");
        _webhookCollection = database.GetCollection<WebhookEntity>("webhooks");
    }

    public async Task<bool> SavePin(
        string messageId,
        string guildId,
        string channelId,
        string userId,
        DateTime datePinned,
        string newPinMessageId,
        string pinChannelId)
    {
        var filter = Builders<PinnedMessageEntity>.Filter.Eq(p => p.MessageId, messageId);
        var foundPinnedMessage = await _pinnedMessageCollection.Find(filter).FirstOrDefaultAsync();
        if (foundPinnedMessage != null)
        {
            return false;
        }

        try
        {
            await _pinnedMessageCollection.InsertOneAsync(new PinnedMessageEntity
            {
                MessageId = messageId,
                GuildId = guildId,
                ChannelId = channelId,
                PinningUserId = userId,
                NewPinMessageId = newPinMessageId,
                PinChannelId = pinChannelId,
                PinnedDate = datePinned.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'")
                //DateTime.ParseExact(accessTokenResponse.ExpireTime, "yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
            });
            return true;
        }
        catch (Exception ex)
        {
            // TODO: log?
            return false;
        }
    }

    public async Task<bool> DeleteByMessageId(string messageId, string guildId)
    {
        var filter = Builders<PinnedMessageEntity>.Filter.Eq(p => p.MessageId, messageId);
        var deleteResult = await _pinnedMessageCollection.DeleteOneAsync(filter);
        return deleteResult.DeletedCount == 1;
    }

    public async Task<bool> DeleteByPinId(string pinMessageId, string guildId)
    {
        var filter = Builders<PinnedMessageEntity>.Filter.Eq(p => p.NewPinMessageId, pinMessageId);
        var deleteResult = await _pinnedMessageCollection.DeleteOneAsync(filter);
        return deleteResult.DeletedCount == 1;
    }

    public async Task<PinnedMessage?> GetPinnedMessage(string messageId, string guildId)
    {
        var filter = Builders<PinnedMessageEntity>.Filter.And(
            Builders<PinnedMessageEntity>.Filter.Where(pm => pm.MessageId == messageId),
            Builders<PinnedMessageEntity>.Filter.Where(pm => pm.GuildId == guildId)
        );
        var pinnedMessage = await _pinnedMessageCollection.Find(filter).FirstOrDefaultAsync();
        return pinnedMessage?.ToDomain() ?? null;
    }

    public async Task<PinnedMessage?> GetPinnedMessageByPinId(string pinId, string guildId)
    {
        var filter = Builders<PinnedMessageEntity>.Filter.And(
            Builders<PinnedMessageEntity>.Filter.Where(pm => pm.NewPinMessageId == pinId),
            Builders<PinnedMessageEntity>.Filter.Where(pm => pm.GuildId == guildId)
        );
        var pinnedMessage = await _pinnedMessageCollection.Find(filter).FirstOrDefaultAsync();
        return pinnedMessage?.ToDomain() ?? null;
    }

    public async Task<bool> SetWebhook(string webhookId, string guildId, string token, string channelId)
    {
        var webhook = await GetExistingWebhook(guildId);
        if (webhook != null)
        {
            var filter = Builders<WebhookEntity>.Filter.Eq(w => w.GuildId, guildId);
            var update = Builders<WebhookEntity>.Update
                .Set(w => w.WebhookId, webhookId)
                .Set(w => w.Token, token)
                .Set(w => w.ChannelId, channelId)
                .Set(w => w.GuildId, guildId);
            var updateResult = await _webhookCollection.UpdateOneAsync(filter, update);
            return updateResult.MatchedCount == 1 && updateResult.ModifiedCount == 1;
        }

        webhook = new WebhookEntity
        {
            WebhookId = webhookId,
            Token = token,
            ChannelId = channelId,
            GuildId = guildId
        };

        try
        {
            await _webhookCollection.InsertOneAsync(webhook);
        }
        catch (Exception ex)
        {
            // TODO: log?
            return false;
        }
        return true;
    }

    private async Task<WebhookEntity?> GetExistingWebhook(string guildId)
    {
        var filter = Builders<WebhookEntity>.Filter.Eq(p => p.GuildId, guildId);
        return await _webhookCollection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<Webhook?> GetWebhook(string guildId)
    {
        return (await GetExistingWebhook(guildId))?.ToDomain() ?? null;
    }

    private async Task<SettingsEntity?> GetExistingSettings(string guildId)
    {
        var filter = Builders<SettingsEntity>.Filter.Eq(p => p.GuildId, guildId);
        return await _settingsCollection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<Settings?> GetSettings(string guildId)
    {
        return (await GetExistingSettings(guildId))?.ToDomain() ?? null;
    }

    public async Task<bool> SaveSettings(string guildId, bool enableAutoMode)
    {
        var settings = await GetExistingSettings(guildId);
        if (settings != null)
        {
            var filter = Builders<SettingsEntity>.Filter.Eq(w => w.GuildId, guildId);
            var update = Builders<SettingsEntity>.Update
                .Set(w => w.GuildId, guildId)
                .Set(w => w.EnableAutoMode, enableAutoMode);
            var updateResult = await _settingsCollection.UpdateOneAsync(filter, update);
            return updateResult.MatchedCount == 1 && updateResult.ModifiedCount == 1;
        }

        settings = new SettingsEntity
        {
            GuildId = guildId,
            EnableAutoMode = enableAutoMode
        };

        try
        {
            await _settingsCollection.InsertOneAsync(settings);
        }
        catch (Exception ex)
        {
            // TODO: log?
            return false;
        }
        return true;
    }
}