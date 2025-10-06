# Persistance de données  
## Sécurité et protection des données  

---

## 1. Objectifs pédagogiques

- Comprendre la différence entre hash et chiffrement  
- Savoir hasher et vérifier des mots de passe correctement  
- Découvrir un chiffrement symétrique simple (AES-GCM) pour protéger une sauvegarde  
- Intégrer ces notions dans une petite application console .NET

---

## 2. Principes de sécurité des données

Trois besoins fondamentaux :
1. Confidentialité → chiffrement  
2. Intégrité → détection de modification  
3. Authentification → hash des mots de passe

Différences entre hash et chiffrement :

|               | Hash                    | Chiffrement                   |
|---------------|--------------------------|--------------------------------|
| Sens          | Irréversible             | Réversible                    |
| Usage typique | Mots de passe            | Fichiers, messages            |
| Exemples      | PBKDF2, BCrypt, SHA256   | AES, RSA                      |

Pourquoi ne jamais stocker un mot de passe en clair.  
Pourquoi ne pas chiffrer un mot de passe mais le hasher + vérifier.

---

## 3. Hash sécurisé de mots de passe

### a) PBKDF2 intégré au .NET BCL

#### 1. Pourquoi PBKDF2

Lorsqu'un utilisateur choisit un mot de passe, on ne doit **jamais** le stocker en clair.  
De même, faire simplement un `SHA256(password)` n'est pas sécurisé :

- Trop rapide → vulnérable aux attaques par force brute (GPU, rainbow tables)  
- Pas de sel → deux utilisateurs avec le même mot de passe auront le même hash

Pour pallier à cela, on utilise **PBKDF2** (Password-Based Key Derivation Function 2) :

- Utilise un mot de passe + un **sel aléatoire unique**
- Effectue un grand nombre d'itérations (par exemple 100 000)
- Produit un **hash sécurisé** difficile à casser
- Fournit le résultat sous forme de tableau d'octets

.NET inclut PBKDF2 via la classe **`Rfc2898DeriveBytes`** dans `System.Security.Cryptography`.

---

#### 2. Principe d'utilisation

##### Étape 1 : Création de mot de passe
- Générer un **sel aléatoire** (non secret)
- Appliquer PBKDF2 sur le mot de passe + le sel
- Sauvegarder le hash et le sel (en Base64) dans la base ou un fichier

##### Étape 2 : Vérification à la connexion
- Récupérer le sel et le hash stockés
- Recalculer PBKDF2 avec le mot de passe fourni et le sel stocké
- Comparer le résultat avec le hash enregistré en **temps constant**

---

#### 3. Exemple complet en C#

```csharp
using System;
using System.Security.Cryptography;
using System.Text;

public static class PasswordService
{
    private const int SaltSize = 16;        // 128 bits
    private const int HashSize = 32;        // 256 bits
    private const int Iterations = 100_000; // nombre d'itérations PBKDF2

    // Génère le hash et le sel à partir du mot de passe
    public static (string hashB64, string saltB64) HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

        var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        byte[] hash = pbkdf2.GetBytes(HashSize);

        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    // Vérifie si un mot de passe correspond au hash stocké
    public static bool VerifyPassword(string password, string storedHashB64, string storedSaltB64)
    {
        byte[] salt = Convert.FromBase64String(storedSaltB64);
        byte[] storedHash = Convert.FromBase64String(storedHashB64);

        var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        byte[] computedHash = pbkdf2.GetBytes(HashSize);

        return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
    }
}
````

---

#### 4. Exemple d'utilisation (Program.cs)

```csharp
Console.Write("Mot de passe à hasher : ");
string pwd = Console.ReadLine() ?? "";

var (hash, salt) = PasswordService.HashPassword(pwd);
Console.WriteLine($"Hash : {hash}");
Console.WriteLine($"Salt : {salt}");

Console.Write("\nTest de vérification - retapez le mot de passe : ");
string input = Console.ReadLine() ?? "";

