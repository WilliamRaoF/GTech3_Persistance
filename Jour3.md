# Persistance des données  
## Bases de données NoSQL avec MongoDB en .NET  

---

## Objectifs pédagogiques

- Comprendre les **principes fondamentaux des bases NoSQL orientées documents**.  
- Savoir **modéliser des documents MongoDB** pour un projet concret (profils + sauvegardes de jeu).  
- Mettre en place une **connexion MongoDB** dans une application .NET et réaliser les opérations CRUD basiques.  
- Créer et utiliser des **index** pour garantir des contraintes d’unicité et améliorer les performances de requêtes.  
- Écrire et exécuter des **requêtes utiles**, notamment un leaderboard simple basé sur les scores.  
- Mettre en œuvre MongoDB comme **alternative aux fichiers** utilisés dans les jours précédents.

---

## Partie 1 – Théorie et démos

### 1. Introduction et positionnement

Avant d’entrer dans la pratique, il est essentiel de **comprendre la différence entre SQL et NoSQL**.

- **Bases SQL** :  
  - Reposent sur un **schéma fixe**, des **tables** et des **relations** bien définies.  
  - Les données sont **structurées** (types, colonnes, contraintes).  
  - Utilisent le **langage SQL** pour manipuler les données (SELECT, JOIN, etc.).  
  - Exemple : MySQL, PostgreSQL, SQL Server.

- **Bases NoSQL** (Not Only SQL) :  
  - Permettent de **stocker des données non structurées ou semi-structurées**.  
  - Le schéma est **souple ou inexistant** : chaque document peut avoir une structure différente.  
  - Pas ou peu de jointures : les données sont souvent **regroupées dans un seul document**.  
  - Exemples : MongoDB (documents), Redis (clé/valeur), Cassandra (colonnes larges).

**Pourquoi MongoDB ici ?**  
- Parce que notre application de jeu manipule déjà des **objets JSON** (sérialisation en fichiers).  
- MongoDB stocke ses données en **BSON** (Binary JSON) → adaptation naturelle.  
- Elle permet une **évolution du modèle** sans migration lourde, ce qui correspond bien à un projet pédagogique où les structures changent souvent.

---

### 2. Modélisation de documents

MongoDB ne fonctionne pas avec des tables mais avec des **collections** de **documents** (équivalent d’objets JSON).

#### a. Quelles collections utiliser ?

Pour un jeu simple avec profils et sauvegardes :
- `profiles` : stocke les informations d’un joueur (nom d’utilisateur unique, mot de passe hashé, date de création).  
- `saves` : stocke les sauvegardes de parties, potentiellement plusieurs par profil.

#### b. Deux façons d’organiser les données

| Approche         | Description                                                                                   | Avantages                                  | Inconvénients                                 |
|------------------|-----------------------------------------------------------------------------------------------|---------------------------------------------|-----------------------------------------------|
| Embedding        | Mettre les données de sauvegarde **directement dans** le document `profile`.                  | Lecture simple si 1 seule sauvegarde       | Document unique peut devenir gros, pas pratique si plusieurs saves |
| Références       | Avoir une collection `saves` séparée avec un champ `ProfileId` qui référence le profil.       | Flexible, plusieurs sauvegardes possibles | Nécessite plusieurs requêtes ou des agrégations |

> Pour ce TP, on choisit **l’approche “références”**, car elle est plus générale et prépare aux cas multi-sauvegardes.

#### c. Évolution du schéma
- En NoSQL, il n’y a pas de migration forcée → ajouter un champ est **facile**.  
- Pour gérer des évolutions propres, on ajoute souvent un champ `SchemaVersion` dans les documents.

---

### 3. Installation et connexion à MongoDB

#### a. Installation locale
1. Installer **MongoDB Community Edition** (service local).
2. Installer **MongoDB Compass** (GUI graphique pratique).
3. Vérifier que le service MongoDB est lancé :
```

localhost:27017

````
ou via `services.msc` sous Windows.

#### b. Ajouter le driver .NET
```bash
dotnet add package MongoDB.Driver
````

Cela ajoute la bibliothèque officielle pour interagir avec MongoDB dans ton projet C#.

#### c. Connexion

```csharp
var client = new MongoClient("mongodb://localhost:27017");
var database = client.GetDatabase("game");
```

* `MongoClient` est réutilisable → on le crée une seule fois pour toute l’application.
* `GetDatabase("game")` sélectionne la base (créée automatiquement au premier insert).

---

### 4. Modèles C# et annotations BSON 

On crée des classes C# (POCOs) qui correspondent à la structure des documents dans MongoDB.

