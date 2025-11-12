# Explication détaillée du code

## Vue d'ensemble

Le programme génère des niveaux Mario en réorganisant les colonnes d'un niveau existant, soit de manière aléatoire, soit en utilisant l'algorithme Hill Climbing pour optimiser la jouabilité.

## Structure du programme

### 1. **MarioLevelRandomizer.cs** - Programme principal

#### Responsabilités:
- Point d'entrée du programme (`Main`)
- Gestion des arguments de ligne de commande
- Chargement et sauvegarde des fichiers TMX (format XML)
- Coordination entre les différents modes (aléatoire vs Hill Climbing)

#### Flux d'exécution:

```
Main()
  ↓
Vérifier les arguments
  ↓
Créer une instance de MarioLevelRandomizer
  ↓
Mode aléatoire?          Mode Hill Climbing?
  ↓                           ↓
ShuffleLayers()         HillClimbingGeneration()
  ↓                           ↓
Charger TMX                Charger TMX
  ↓                           ↓
Pour chaque layer:         Pour chaque layer:
  - Créer TileLayer          - Créer TileLayer
  - ShuffleColumns()         - CreateBadConfiguration()
                             - HillClimbing()
  ↓                           ↓
Sauvegarder TMX            Sauvegarder TMX
```

### 2. **TileLayer.cs** - Gestion des niveaux

#### Classes principales:

##### **TileColumn** - Représente une colonne verticale
- `Id`: Position x de la colonne
- `Column`: Liste des tiles (de haut en bas)
- `Height`: Hauteur maximale
- `GetColumnHeight()`: Calcule la hauteur réelle (tiles non-vides)

##### **TileLayer** - Représente une couche complète
- `TileMap`: Liste de toutes les colonnes
- `Width`, `Height`: Dimensions
- Méthodes principales:
  - `LoadCSVLayerData()`: Charge depuis XML
  - `ShuffleColumns()`: Mélange aléatoire
  - `CalculateFitness()`: Évalue la qualité
  - `HillClimbing()`: Optimise le niveau
  - `WriteCSVLayerData()`: Sauvegarde dans XML

## Concepts clés

### Format TMX/CSV

Le format TMX stocke les niveaux en XML. Les tiles sont dans un élément `<data>` au format CSV:

```xml
<map width="212" height="14">
  <layer name="Terrain">
    <data encoding="csv">
      0,0,1,1,0,0,1,...
      0,1,1,1,1,0,1,...
      ...
    </data>
  </layer>
</map>
```

**Organisation des données:**
- Format linéaire: ligne par ligne, de gauche à droite
- Index dans le tableau 1D: `y * Width + x`
- Valeur 0 = tile vide
- Autres valeurs = différents types de tiles

### Conversion CSV → Colonnes

Le programme réorganise les données par colonnes pour faciliter les échanges:

```
Format CSV (ligne par ligne):     Format colonnes:
0,0,1,1,0                         Col0  Col1  Col2  Col3  Col4
0,1,1,1,1                         [0]   [0]   [1]   [1]   [0]
1,1,1,1,1                         [0]   [1]   [1]   [1]   [1]
                                  [1]   [1]   [1]   [1]   [1]
```

**Avantage:** On peut facilement échanger des colonnes entières!

### Fonction de fitness

**Objectif:** Mesurer la "jouabilité" du niveau

**Critère:** Transitions douces entre colonnes adjacentes

**Calcul:**
```
Pour chaque paire de colonnes adjacentes (i, i+1):
  hauteur1 = hauteur de la colonne i
  hauteur2 = hauteur de la colonne i+1
  différence = |hauteur1 - hauteur2|
  
  Si différence ≤ 2:
    score++

Retourner score
```

**Interprétation:**
- Score maximum = `Width - 1` (toutes les transitions sont bonnes)
- Score élevé = niveau plus jouable
- Score faible = beaucoup de sauts difficiles

**Exemple:**

```
Colonnes:  [h=1] [h=2] [h=1] [h=5] [h=3]
           
Paires:    (1,2) (2,1) (1,5) (5,3)
Diff:       1     1     4     2
OK?        ✓     ✓     ✗     ✓

Score: 3/4 = 75%
```

