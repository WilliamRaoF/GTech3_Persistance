# Persistance de données  
## Jour 1 – Introduction & Sérialisation JSON  

---

## Objectifs pédagogiques

- Comprendre les notions de **persistance** et **sérialisation**.  
- Découvrir le format **JSON** et savoir l’utiliser en C#.  
- Apprendre à **lire et écrire des fichiers** localement dans une application console.  
- Préparer le terrain pour la gestion de **sauvegardes de parties**.

---

## 1. Introduction à la persistance

### Volatile vs Persistant
- **Volatile** → données perdues quand l’application s’arrête (ex : variables en RAM).
- **Persistant** → données conservées sur disque ou dans une base (ex : fichiers, BDD).

### Pourquoi stocker ?
- Sauvegarde de partie, préférences utilisateur, historique, logs.
- Accès partagé entre plusieurs sessions ou utilisateurs.
- Reprise de l’état d’un programme après redémarrage.

### Modes de persistance
| Mode | Exemples | Avantages | Limites |
|------|----------|-----------|---------|
| Fichiers plats | .txt, .csv, .json | Simples, portables | Risque d’erreurs, peu structurés |
| Bases SQL | MySQL, PostgreSQL | Structurées, puissantes | Complexité d’installation |
| Bases NoSQL | MongoDB | Flexibles, orientées documents | Moins de contraintes de schéma |
| Services | API REST | Centralisation, accès distant | Nécessite serveur |

---

## 2. La sérialisation

### Définition
> La **sérialisation** est la conversion d’un objet en une représentation qui peut être stockée ou transmise.  
> La **désérialisation** est l’opération inverse.

### Utilité
- Sauvegarde dans des fichiers.
- Échanges réseau (API REST).
- Export/import de données.

### Formats courants
- **JSON** → simple, lisible, standard du web.  
- XML → verbeux mais structuré.  
- Binaire → rapide mais illisible pour un humain.

---

## 3. JSON en C#

### JSON (JavaScript Object Notation)
- Texte structuré en paires clé-valeur.
- Très utilisé pour la configuration, la sauvegarde et les APIs.

**Exemple JSON d’un joueur :**
```json
{
  "Nom": "Alice",
  "Niveau": 3,
  "Score": 2500
}
````

---

### Sérialisation en C#

Bibliothèque standard : `System.Text.Json`
Disponible par défaut dans .NET 5+.

```csharp
using System.Text.Json;

public class Joueur
{
    public string Nom { get; set; }
    public int Niveau { get; set; }
    public int Score { get; set; }
}

var joueur = new Joueur { Nom = "Alice", Niveau = 3, Score = 2500 };

string json = JsonSerializer.Serialize(
    joueur,
    new JsonSerializerOptions { WriteIndented = true }
);

File.WriteAllText("joueur.json", json);
```

Crée un fichier `joueur.json` contenant les données sérialisées.

---

### Désérialisation en C#

```csharp
string contenu = File.ReadAllText("joueur.json");
Joueur j = JsonSerializer.Deserialize<Joueur>(contenu);

Console.WriteLine($"Nom : {j.Nom}, Niveau : {j.Niveau}, Score : {j.Score}");
```

Lit le fichier et reconstruit l’objet `Joueur`.

---

## 4. Manipulation de fichiers en C#

### Classes utiles

* `File.WriteAllText(path, content)`
* `File.ReadAllText(path)`
* `File.Exists(path)`
* `Path.Combine(...)`
* `Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)` (pour des chemins propres)

### Exemple pratique

```csharp
string dossier = "Saves";
Directory.CreateDirectory(dossier);

string chemin = Path.Combine(dossier, "save.json");

if (File.Exists(chemin))
{
    string contenu = File.ReadAllText(chemin);
    var joueur = JsonSerializer.Deserialize<Joueur>(contenu);
    Console.WriteLine($"Partie chargée pour {joueur.Nom}");
}
else
{
    var nouveau = new Joueur { Nom = "Bob", Niveau = 1, Score = 0 };
    string json = JsonSerializer.Serialize(nouveau, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(chemin, json);
    Console.WriteLine("Nouvelle partie sauvegardée.");
}
```

---

## 5. Bonnes pratiques

* **Vérifier l’existence** du fichier avant de le lire.
* **Gérer les erreurs** avec `try/catch` (fichier corrompu, vide, etc.).
* **Organiser** les fichiers dans des dossiers dédiés (ex: `Saves`).
* **Séparer les responsabilités** : classe modèle vs logique de persistance vs UI console.

---

## 6. À retenir

* La **persistance** est essentielle pour conserver l’état d’une application.
* Le **JSON** est un format clé, lisible et très utilisé.
* La **sérialisation** permet de transformer des objets en texte (et inversement).
* C# fournit des outils intégrés simples (`System.Text.Json`, `File.*`).

---

## Pour la pratique

**TP : Sauvegarde simple de multiple joueurs**

* Créer une classe Joueur.
* Sauvegarder dans un fichier JSON.
* Charger au démarrage si le fichier existe.
* Menu console pour créer/charger/afficher un joueur.

