
# Persistance des données  
## Introduction aux API REST : théorie + pratique PHP & .NET  

---

## Objectifs pédagogiques

- Comprendre les concepts fondamentaux des **API REST** et du protocole HTTP.  
- Savoir **concevoir et exposer une API simple** avec PHP et Apache.  
- Comprendre le rôle des **endpoints**, **verbes HTTP** et **codes de statut**.  
- Manipuler **JSON** pour échanger des données entre client et serveur.  
- Écrire un **client .NET** qui consomme l’API via `HttpClient`.  
- Poser les bases d’une architecture **client / serveur / persistance**.

---

## 1. Qu’est-ce qu’une API ?

**API** = *Application Programming Interface*  
→ Un ensemble de **règles et d’interfaces** qui permettent à un programme d’interagir avec un autre programme.

### Exemple concret
- Une application mobile ou .NET → appelle une API → reçoit des données JSON.  
- L’API sert de **pont entre la logique de l’application** et **la base de données**.

```

[ Client .NET ]  ⇄  [ API REST ]  ⇄  [ Base de données ]

```

---

## 2. Le protocole HTTP : fondation des API REST

### 2.1 Requête HTTP
Une requête HTTP comporte :
- **Méthode** (verbe) : GET, POST, PUT, DELETE…  
- **URL** : identifie la ressource ciblée.  
- **En-têtes** : métadonnées (Content-Type, Authorization…).  
- **Corps (body)** : facultatif, contient des données JSON pour POST/PUT.

Exemple de requête :
```

POST /api/profiles HTTP/1.1
Host: localhost
Content-Type: application/json
Authorization: Bearer demo-key-123

{
"username": "alice"
}

```

---

### 2.2 Réponse HTTP
Une réponse comporte :
- **Code de statut** (200, 201, 404, 500…)  
- **En-têtes** (Content-Type…)  
- **Corps (body)** en JSON le plus souvent.

Exemple :
```

HTTP/1.1 201 Created
Content-Type: application/json

{
"id": "66f08e...b13",
"username": "alice",
"createdUtc": "2025-10-09T08:45:00Z"
}

````

---

## 3. REST : principes de base

**REST** = REpresentational State Transfer  
→ Style architectural utilisant HTTP comme moyen de communication.

### 3.1 Ressources
- Tout est une **ressource** identifiée par une URL.  
- Ex : `/api/profiles`, `/api/profiles/{id}`, `/api/saves?profileId=...`.

### 3.2 Verbes HTTP
| Verbe  | Signification             | Exemple                          |
|--------|----------------------------|-----------------------------------|
| GET    | Lire                      | GET /api/profiles                |
| POST   | Créer                     | POST /api/profiles               |
| PUT    | Remplacer ou Upsert       | PUT /api/saves                   |
| DELETE | Supprimer                 | DELETE /api/profiles/{id}       |

### 3.3 Codes de statut
| Code | Signification            | Usage |
|------|---------------------------|-------|
| 200  | OK                        | Lecture réussie |
| 201  | Created                   | Création réussie |
| 204  | No Content                | Suppression réussie |
| 400  | Bad Request               | Données invalides |
| 401  | Unauthorized              | Clé/API manquante ou invalide |
| 404  | Not Found                 | Ressource inexistante |
| 409  | Conflict                  | Conflit ou doublon |
| 500  | Internal Server Error     | Erreur côté serveur |

---

## 4. Conception d’une API simple

### 4.1 Ressources et endpoints
Nous allons gérer deux ressources :

1. **Profiles**
   - `GET /api/profiles` → liste
   - `POST /api/profiles` → créer
   - `GET /api/profiles/{id}` → lire un profil

2. **Saves**
   - `GET /api/saves?profileId=xyz`
   - `PUT /api/saves` → insérer ou mettre à jour une sauvegarde


### 4.2 Verbes HTTP : fonctionnement et exemple de code

Dans une API REST, chaque **verbe HTTP** correspond à une **intention précise** : lire, créer, modifier ou supprimer une ressource.

| Verbe  | Action                  | Idempotent | Corps requis | Utilisation                                 |
| ------ | ----------------------- | ---------- | ------------ | ------------------------------------------- |
| GET    | Lire une ressource      | ✅ Oui      | ❌ Non        | Lecture simple, liste ou détail             |
| POST   | Créer une ressource     | ❌ Non      | ✅ Oui        | Création d’un nouveau profil, etc.          |
| PUT    | Remplacer / Upsert      | ✅ Oui      | ✅ Oui        | Mise à jour complète ou création si absente |
| DELETE | Supprimer une ressource | ✅ Oui      | ❌ Souvent    | Suppression ciblée                          |

---

### **1. GET – Lire**

#### Requête

```http
GET /api/profiles HTTP/1.1
Host: localhost
Authorization: Bearer demo-key-123
```

#### PHP (index.php)

```php
if ($method === 'GET' && $parts[1] === 'profiles' && count($parts) === 2) {
    echo json_encode($data['profiles']);
    exit;
}
```

> Ici, on vérifie la méthode `GET`, la ressource `profiles`, et on renvoie la liste en JSON.

#### C# (HttpClient)

```csharp
var profiles = await http.GetFromJsonAsync<dynamic[]>("profiles");
Console.WriteLine($"Nombre de profils : {profiles.Length}");
```

---

### **2. POST – Créer**

#### Requête

```http
POST /api/profiles HTTP/1.1
Host: localhost
Content-Type: application/json
Authorization: Bearer demo-key-123