bool ok = PasswordService.VerifyPassword(input, hash, salt);
Console.WriteLine(ok ? "Mot de passe correct" : "Mot de passe incorrect");
```

---

#### 5. Exemple de stockage JSON

Exemple d'une entrée dans un fichier `profiles.json` :

```json
[
  {
    "Username": "alice",
    "PasswordHash": "Q8nyH8YUs1VgPaqtLF6gqX8ydSnTnEnXbRJDU2j6aL0=",
    "Salt": "3qAzg2Ytkz17AoYcXuz5rA==",
    "CreatedUtc": "2025-10-07T08:30:00Z"
  }
]
```

---

#### 6. Bonnes pratiques

| Élément                       | Rôle                                                    |
| ----------------------------- | ------------------------------------------------------- |
| Sel unique et aléatoire       | Rend chaque hash unique, empêche les rainbow tables     |
| Itérations élevées (100k)     | Ralentit les attaques par force brute                   |
| Base64                        | Simplifie le stockage en JSON ou BDD                    |
| Comparaison en temps constant | Empêche les attaques par mesure de temps                |
| Hash + sel uniquement         | Ne jamais stocker le mot de passe ou la clé directement |

---

#### 7. À retenir

* PBKDF2 est une fonction de dérivation lente et robuste pour protéger les mots de passe.
* Elle est disponible nativement dans .NET via `Rfc2898DeriveBytes`.
* Le sel n’est pas secret, mais il doit être unique et aléatoire.
* Le hash et le sel sont stockés ensemble (par exemple en JSON).
* Pour vérifier un mot de passe, on refait PBKDF2 avec le même sel et on compare les résultats.


### b) Alternative avec BCrypt.Net-Next

Installer le package :

```bash
dotnet add package BCrypt.Net-Next
```

Utilisation :

```csharp
string hash = BCrypt.Net.BCrypt.HashPassword(password);
bool ok = BCrypt.Net.BCrypt.Verify(inputPassword, hash);
```

Remarque : les sels sont générés automatiquement et intégrés au hash.

---

## 4. Chiffrement symétrique simple : AES-GCM

### 1. Pourquoi chiffrer

Lorsqu'on stocke des données sensibles (par exemple une sauvegarde de jeu contenant des informations personnelles, des scores ou un mot de passe hashé), il faut assurer la **confidentialité**.  
Même si un fichier tombe entre de mauvaises mains, il ne doit pas être lisible sans le mot de passe ou la clé.

Le chiffrement symétrique permet de transformer un texte lisible en données illisibles (chiffrement), et de refaire l'opération inverse (déchiffrement) avec la **même clé secrète**.

Exemple d’usage dans un projet console :
- L'utilisateur entre un mot de passe.
- On dérive une **clé symétrique** depuis ce mot de passe.
- On chiffre la sauvegarde JSON avec cette clé.
- Pour relire la sauvegarde, il faut le bon mot de passe.

---

### 2. Pourquoi AES-GCM

AES (Advanced Encryption Standard) est un standard moderne et très performant.  
Le mode **GCM (Galois/Counter Mode)** offre :

- Chiffrement sécurisé et rapide  
- Intégrité authentifiée (détection automatique des modifications ou mauvais mots de passe)  
- Pas besoin de MAC séparé

.NET supporte AES-GCM nativement via la classe `AesGcm` dans `System.Security.Cryptography`.

---

### 3. Concepts clés

| Terme        | Taille typique | Rôle                                                                 |
|-------------|---------------|----------------------------------------------------------------------|
| Clé         | 256 bits (32 B) | Dérivée du mot de passe avec PBKDF2                                 |
| Sel         | 128 bits (16 B) | Stocké en clair, sert à dériver la même clé à chaque chargement     |
| Nonce       | 96 bits (12 B) | Valeur aléatoire unique par chiffrement (équivalent à un IV)       |
| Tag         | 128 bits (16 B) | Vérifie l'intégrité et l'authenticité du message                   |
| Ciphertext  | variable       | Résultat chiffré, illisible sans la clé                             |

Ce qu’il faut **stocker dans le fichier chiffré** :  
- salt  
- nonce  
- tag  
- ciphertext

---

### 4. Étapes de chiffrement avec AES-GCM

#### 1) Générer le sel et dériver la clé
On utilise PBKDF2 pour transformer le mot de passe en une clé 256 bits.

```csharp
using System.Security.Cryptography;

string password = "motDePasseUtilisateur";
byte[] salt = RandomNumberGenerator.GetBytes(16); // 128 bits

var key = new Rfc2898DeriveBytes(
    password,
    salt,
    100_000,
    HashAlgorithmName.SHA256
).GetBytes(32); // 256 bits
````

#### 2) Préparer le plaintext (ex. JSON)

```csharp
using System.Text;

string json = "{\"PlayerName\":\"Alice\",\"Level\":3}";
byte[] plaintext = Encoding.UTF8.GetBytes(json);
```

#### 3) Chiffrer avec AES-GCM

```csharp
using var aes = new AesGcm(key);
byte[] nonce = RandomNumberGenerator.GetBytes(12); // 96 bits
byte[] ciphertext = new byte[plaintext.Length];
byte[] tag = new byte[16]; // 128 bits

aes.Encrypt(nonce, plaintext, ciphertext, tag);
```

À ce stade :

* `ciphertext` contient les données chiffrées
* `tag` sert à vérifier l'intégrité
* `nonce` et `salt` doivent être sauvegardés pour déchiffrer

#### 4) Stocker le résultat

On peut concaténer ou stocker dans un objet, puis le sérialiser.
Par exemple, dans un fichier `.enc` :

```csharp
using System.Text.Json;

var payload = new
{
    Salt = Convert.ToBase64String(salt),
    Nonce = Convert.ToBase64String(nonce),
    Tag = Convert.ToBase64String(tag),
    Data = Convert.ToBase64String(ciphertext)
};

string encryptedJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
File.WriteAllText("save.enc", encryptedJson);
```

---

### 5. Étapes de déchiffrement avec AES-GCM

