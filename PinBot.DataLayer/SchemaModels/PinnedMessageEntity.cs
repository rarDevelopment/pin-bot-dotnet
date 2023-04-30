using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PinBot.Models;

namespace PinBot.DataLayer.SchemaModels;

public class PinnedMessageEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    [BsonElement("messageId")]
    public string? MessageId { get; set; }
    [BsonElement("guildId")]
    public string GuildId { get; set; }
    [BsonElement("channelId")]
    public string ChannelId { get; set; }
    [BsonElement("pinnedDate")]
    public string PinnedDate { get; set; }
    [BsonElement("pinUserSnowflake")]
    public string PinningUserId { get; set; }
    [BsonElement("newPinMessageId")]
    public string NewPinMessageId { get; set; }
    [BsonElement("pinChannelId")]
    public string PinChannelId { get; set; }

    public PinnedMessage ToDomain()
    {
        return new PinnedMessage
        {
            GuildId = GuildId,
            ChannelId = ChannelId,
            PinnedDate = PinnedDate,
            MessageId = MessageId,
            NewPinMessageId = NewPinMessageId,
            PinChannelId = PinChannelId,
            PinningUserId = PinningUserId
        };
    }
}