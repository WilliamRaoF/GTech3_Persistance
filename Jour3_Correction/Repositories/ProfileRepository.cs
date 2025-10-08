using Game.Persistence.Mongo.Models;
using Game.Persistence.Mongo.Security;
using MongoDB.Driver;

namespace Game.Persistence.Mongo.Repositories;

public sealed class ProfileRepository
{
    private readonly MongoContext _ctx;
    public ProfileRepository(MongoContext ctx) => _ctx = ctx;

    public async Task<(bool ok, string? error)> CreateAsync(string username, string password)
    {
        try
        {
            var (hash, salt, it) = Pbkdf2.HashPassword(password);
            var p = new Profile
            {
                Username = username,
                PasswordHash = hash,
                PasswordSalt = salt,
                Iterations = it,
                CreatedUtc = DateTime.UtcNow
            };
            await _ctx.Profiles.InsertOneAsync(p);
            return (true, null);
        }
        catch (MongoWriteException mwx) when (mwx.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return (false, "Ce nom d’utilisateur est déjà pris. Choisis-en un autre.");
        }
        catch (Exception)
        {
            return (false, "Impossible de créer le profil pour le moment (problème de base de données).");
        }
    }

    public async Task<(bool ok, string? error)> VerifyAsync(string username, string password)
    {
        try
        {
            var profile = await _ctx.Profiles
                .Find(x => x.Username == username)
                .FirstOrDefaultAsync();

            if (profile is null) return (false, "Profil introuvable.");

            var ok = Pbkdf2.Verify(password, profile.PasswordHash, profile.PasswordSalt, profile.Iterations);
            return ok ? (true, null) : (false, "Mot de passe incorrect.");
        }
        catch (Exception)
        {
            return (false, "Vérification impossible pour le moment (problème de base de données).");
        }
    }
}
