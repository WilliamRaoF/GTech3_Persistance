using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Game.Persistence.Mongo.Models;

public sealed class Profile
{
    [BsonId]
    public ObjectId Id { get; set; }

    [BsonElement("Username")]
    public string Username { get; set; } = default!;

    [BsonElement("PasswordHash")]
    public string PasswordHash { get; set; } = default!;

    [BsonElement("PasswordSalt")]
    public string PasswordSalt { get; set; } = default!; 

    [BsonElement("Iterations")]
    public int Iterations { get; set; }

    [BsonElement("CreatedUtc")]
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
