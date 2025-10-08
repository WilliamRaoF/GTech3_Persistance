using MongoDB.Driver;

namespace Game.Persistence.Mongo;

public sealed class MongoContext
{
    public IMongoDatabase Db { get; }

    public IMongoCollection<Models.Profile> Profiles => Db.GetCollection<Models.Profile>("profiles");
    public IMongoCollection<Models.SaveGame> Saves => Db.GetCollection<Models.SaveGame>("saves");

    public MongoContext(string connectionString, string databaseName)
    {
        var settings = MongoClientSettings.FromConnectionString(connectionString);
        settings.RetryWrites = true;
        var client = new MongoClient(settings);
        Db = client.GetDatabase(databaseName);

        EnsureIndexes();
    }

    void EnsureIndexes()
    {
        // Index unique sur Username (profiles)
        var idxProfiles = new CreateIndexModel<Models.Profile>(
            Builders<Models.Profile>.IndexKeys.Ascending(x => x.Username),
            new CreateIndexOptions { Unique = true, Name = "ux_profiles_username" }
        );
        Profiles.Indexes.CreateOne(idxProfiles);

        // Index sur Username dans saves + tri utile
        var idxSavesUser = new CreateIndexModel<Models.SaveGame>(
            Builders<Models.SaveGame>.IndexKeys.Ascending(x => x.Username),
            new CreateIndexOptions { Name = "ix_saves_username" }
        );
        Saves.Indexes.CreateOne(idxSavesUser);

        var idxLeaderboard = new CreateIndexModel<Models.SaveGame>(
            Builders<Models.SaveGame>.IndexKeys
                .Descending(x => x.Score)
                .Descending(x => x.LastSaveUtc),
            new CreateIndexOptions { Name = "ix_leaderboard_score_date" }
        );
        Saves.Indexes.CreateOne(idxLeaderboard);
    }

    public static MongoContext CreateFromEnv() =>
        new(Environment.GetEnvironmentVariable("MONGO_CONN") ?? "mongodb://localhost:27017",
            Environment.GetEnvironmentVariable("MONGO_DB") ?? "game");
}
