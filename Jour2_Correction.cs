using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

const string ProfileFile = "profile.json";
const string SaveFile = "save.enc";

Profile? profile = File.Exists(ProfileFile) ? LoadProfile() : null;
SaveGame? save = null;

while (true)
{
    Console.WriteLine("\n=== MENU ===");
    if (profile is null)
    {
        Console.WriteLine("1. Créer un profil");
        Console.WriteLine("0. Quitter");
        Console.Write("> ");
        var choice = Console.ReadLine();
        if (choice == "1") profile = CreateProfile();
        else if (choice == "0") break;
    }
    else
    {
        Console.WriteLine($"Profil : {profile.Username}");
        Console.WriteLine("1. Jouer (modifier score)");
        Console.WriteLine("2. Sauvegarder (AES-GCM)");
        Console.WriteLine("3. Charger");
        Console.WriteLine("4. Réinitialiser profil");
        Console.WriteLine("0. Quitter");
        Console.Write("> ");
        var choice = Console.ReadLine();

        switch (choice)
        {
            case "1":
                save ??= new SaveGame { PlayerName = profile.Username };
                save.Score += 10;
                Console.WriteLine($"Score = {save.Score}");
                break;

            case "2":
                if (save is null) { Console.WriteLine("Rien à sauvegarder."); break; }
                Console.Write("Mot de passe : ");
                var pwd = ReadHidden();
                EncryptAndSave(save, pwd, profile.SaltB64);
                Console.WriteLine("Sauvegarde chiffrée écrite.");
                break;

            case "3":
                Console.Write("Mot de passe : ");
                var input = ReadHidden();
                try
                {
                    save = DecryptAndLoad(input);
                    Console.WriteLine($"Chargé: {save.PlayerName} | Score={save.Score}");
                }
                catch (Exception ex) when (ex is CryptographicException || ex is JsonException)
                {
                    Console.WriteLine("Mot de passe incorrect ou fichier corrompu.");
                }
                catch (FileNotFoundException)
                {
                    Console.WriteLine("Aucune sauvegarde à charger.");
                }
                break;

            case "4":
                File.Delete(ProfileFile);
                File.Delete(SaveFile);
                profile = null;
                save = null;
                Console.WriteLine("Profil supprimé.");
                break;

            case "0": return;
        }
    }
}

static Profile CreateProfile()
{
    Console.Write("Nom d'utilisateur : ");
    var username = Console.ReadLine() ?? "Player";

    Console.Write("Mot de passe : ");
    var password = ReadHidden();

    var (hash, salt) = HashPassword(password);
    var p = new Profile(username, hash, salt, DateTime.UtcNow);

    File.WriteAllText(ProfileFile, JsonSerializer.Serialize(p, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine("Profil créé.");
    return p;
}

static Profile LoadProfile()
{
    var json = File.ReadAllText(ProfileFile);
    return JsonSerializer.Deserialize<Profile>(json)!;
}

static (string hashB64, string saltB64) HashPassword(string password)
{
    byte[] salt = RandomNumberGenerator.GetBytes(16);
    using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
    byte[] hash = pbkdf2.GetBytes(32);
    return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
}

static byte[] DeriveKey(string password, string saltB64)
{
    var salt = Convert.FromBase64String(saltB64);
    using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
    return pbkdf2.GetBytes(32);
}

static void EncryptAndSave(SaveGame save, string password, string saltB64)
{
    var json = JsonSerializer.Serialize(save);
    byte[] plaintext = Encoding.UTF8.GetBytes(json);
    byte[] key = DeriveKey(password, saltB64);

    byte[] nonce = RandomNumberGenerator.GetBytes(12);
    byte[] ciphertext = new byte[plaintext.Length];
    byte[] tag = new byte[16];

    using var aes = new AesGcm(key);
    aes.Encrypt(nonce, plaintext, ciphertext, tag);

    var payload = new
    {
        Salt = saltB64,
        Nonce = Convert.ToBase64String(nonce),
        Tag = Convert.ToBase64String(tag),
        Data = Convert.ToBase64String(ciphertext)
    };

    var encJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(SaveFile, encJson);
}

static SaveGame DecryptAndLoad(string password)
{
    if (!File.Exists(SaveFile)) throw new FileNotFoundException();

    var payload = JsonSerializer.Deserialize<EncryptedPayload>(File.ReadAllText(SaveFile))!;
    byte[] key = DeriveKey(password, payload.Salt);

    byte[] nonce = Convert.FromBase64String(payload.Nonce);
    byte[] tag = Convert.FromBase64String(payload.Tag);
    byte[] data = Convert.FromBase64String(payload.Data);

    byte[] plaintext = new byte[data.Length];
    using var aes = new AesGcm(key);
    aes.Decrypt(nonce, data, tag, plaintext);

    var json = Encoding.UTF8.GetString(plaintext);
    return JsonSerializer.Deserialize<SaveGame>(json)!;
}

static string ReadHidden()
{
    var pwd = new StringBuilder();
    ConsoleKeyInfo key;
    while ((key = Console.ReadKey(true)).Key != ConsoleKey.Enter)
    {
        if (key.Key == ConsoleKey.Backspace && pwd.Length > 0)
        {
            pwd.Length--;
            Console.Write("\b \b");
        }
        else if (!char.IsControl(key.KeyChar))
        {
            pwd.Append(key.KeyChar);
            Console.Write("*");
        }
    }
    Console.WriteLine();
    return pwd.ToString();
}

public record Profile(string Username, string PasswordHashB64, string SaltB64, DateTime CreatedUtc);

public class SaveGame
{
    public string PlayerName { get; set; } = "Player";
    public int Level { get; set; } = 1;
    public int Score { get; set; } = 0;
    public DateTime LastSaveUtc { get; set; } = DateTime.UtcNow;
}

public class EncryptedPayload
{
    public string Salt { get; set; } = string.Empty;
    public string Nonce { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
}
