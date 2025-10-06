using System.Text.Json;

var dir = "Saves";
Directory.CreateDirectory(dir);
var path = Path.Combine(dir, "save.json");

var save = SaveService.LoadOrDefault(path);
Console.WriteLine($"Save chargée pour: {save.PlayerName} | Niveau {save.Level} | Score {save.Score}");

while (true)
{
    Console.WriteLine("\n=== Menu ===");
    Console.WriteLine("1. Nouvelle partie");
    Console.WriteLine("2. Charger");
    Console.WriteLine("3. Jouer (+10 score)");
    Console.WriteLine("4. Sauvegarder");
    Console.WriteLine("0. Quitter");
    Console.Write("> Votre choix: ");
    var input = Console.ReadLine()?.Trim();

    switch (input)
    {
        case "1":
            Console.Write("Nom du joueur: ");
            var name = Console.ReadLine();
            save = SaveGame.Default(name);
            Console.WriteLine($"Nouvelle partie : {save.PlayerName} (Score {save.Score})");
            break;

        case "2":
            save = SaveService.LoadOrDefault(path);
            Console.WriteLine($"Chargé: {save.PlayerName} | Niveau {save.Level} | Score {save.Score}");
            break;

        case "3":
            save.Score += 10;
            Console.WriteLine($"Vous jouez... Score = {save.Score}");
            break;

        case "4":
            SaveService.Save(path, save);
            Console.WriteLine($"Sauvegardé ✓ ({save.LastSaveUtc:yyyy-MM-dd HH:mm:ss} UTC)");
            break;

        case "0":
            Console.WriteLine("Au revoir !");
            return;

        default:
            Console.WriteLine("Choix invalide.");
            break;
    }
}

/* ======= Modèles & Service ======= */

public class SaveGame
{
    public string PlayerName { get; set; } = "Player";
    public int Level { get; set; } = 1;
    public int Score { get; set; } = 0;
    public DateTime LastSaveUtc { get; set; } = DateTime.UtcNow;

    public static SaveGame Default(string? name = null) =>
        new SaveGame { PlayerName = string.IsNullOrWhiteSpace(name) ? "Player" : name.Trim() };
}

public static class SaveService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    public static SaveGame LoadOrDefault(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                Console.WriteLine("Aucune sauvegarde trouvée. Création d'une sauvegarde par défaut.");
                var def = SaveGame.Default();
                Save(path, def);
                return def;
            }

            var json = File.ReadAllText(path);
            var loaded = JsonSerializer.Deserialize<SaveGame>(json, Options);

            if (loaded is null)
            {
                Console.WriteLine("Sauvegarde vide/invalide. Réinitialisation.");
                var def = SaveGame.Default();
                Save(path, def);
                return def;
            }

            return loaded;
        }
        catch (JsonException)
        {
            Console.WriteLine("JSON corrompu. Réinitialisation d'une sauvegarde propre.");
            var def = SaveGame.Default();
            Save(path, def);
            return def;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur d'accès fichier: {ex.Message}");
            Console.WriteLine("Reprise avec une sauvegarde par défaut.");
            var def = SaveGame.Default();
            Save(path, def);
            return def;
        }
    }

    public static void Save(string path, SaveGame save)
    {
        save.LastSaveUtc = DateTime.UtcNow;
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        var tmp = path + ".tmp";
        var json = JsonSerializer.Serialize(save, Options);

        File.WriteAllText(tmp, json);
        // Remplacement "quasi-atomique" simple (suffisant pour ce TP)
        File.Move(tmp, path, overwrite: true);
    }
}
