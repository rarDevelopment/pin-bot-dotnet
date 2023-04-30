using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using PinBot.Models;

namespace PinBot.DataLayer.SchemaModels;

public class WebhookEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    [BsonElement("webhookId")]
    public string? WebhookId { get; set; }
    [BsonElement("token")]
    public string? Token { get; set; }
    [BsonElement("guildId")]
    public string GuildId { get; set; }
    [BsonElement("channelId")]
    public string ChannelId { get; set; }

    public Webhook ToDomain()
    {
        return new Webhook
        {
            WebhookId = WebhookId,
            Token = Token,
            GuildId = GuildId,
            ChannelId = ChannelId,
        };
    }
}