Pour relire le fichier, on refait les étapes en sens inverse.

#### 1) Charger le fichier et extraire les éléments

```csharp
string encryptedContent = File.ReadAllText("save.enc");
var payload = JsonSerializer.Deserialize<EncryptedPayload>(encryptedContent);

byte[] salt = Convert.FromBase64String(payload.Salt);
byte[] nonce = Convert.FromBase64String(payload.Nonce);
byte[] tag = Convert.FromBase64String(payload.Tag);
byte[] ciphertext = Convert.FromBase64String(payload.Data);
```

#### 2) Redériver la clé depuis le mot de passe et le salt

```csharp
string inputPassword = "motDePasseUtilisateur"; // demandé à l'utilisateur
var key = new Rfc2898DeriveBytes(
    inputPassword,
    salt,
    100_000,
    HashAlgorithmName.SHA256
).GetBytes(32);
```

#### 3) Déchiffrer

```csharp
using var aes = new AesGcm(key);
byte[] decrypted = new byte[ciphertext.Length];

try
{
    aes.Decrypt(nonce, ciphertext, tag, decrypted);
    string json = Encoding.UTF8.GetString(decrypted);
    Console.WriteLine("Fichier déchiffré :");
    Console.WriteLine(json);
}
catch (CryptographicException)
{
    Console.WriteLine("Mot de passe incorrect ou fichier corrompu.");
}
```

#### Modèle utilisé pour la désérialisation

```csharp
public class EncryptedPayload
{
    public string Salt { get; set; } = string.Empty;
    public string Nonce { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
}
```

---

### 6. Résultat dans le fichier `.enc`

Exemple de contenu généré :

```json
{
  "Salt": "RWjD1JbT7tTtObURnWQ3zQ==",
  "Nonce": "lQecvdvvJhmh4toP",
  "Tag": "1Sc2xsdZwNm5uogpn3bQGg==",
  "Data": "xT+UoEyRf8n6Lj2bdFjqDA=="
}
```

Même si on ouvre ce fichier dans VS Code, le contenu est illisible sans le bon mot de passe.

---

### 7. Points clés à retenir

* AES-GCM fournit **chiffrement + authentification intégrée** (via le tag)
* Le **nonce doit être unique** pour chaque chiffrement, mais peut être public
* Le **sel** sert à redériver la clé → il n’est pas secret
* Si le mot de passe est faux ou les données modifiées → une exception `CryptographicException` est levée
* **Le mot de passe n’est jamais stocké**, seule la clé dérivée est utilisée en mémoire temporaire

---

### 8. Exemple d'intégration typique

Lors de la **sauvegarde** :

1. Sérialiser la sauvegarde en JSON
2. Chiffrer avec AES-GCM et stocker dans `save.enc`

Lors du **chargement** :

1. Demander le mot de passe à l'utilisateur
2. Redériver la clé avec PBKDF2 et le sel stocké
3. Déchiffrer, désérialiser et restaurer l'objet

---

### 9. À retenir

| Élément                | Rôle                                                                |
| ---------------------- | ------------------------------------------------------------------- |
| PBKDF2                 | Dériver une clé forte à partir d'un mot de passe                    |
| AES-GCM                | Chiffrer et garantir l'intégrité des données                        |
| Salt + Nonce           | Non secrets mais indispensables pour déchiffrer                     |
| Tag                    | Garantit que les données n'ont pas été modifiées                    |
| CryptographicException | Lève une erreur si le mot de passe est faux ou les données altérées |

---

## 5. Bonnes pratiques

* Ne jamais stocker un mot de passe en clair.
* Toujours hasher avec un sel unique par mot de passe.
* Utiliser une fonction lente et résistante (PBKDF2 / BCrypt).
* Pour chiffrer des fichiers : générer une clé avec PBKDF2 + AES-GCM.
* Gérer les erreurs de déchiffrement proprement (fichier corrompu ou mauvais mot de passe).

---

## TP : Sauvegarde sécurisée 

Objectif : créer une application console qui permet de :

* Créer un profil avec mot de passe hashé
* Sauvegarder les données chiffrées avec AES-GCM dans un fichier
* Charger la sauvegarde en entrant le mot de passe
* Gérer proprement le cas d’un mot de passe incorrect ou d’un fichier corrompu

### Étapes

1. Créer un profil avec username, hash, salt.
2. Hash du mot de passe à la création.
3. Sauvegarde :
   * Sérialiser SaveGame en JSON
   * Dériver une clé depuis le mot de passe
   * Chiffrer et sauvegarder (salt + nonce + tag + données) dans save.enc
4. Chargement :
   * Lire save.enc
   * Extraire salt
   * Dériver la clé
   * Déchiffrer et désérialiser
5. Si mauvais mot de passe : message clair et refus de chargement.

### Exigences minimales

* Le fichier chiffré ne doit pas être lisible en clair.
* L'application doit fonctionner avec au moins un profil et une sauvegarde chiffrée.
* Aucun message d'erreur brut ne doit s'afficher.


