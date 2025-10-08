using Game.Persistence.Mongo;
using Game.Persistence.Mongo.Repositories;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var ctx = MongoContext.CreateFromEnv();
var profiles = new ProfileRepository(ctx);
var saves = new SaveGameRepository(ctx);

// ---- DEMO ----
async Task DemoAsync()
{
    Console.WriteLine("== Création de profil ==");
    var (okCreate, errCreate) = await profiles.CreateAsync("alice", "S3cret!");
    Console.WriteLine(okCreate ? "Profil créé." : $"Erreur: {errCreate}");

    Console.WriteLine("\n== Création doublon ==");
    var (okDup, errDup) = await profiles.CreateAsync("alice", "whatever");
    Console.WriteLine(okDup ? "Inattendu !" : $"OK: {errDup}");

    Console.WriteLine("\n== Vérification mot de passe ==");
    var (okLogin, errLogin) = await profiles.VerifyAsync("alice", "S3cret!");
    Console.WriteLine(okLogin ? "Authentifié." : $"Erreur: {errLogin}");

    Console.WriteLine("\n== Sauvegarde 1 (insertion) ==");
    var (okSave1, errSave1) = await saves.UpsertAsync("alice", 1234);
    Console.WriteLine(okSave1 ? "Sauvegarde faite." : $"Erreur: {errSave1}");

    Console.WriteLine("\n== Sauvegarde 2 (remplacement) ==");
    await Task.Delay(1000);
    var (okSave2, errSave2) = await saves.UpsertAsync("alice", 2222);
    Console.WriteLine(okSave2 ? "Sauvegarde remplacée." : $"Erreur: {errSave2}");

    Console.WriteLine("\n== Chargement ==");
    var (loaded, errLoad) = await saves.LoadAsync("alice");
    Console.WriteLine(loaded is not null
        ? $"Score={loaded.Score}, Date={loaded.LastSaveUtc:O}"
        : $"Erreur: {errLoad}");

    Console.WriteLine("\n== Leaderboard Top 5 ==");
    // Data de test
    await profiles.CreateAsync("bob", "x");
    await profiles.CreateAsync("carol", "x");
    await profiles.CreateAsync("dave", "x");
    await profiles.CreateAsync("erin", "x");

    await saves.UpsertAsync("bob", 5000);
    await Task.Delay(10);
    await saves.UpsertAsync("carol", 5000);    // même score, date plus récente
    await saves.UpsertAsync("dave", 100);
    await saves.UpsertAsync("erin", 9000);

    var top = await saves.GetTop5Async();
    int rank = 1;
    foreach (var s in top)
        Console.WriteLine($"{rank++}. {s.Username} — {s.Score} ({s.LastSaveUtc:yyyy-MM-dd HH:mm:ss} UTC)");
}

try
{
    await DemoAsync();
}
catch (MongoDB.Driver.MongoConnectionException)
{
    Console.WriteLine("Impossible de se connecter à MongoDB. Vérifie que le service est démarré et la chaîne de connexion correcte.");
}
catch (Exception)
{
    Console.WriteLine("Une erreur inattendue est survenue. Merci de réessayer ou de contacter le support.");
}