{
  "username": "alice"
}
```

#### PHP

```php
if ($method === 'POST' && $parts[1] === 'profiles') {
    $body = json_decode(file_get_contents('php://input'), true);
    if (!isset($body['username'])) {
        http_response_code(400);
        echo json_encode(['error'=>'BAD_REQUEST']);
        exit;
    }
    $profile = [
        'id' => bin2hex(random_bytes(12)),
        'username' => $body['username'],
        'createdUtc' => gmdate('c')
    ];
    $data['profiles'][] = $profile;
    writeData($DATA, $data);
    http_response_code(201);
    echo json_encode($profile);
    exit;
}
```

> On lit le **corps JSON**, on valide, on crée un objet, on le stocke, puis on renvoie 201 Created.

#### C# (HttpClient)

```csharp
var response = await http.PostAsJsonAsync("profiles", new { username = "bob" });
Console.WriteLine($"POST /profiles => {response.StatusCode}");
```

---

### **3. PUT – Mettre à jour ou créer (Upsert)**

#### Requête

```http
PUT /api/saves HTTP/1.1
Host: localhost
Content-Type: application/json
Authorization: Bearer demo-key-123

{
  "profileId": "66f0a4b1...",
  "level": 3,
  "score": 1500,
  "inventory": ["Sword", "Shield"]
}
```

#### PHP

```php
if ($method === 'PUT' && $parts[1] === 'saves') {
    $body = json_decode(file_get_contents('php://input'), true);
    $save = [
        'profileId' => $body['profileId'],
        'level' => $body['level'] ?? 1,
        'score' => $body['score'] ?? 0,
        'inventory' => $body['inventory'] ?? [],
        'lastSaveUtc' => gmdate('c')
    ];
    $found = false;
    foreach ($data['saves'] as $i=>$s) {
        if ($s['profileId'] === $save['profileId']) {
            $data['saves'][$i] = $save;
            $found = true;
            break;
        }
    }
    if (!$found) $data['saves'][] = $save;
    writeData($DATA, $data);
    echo json_encode($save);
    exit;
}
```

> Ici PUT agit comme un **upsert** : il met à jour si la sauvegarde existe, sinon il la crée.

#### C# (HttpClient)

```csharp
await http.PutAsJsonAsync("saves", new {
    profileId = "66f0a4b1...",
    level = 3,
    score = 1500,
    inventory = new[] { "Sword", "Shield" }
});
```

---

### **4. DELETE – Supprimer**

#### Requête

```http
DELETE /api/profiles/66f0a4b1... HTTP/1.1
Host: localhost
Authorization: Bearer demo-key-123
```

#### PHP

```php
if ($method === 'DELETE' && $parts[1] === 'profiles' && count($parts) === 3) {
    $id = $parts[2];
    $data['profiles'] = array_values(array_filter($data['profiles'], fn($p) => $p['id'] !== $id));
    writeData($DATA, $data);
    http_response_code(204); // No Content
    exit;
}
```

> On filtre les profils pour retirer celui avec l’ID donné, puis on renvoie 204 (succès sans contenu).

#### C# (HttpClient)

```csharp
var deleteResponse = await http.DeleteAsync($"profiles/{profileId}");
Console.WriteLine($"DELETE /profiles/{profileId} => {deleteResponse.StatusCode}");
```

---

## À retenir

* **GET** → lecture, ne modifie rien, pas de body.
* **POST** → création, renvoie 201 avec la ressource créée.
* **PUT** → mise à jour complète ou création si absente.
* **DELETE** → suppression, souvent 204 No Content.
* Toujours retourner des **codes de statut clairs** + un corps JSON structuré en cas d’erreur.

---

## 5. Mise en place d'une API en PHP

### 5.1 Pré-requis
- PHP + Apache (XAMPP/WAMP/MAMP ou installation manuelle)
- Document root → souvent `htdocs` (ex. `C:\xampp\htdocs`)
- Créer un dossier `api/` dans `htdocs`
- URL locale → http://localhost/api/

---

### 5.2 Fichier `htdocs/api/index.php`

```php
<?php
declare(strict_types=1);
header('Content-Type: application/json; charset=utf-8');

// CORS
header('Access-Control-Allow-Origin: *');
header('Access-Control-Allow-Methods: GET,POST,PUT,DELETE,OPTIONS');
header('Access-Control-Allow-Headers: Content-Type, Authorization, X-Api-Key');
if ($_SERVER['REQUEST_METHOD'] === 'OPTIONS') { http_response_code(204); exit; }

