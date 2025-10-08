using Game.Persistence.Mongo.Models;
using MongoDB.Driver;

namespace Game.Persistence.Mongo.Repositories;

public sealed class SaveGameRepository
{
    private readonly MongoContext _ctx;
    public SaveGameRepository(MongoContext ctx) => _ctx = ctx;

    public async Task<(bool ok, string? error)> UpsertAsync(string username, int score)
    {
        try
        {
            var filter = Builders<SaveGame>.Filter.Eq(x => x.Username, username);
            var update = Builders<SaveGame>.Update
                .Set(x => x.Username, username)
                .Set(x => x.Score, score)
                .Set(x => x.LastSaveUtc, DateTime.UtcNow);

            var opts = new FindOneAndUpdateOptions<SaveGame>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After
            };

            _ = await _ctx.Saves.FindOneAndUpdateAsync(filter, update, opts);
            return (true, null);
        }
        catch (Exception)
        {
            return (false, "Sauvegarde impossible (problème de base de données).");
        }
    }

    public async Task<(SaveGame? save, string? error)> LoadAsync(string username)
    {
        try
        {
            var save = await _ctx.Saves.Find(x => x.Username == username).FirstOrDefaultAsync();
            if (save is null) return (null, "Aucune sauvegarde trouvée pour ce profil.");
            return (save, null);
        }
        catch (Exception)
        {
            return (null, "Chargement impossible (problème de base de données).");
        }
    }

    public async Task<IReadOnlyList<SaveGame>> GetTop5Async()
    {
        try
        {
            return await _ctx.Saves.Find(FilterDefinition<SaveGame>.Empty)
                .SortByDescending(x => x.Score)
                .ThenByDescending(x => x.LastSaveUtc)
                .Limit(5)
                .ToListAsync();
        }
        catch
        {
            return Array.Empty<SaveGame>();
        }
    }
}
