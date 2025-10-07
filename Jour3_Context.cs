using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;


var context = new MongoContext("mongodb://localhost:27017", "game");

var newProfile = new ProfileDoc
{
    Username = "Alice"
};

await context.Profiles.InsertOneAsync(newProfile);

public class MongoContext
{
    public IMongoDatabase Db { get; }
    public IMongoCollection<ProfileDoc> Profiles => Db.GetCollection<ProfileDoc>("profiles");

    public MongoContext(string connectionString, string dbName)
    {
        var settings = MongoClientSettings.FromConnectionString(connectionString);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(3);
        settings.ConnectTimeout = TimeSpan.FromSeconds(3);
        settings.WaitQueueTimeout = TimeSpan.FromSeconds(5);

        var client = new MongoClient(settings);
        Db = client.GetDatabase(dbName);
    }
}

public class ProfileDoc
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;
    public string Username { get; set; } = default!;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