### Algorithme Hill Climbing

**Principe:** Recherche locale greedy (gloutonne)

**Étapes:**

1. **Initialisation**
   ```
   configuration_actuelle = configuration_initiale
   fitness_actuel = CalculateFitness()
   ```

2. **Boucle principale** (jusqu'à max_iterations)
   ```
   a. Générer un voisin:
      - Choisir 2 colonnes adjacentes au hasard
      - Les échanger
   
   b. Évaluer le voisin:
      - Calculer son fitness
   
   c. Décider:
      Si fitness_voisin > fitness_actuel:
        ACCEPTER (garder le voisin)
        fitness_actuel = fitness_voisin
      Sinon:
        REJETER (annuler l'échange)
   ```

3. **Arrêt**
   - Après max_iterations, OU
   - Si fitness optimal atteint (= Width - 1)

**Caractéristiques:**
- ✅ Simple et rapide
- ✅ N'accepte que les améliorations
- ❌ Peut se bloquer dans un optimum local
- ❌ Dépend de la configuration initiale

### Génération de voisins

**Stratégie:** Échange de 2 colonnes adjacentes

**Pourquoi adjacentes?**
- Modification minimale (principe de recherche locale)
- Affecte seulement 2-3 paires dans le calcul de fitness
- Exploration progressive de l'espace de solutions

**Code:**
```csharp
// Choisir une position aléatoire
int swapIndex = random.Next(0, Width - 1);

// Échanger avec la suivante
var temp = TileMap[swapIndex];
TileMap[swapIndex] = TileMap[swapIndex + 1];
TileMap[swapIndex + 1] = temp;
```

**Voisinage:**
- Nombre de voisins possibles = `Width - 1`
- Chaque voisin diffère d'un seul échange

## Exemple d'exécution

### Mode aléatoire

```bash
dotnet run Samples/Mario_level1.tmx Samples/random.tmx
```

**Ce qui se passe:**
1. Charger `Mario_level1.tmx`
2. Pour chaque layer:
   - Charger les colonnes
   - Les mélanger aléatoirement (OrderBy avec Guid.NewGuid())
   - Sauvegarder
3. Écrire `random.tmx`

**Résultat:** Niveau complètement aléatoire (probablement injouable)

### Mode Hill Climbing

```bash
dotnet run Samples/Mario_level1.tmx Samples/optimized.tmx hillclimbing 1000
```

**Ce qui se passe:**
1. Charger `Mario_level1.tmx`
2. Pour chaque layer:
   - Charger les colonnes
   - Créer une configuration désordonnée
   - Afficher fitness initial
   - Boucle Hill Climbing (1000 itérations):
     * Essayer un voisin
     * Si meilleur: garder
     * Sinon: rejeter
   - Afficher fitness final
   - Sauvegarder
3. Écrire `optimized.tmx`

**Sortie console:**
```
Génération de niveau avec Hill Climbing (max 1000 itérations)
Dimensions: 212x14
2968 tiles loaded
Création d'une configuration initiale aléatoire...
Distribution des hauteurs de colonnes:
  Hauteur 0: 11 colonnes
  Hauteur 1: 197 colonnes
  Hauteur 10: 4 colonnes
  Min: 0, Max: 10, Moyenne: 1.1
Fitness après création de la mauvaise configuration: 203/211

Début de l'optimisation par Hill Climbing...

Fitness initial: 203/211 (96.2%)
Itération 100: Pas d'amélioration (fitness actuel: 203/211)
Itération 200: Pas d'amélioration (fitness actuel: 203/211)
...

Résumé:
  Nombre d'améliorations: 0
  Fitness final: 203/211 (96.2%)
Niveau généré et sauvegardé dans: Samples/optimized.tmx
```

## Points techniques importants

### 1. Calcul de hauteur de colonne

```csharp
public int GetColumnHeight()
{
    int height = 0;
    // Parcourir depuis le bas vers le haut
    for (int i = Column.Count - 1; i >= 0; i--)
    {
        if (Column[i] != 0)  // Première tile non-vide
        {
            height = Column.Count - i;
            break;
        }
    }
    return height;
}
```

**Exemple:**
```
Colonne (de haut en bas):
Index: 0  1  2  3  4  5  6  7  8  9
Tile:  0  0  0  0  0  0  1  1  1  1

Parcours depuis i=9 vers i=0:
i=9: Column[9]=1 ≠ 0 → height = 10-9 = 1? Non!
i=6: Column[6]=1 ≠ 0 → height = 10-6 = 4 ✓
```

### 2. Conversion index 2D → 1D

Pour accéder à la tile en position (x, y) dans le tableau 1D:

```
index = y * Width + x
```

**Exemple:** Width=5
```
Position (x,y):  (0,0) (1,0) (2,0) (3,0) (4,0)
                 (0,1) (1,1) (2,1) (3,1) (4,1)
                 (0,2) (1,2) (2,2) (3,2) (4,2)

Index 1D:         0     1     2     3     4
                  5     6     7     8     9
                 10    11    12    13    14

Position (2,1) → index = 1*5 + 2 = 7 ✓
```

### 3. Mélange aléatoire avec LINQ

```csharp
TileMap = TileMap.OrderBy(x => Guid.NewGuid()).ToList();
```

**Comment ça marche:**
1. Pour chaque colonne, générer un GUID aléatoire
2. Trier les colonnes selon ces GUIDs
3. Résultat: ordre complètement aléatoire

**Alternative équivalente:**
```csharp
// Algorithme de Fisher-Yates
for (int i = TileMap.Count - 1; i > 0; i--)
{
    int j = random.Next(0, i + 1);
    var temp = TileMap[i];
    TileMap[i] = TileMap[j];
    TileMap[j] = temp;
}
```

### 4. Annulation d'échange

Pour annuler un échange, on ré-échange dans l'autre sens:

```csharp
// Échange initial
var temp = TileMap[i];
TileMap[i] = TileMap[i+1];
TileMap[i+1] = temp;

// Annulation (même opération!)
temp = TileMap[i];
TileMap[i] = TileMap[i+1];
TileMap[i+1] = temp;
```

**Pourquoi ça marche?** L'échange est son propre inverse!

## Améliorations possibles

### 1. Fonction de fitness plus sophistiquée
```csharp
public int CalculateFitness()
{
    int score = 0;
    for (int i = 0; i < TileMap.Count - 1; i++)
    {
        int diff = Math.Abs(TileMap[i].GetColumnHeight() - 
                           TileMap[i+1].GetColumnHeight());
        
        // Pénaliser selon la différence
        if (diff == 0) score += 3;      // Parfait
        else if (diff == 1) score += 2; // Très bien
        else if (diff == 2) score += 1; // Acceptable
        // diff > 2: score += 0 (mauvais)
    }
    return score;
}
```

### 2. Simulated Annealing
```csharp
double temperature = 100.0;
double coolingRate = 0.995;

for (int iteration = 0; iteration < maxIterations; iteration++)
{
    // Générer voisin...
    
    if (neighborFitness > currentFitness ||
        random.NextDouble() < Math.Exp((neighborFitness - currentFitness) / temperature))
    {
        // Accepter
    }
    
    temperature *= coolingRate;
}
```

### 3. Multi-start Hill Climbing
```csharp
int bestFitness = 0;
List<TileColumn> bestConfiguration = null;

for (int run = 0; run < 10; run++)
{
    CreateBadConfiguration();
    HillClimbing(1000);
    
    if (CalculateFitness() > bestFitness)
    {
        bestFitness = CalculateFitness();
        bestConfiguration = new List<TileColumn>(TileMap);
    }
}

TileMap = bestConfiguration;
```

## Conclusion

Le code implémente un système complet de génération de niveaux Mario avec:
- ✅ Chargement/sauvegarde de fichiers TMX
- ✅ Réorganisation par colonnes
- ✅ Fonction de fitness basée sur les transitions
- ✅ Algorithme Hill Climbing pour optimisation
- ✅ Commentaires détaillés pour comprendre chaque ligne

C'est une excellente base pour expérimenter avec d'autres algorithmes d'optimisation!
