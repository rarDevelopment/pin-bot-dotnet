using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using PinBot.Models;

namespace PinBot.DataLayer.SchemaModels;

public class SettingsEntity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    [BsonElement("guildId")]
    public string GuildId { get; set; }
    [BsonElement("enableAutoMode")]
    public bool EnableAutoMode { get; set; }

    public Settings ToDomain()
    {
        return new Settings
        {
            GuildId = GuildId,
            EnableAutoMode = EnableAutoMode
        };
    }
}