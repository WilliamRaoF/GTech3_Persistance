using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Game.Persistence.Mongo.Models;

public sealed class SaveGame
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonElement("Username")]
    public string Username { get; set; } = default!;

    [BsonElement("Score")]
    public int Score { get; set; }

    [BsonElement("LastSaveUtc")]
    public DateTime LastSaveUtc { get; set; } = DateTime.UtcNow;
}