* `[BsonId]` indique l’identifiant unique.
* `[BsonRepresentation(BsonType.ObjectId)]` permet de manipuler les `ObjectId` MongoDB comme des strings côté C#.
* Les noms de propriétés deviennent les noms de champs BSON par défaut.

Exemple :

```csharp
public class ProfileDoc
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;
    public string Username { get; set; } = default!;
    public string PasswordHashB64 { get; set; } = default!;
    public string SaltB64 { get; set; } = default!;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
```

---

### 5. Contexte MongoDB et création d’index

#### a. Centralisation du contexte

On crée une classe `MongoContext` qui :

* instancie une seule fois le `MongoClient`,
* configure les collections,
* crée les index nécessaires au démarrage.

#### b. Index unique

Sur `profiles`, on crée un index unique sur `Username` pour empêcher deux comptes avec le même nom :

```csharp
var keys = Builders<ProfileDoc>.IndexKeys.Ascending(p => p.Username);
var options = new CreateIndexOptions { Unique = true, Name = "ux_username" };
Profiles.Indexes.CreateOne(new CreateIndexModel<ProfileDoc>(keys, options));
```

#### c. Index sur scores

Sur `saves`, un index combiné `Score` DESC + `LastSaveUtc` DESC permet des classements rapides :

```csharp
var scoreKeys = Builders<SaveGameDoc>.IndexKeys
    .Descending(s => s.Score)
    .Descending(s => s.LastSaveUtc);
Saves.Indexes.CreateOne(new CreateIndexModel<SaveGameDoc>(scoreKeys));
```

---

### 6. CRUD de base avec `MongoDB.Driver`

#### CREATE

```csharp
await ctx.Profiles.InsertOneAsync(new ProfileDoc { Username = "alice", ... });
```

#### READ

```csharp
var alice = await ctx.Profiles.Find(p => p.Username == "alice").FirstOrDefaultAsync();
```

#### UPDATE

```csharp
var upd = Builders<SaveGameDoc>.Update
    .Set(s => s.Score, 4200)
    .Set(s => s.LastSaveUtc, DateTime.UtcNow);
await ctx.Saves.UpdateOneAsync(s => s.Id == saveId, upd);
```

#### DELETE

```csharp
await ctx.Saves.DeleteOneAsync(s => s.Id == saveId);
```

---

### 7. Requêtes utiles (Leaderboard)

Afficher les 10 meilleurs scores :

```csharp
var top10 = await ctx.Saves
    .Find(FilterDefinition<SaveGameDoc>.Empty)
    .SortByDescending(s => s.Score).ThenByDescending(s => s.LastSaveUtc)
    .Limit(10)
    .ToListAsync();
```

---

## Partie 2 – TP : Provider MongoDB

### Objectif

Implémenter une couche de persistance MongoDB qui remplace (ou complète) la sauvegarde locale.
Le jeu doit pouvoir :

* créer des profils dans MongoDB,
* vérifier les mots de passe hashés (PBKDF2, jour 2),
* sauvegarder et charger des `SaveGame` depuis MongoDB,
* afficher un **Top 5** des meilleurs scores.

---

### Étapes du TP

1. **Connexion** : mettre en place un `MongoContext` unique.
2. **Profils** :

   * Création avec insertion MongoDB.
   * Gestion des doublons via index unique.
   * Vérification via PBKDF2.
3. **Sauvegardes** :

   * Upsert : si une sauvegarde existe → remplacement ; sinon → insertion.
   * Mise à jour du score et de la date.
4. **Chargement** : récupération de la sauvegarde associée au profil connecté.
5. **Leaderboard** : requête top 5 triée Score desc, LastSaveUtc desc.
6. **Gestion d'erreurs** : aucun affichage brut de stacktrace.

---

### Critères de réussite

* Index unique fonctionnel sur `Username`.
* Création et vérification de profils opérationnelles.
* Sauvegarde MongoDB fonctionnelle (upsert).
* Lecture et affichage correct des sauvegardes.
* Leaderboard Top 5 fonctionnel.
* Gestion propre des erreurs.

---

### Tests manuels à réaliser

1. Création d’un profil avec un username déjà existant → message clair.
2. Première sauvegarde → insertion ; seconde → remplacement.
3. Chargement correct de la sauvegarde.
4. Affichage du Top 5 trié correctement.
5. Arrêt de MongoDB → l’application doit afficher une erreur compréhensible, pas planter.

---

## Résumé

* MongoDB est une base documentaire parfaitement adaptée aux données de jeu.
* Sa souplesse permet une évolution rapide sans migration lourde.
* Grâce à `MongoDB.Driver`, les opérations CRUD sont simples et expressives.
* Les index sont essentiels pour garantir l’unicité et la performance.
* Ce jour pose les bases pour une persistance **centralisée et multi-utilisateurs**.