// Auth simple (clé fixe)
$apiKey = $_SERVER['HTTP_AUTHORIZATION'] ?? ($_SERVER['HTTP_X_API_KEY'] ?? '');
$apiKey = preg_replace('/^Bearer\s+/i', '', $apiKey);
if ($apiKey !== 'demo-key-123') {
  http_response_code(401);
  echo json_encode(['error'=>'UNAUTHORIZED']); exit;
}

// Données en fichier JSON
$DATA = __DIR__ . '/data.json';
if (!file_exists($DATA)) file_put_contents($DATA, json_encode(['profiles'=>[], 'saves'=>[]]));

function readData($p){return json_decode(file_get_contents($p),true);}
function writeData($p,$d){file_put_contents($p.'.tmp',json_encode($d,JSON_PRETTY_PRINT));rename($p.'.tmp',$p);}

$method=$_SERVER['REQUEST_METHOD'];
$uri=strtok($_SERVER['REQUEST_URI'],'?');
$parts=array_values(array_filter(explode('/',$uri)));
$data=readData($DATA);

// GET /api/profiles
if($method==='GET' && $parts[1]==='profiles' && count($parts)===2){
  echo json_encode($data['profiles']); exit;
}

// POST /api/profiles
if($method==='POST' && $parts[1]==='profiles'){
  $body=json_decode(file_get_contents('php://input'),true);
  if(!isset($body['username'])){http_response_code(400);exit;}
  foreach($data['profiles'] as $p) if($p['username']===$body['username']){http_response_code(409);exit;}
  $profile=['id'=>bin2hex(random_bytes(12)),'username'=>$body['username']];
  $data['profiles'][]=$profile;
  writeData($DATA,$data);
  http_response_code(201);
  echo json_encode($profile); exit;
}

// GET /api/saves?profileId=xyz
if($method==='GET' && $parts[1]==='saves' && isset($_GET['profileId'])){
  foreach($data['saves'] as $s) if($s['profileId']===$_GET['profileId']){echo json_encode($s);exit;}
  http_response_code(404);exit;
}

// PUT /api/saves
if($method==='PUT' && $parts[1]==='saves'){
  $b=json_decode(file_get_contents('php://input'),true);
  $save=[
    'profileId'=>$b['profileId'],
    'score'=>$b['score']??0,
    'level'=>$b['level']??1,
    'inventory'=>$b['inventory']??[],
    'lastSaveUtc'=>gmdate('c')
  ];
  $found=false;
  foreach($data['saves'] as $i=>$s){
    if($s['profileId']===$save['profileId']){$data['saves'][$i]=$save;$found=true;break;}
  }
  if(!$found)$data['saves'][]=$save;
  writeData($DATA,$data);
  echo json_encode($save); exit;
}

http_response_code(404);
echo json_encode(['error'=>'NOT_FOUND']);
````

---

## 6. Consommer l'API depuis .NET

Créer une appli console C# :

```bash
dotnet new console -n ApiClientDemo
cd ApiClientDemo
```

Installer le package :

```bash
dotnet add package System.Net.Http.Json
```

`Program.cs` :

```csharp
using System.Net.Http;
using System.Net.Http.Json;

var http = new HttpClient { BaseAddress = new Uri("http://localhost/api/") };
http.DefaultRequestHeaders.Authorization =
    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "demo-key-123");

// 1. Créer un profil
var resp = await http.PostAsJsonAsync("profiles", new { username = "bob" });
Console.WriteLine($"POST /profiles => {resp.StatusCode}");
var created = await resp.Content.ReadFromJsonAsync<dynamic>();
Console.WriteLine($"Profil créé ID={created.id}");

// 2. Upsert sauvegarde
string profileId = created.id;
await http.PutAsJsonAsync("saves", new { profileId, score = 1500, level = 3 });

// 3. Lecture de la sauvegarde
var save = await http.GetFromJsonAsync<dynamic>($"saves?profileId={profileId}");
Console.WriteLine($"Score = {save.score}");
```

---

## 7. TP : API + Client

### Tâches

* Mettre en place et tester l’API localement.
* Écrire un client .NET qui :

  * Crée un profil,
  * Fait un upsert de sauvegarde,
  * Lit la sauvegarde,
  * Gère les codes d’erreur.

### Bonus

* Ajouter suppression profil
* Ajouter mise à jour PUT profil
* Ajouter champ `email` et valider côté serveur
* Remplacer la persistence fichier par MongoDB

---

## Résumé du Jour 4

* Les **API REST** sont des points d’entrée accessibles via HTTP.
* **Verbes + URLs + statuts** définissent le contrat.
* **PHP + Apache** suffit à mettre en place une API fonctionnelle.
* **.NET + HttpClient** permet de consommer ces endpoints facilement.
* C’est une **architecture standard** pour séparer application et données.

