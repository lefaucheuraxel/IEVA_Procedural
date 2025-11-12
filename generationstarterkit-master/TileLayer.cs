// Importation des bibliothèques nécessaires
using System;                          // Fonctions de base
using System.Xml;                       // Manipulation XML
using System.Collections.Generic;      // Listes, collections
using System.Xml.Linq;                  // LINQ pour XML
using System.Linq;                      // Requêtes LINQ

namespace mariogenerator
{
    // Classe représentant une colonne de tiles (une colonne verticale du niveau)
    public class TileColumn{
        public int Id {get;private set;}                    // Identifiant de la colonne (position x)
        public readonly List<int> Column = new List<int>(); // Liste des tiles (de haut en bas)
        public int Height{get;private set;}                 // Hauteur maximale de la colonne

        // Constructeur: initialise une colonne avec son ID et sa hauteur
        public TileColumn(int id, int height)
        {
            Id = id;        // Position x de la colonne
            Height = height; // Nombre de lignes dans la colonne
        }
        
        // Méthode pour convertir la colonne en chaîne de caractères (pour affichage)
        public override string ToString()
        {
            return String.Join(",", Column);  // Joindre tous les tiles avec des virgules
        }
        
        // Calcule la hauteur réelle de la colonne (nombre de tiles non-vides depuis le bas)
        public int GetColumnHeight()
        {
            int height = 0;  // Initialiser la hauteur à 0
            
            // Parcourir la colonne depuis le bas (dernière ligne) vers le haut
            for (int i = Column.Count - 1; i >= 0; i--)
            {
                // Si on trouve une tile non-vide (0 = vide, autre = pleine)
                if (Column[i] != 0)
                {
                    // La hauteur = nombre de lignes depuis le haut jusqu'à cette tile
                    height = Column.Count - i;
                    break;  // Arrêter la recherche
                }
            }
            return height;  // Retourner la hauteur calculée
        }
    }
    // Classe représentant une couche (layer) complète du niveau
    public class TileLayer
    {
        public int Id {get; private set;}                       // Identifiant de la couche
        public int Width {get; private set;}                    // Largeur (nombre de colonnes)
        public int Height {get; private set;}                   // Hauteur (nombre de lignes)
        private XElement Layer;                                  // Référence à l'élément XML

        public  List<TileColumn> TileMap = new List<TileColumn>(); // Liste de toutes les colonnes
 
         // Constructeur: charge une couche depuis un élément XML
         public TileLayer(XElement layer, int width, int height)
         {
            Layer = layer;    // Sauvegarder la référence XML
            Width = width;    // Largeur du niveau
            Height = height;  // Hauteur du niveau
            
            // Récupérer l'élément "data" qui contient les tiles
            var data = Layer.Element("data");
            
            // Vérifier le format d'encodage des données
            var encoding = (string)data.Attribute("encoding");
            
            if (encoding == "csv")  // Si format CSV (valeurs séparées par virgules)
                LoadCSVLayerData((string)data.Value);  // Charger les données
            else
                // Sinon, lever une exception (seul CSV est supporté)
                throw new Exception("encoding not supported: use csv when saving Tiled file");
         }

         // Charge les données de la couche depuis le format CSV
         private void LoadCSVLayerData(String csvData)
         {
                // Le format TMX/CSV stocke le niveau comme une liste de
                // Width × Height entiers séparés par des virgules
                // Format: ligne par ligne, de gauche à droite
                
                var tiles = csvData.Split(',');  // Séparer par virgules
                
                // Créer une colonne vide pour chaque position x
                for (var x = 0 ; x <Width; ++x)
                {
                    var tc = new TileColumn(x, Height);  // Nouvelle colonne
                    TileMap.Add(tc);                      // Ajouter à la liste
                }
                
                // Remplir les colonnes avec les données
                // Parcourir ligne par ligne (y), puis colonne par colonne (x)
                for (int y = 0; y < Height; ++y)
                    for(int x = 0 ; x < Width; ++x)
                        // Calculer l'index dans le tableau 1D: y*Width + x
                        // Convertir la chaîne en entier et l'ajouter à la colonne
                        TileMap[x].Column.Add(int.Parse(tiles[y*Width + x].Trim()));
                
                Console.WriteLine($"{tiles.Length} tiles loaded");
         }

         // Convertir la couche en chaîne (pour affichage/debug)
         public override string ToString()
         {
            return String.Join("\n",TileMap);  // Une colonne par ligne
         }

         // Mélange aléatoirement l'ordre des colonnes
         public void ShuffleColumns(){
            // OrderBy avec Guid.NewGuid() crée un ordre aléatoire
            // (chaque colonne reçoit un GUID aléatoire pour le tri)
            TileMap = TileMap.OrderBy( x=> Guid.NewGuid()).ToList();
            
            // Écrire les modifications dans le XML
            WriteCSVLayerData();
         }
         
         // Affiche les statistiques sur les hauteurs de colonnes (pour analyse)
         public void PrintHeightStatistics()
         {
             // Calculer la hauteur de chaque colonne
             var heights = TileMap.Select(col => col.GetColumnHeight()).ToList();
             
             // Grouper les colonnes par hauteur et trier
             var heightGroups = heights.GroupBy(h => h).OrderBy(g => g.Key);
             
             Console.WriteLine("Distribution des hauteurs de colonnes:");
             // Afficher chaque groupe (hauteur → nombre de colonnes)
             foreach (var group in heightGroups)
             {
                 Console.WriteLine($"  Hauteur {group.Key}: {group.Count()} colonnes");
             }
             
             // Afficher les statistiques globales
             Console.WriteLine($"  Min: {heights.Min()}, Max: {heights.Max()}, Moyenne: {heights.Average():F1}");
         }
         
         // Crée une configuration initiale désordonnée (pour tester Hill Climbing)
         public void CreateBadConfiguration(Random random = null)
         {
             // Si pas de générateur aléatoire fourni, en créer un
             if (random == null)
                 random = new Random();
             
             // Mélanger complètement les colonnes de manière aléatoire
             // OrderBy avec random.Next() crée un ordre aléatoire
             TileMap = TileMap.OrderBy(x => random.Next()).ToList();
             
             // Faire des échanges supplémentaires entre première et deuxième moitié
             // pour créer encore plus de discontinuités de hauteur
             for (int i = 0; i < Width / 4; i++)  // Width/4 échanges
             {
                 int idx1 = random.Next(0, Width / 2);      // Index dans première moitié
                 int idx2 = random.Next(Width / 2, Width);  // Index dans deuxième moitié
                 
                 // Échanger les deux colonnes
                 var temp = TileMap[idx1];
                 TileMap[idx1] = TileMap[idx2];
                 TileMap[idx2] = temp;
             }
             
             // Afficher les statistiques de cette configuration
             PrintHeightStatistics();
             Console.WriteLine($"Fitness après création de la mauvaise configuration: {CalculateFitness()}/{Width - 1}");
             
             // Sauvegarder dans le XML
             WriteCSVLayerData();
         }
         
         // Fonction de fitness: évalue la qualité du niveau
         // Compte le nombre de paires de colonnes consécutives qui ont
         // au maximum 2 tiles de différence de hauteur
         // Plus le score est élevé, plus le niveau est "jouable" (transitions douces)
         public int CalculateFitness()
         {
             int score = 0;  // Initialiser le score à 0
             
             // Parcourir toutes les paires de colonnes adjacentes
             for (int i = 0; i < TileMap.Count - 1; i++)
             {
                 // Calculer la hauteur de la colonne actuelle
                 int height1 = TileMap[i].GetColumnHeight();
                 
                 // Calculer la hauteur de la colonne suivante
                 int height2 = TileMap[i + 1].GetColumnHeight();
                 
                 // Calculer la différence absolue entre les deux hauteurs
                 int difference = Math.Abs(height1 - height2);
                 
                 // Si la différence est acceptable (≤ 2 tiles)
                 if (difference <= 2)
                 {
                     score++;  // Incrémenter le score
                 }
             }
             
             // Retourner le score total
             // Score maximum possible = Width - 1 (toutes les paires sont bonnes)
             return score;
         }
         
         // Algorithme Hill Climbing: optimise le niveau par recherche locale
         // Principe: à chaque itération, essayer un voisin (2 colonnes échangées)
         //           et le garder seulement s'il améliore le fitness
         public void HillClimbing(int maxIterations, Random random = null)
         {
             // Si pas de générateur aléatoire fourni, en créer un
             if (random == null)
                 random = new Random();
             
             // Calculer le fitness de la configuration initiale
             int currentFitness = CalculateFitness();
             Console.WriteLine($"Fitness initial: {currentFitness}/{Width - 1} ({100.0 * currentFitness / (Width - 1):F1}%)");
             
             // Compteurs pour les statistiques
             int improvements = 0;          // Nombre d'améliorations trouvées
             int lastReportIteration = 0;   // Dernière itération avec rapport
             
             // Boucle principale: itérer jusqu'au maximum d'itérations
             for (int iteration = 0; iteration < maxIterations; iteration++)
             {
                 // ÉTAPE 1: Générer un voisin
                 // Choisir aléatoirement une position entre 0 et Width-2
                 // (car on échange avec la position suivante)
                 int swapIndex = random.Next(0, Width - 1);
                 
                 // Échanger deux colonnes adjacentes (swapIndex et swapIndex+1)
                 var temp = TileMap[swapIndex];              // Sauvegarder temporairement
                 TileMap[swapIndex] = TileMap[swapIndex + 1]; // Déplacer la suivante
                 TileMap[swapIndex + 1] = temp;               // Mettre l'ancienne à la place suivante
                 
                 // ÉTAPE 2: Évaluer le voisin
                 // Calculer le fitness de cette nouvelle configuration
                 int neighborFitness = CalculateFitness();
                 
                 // ÉTAPE 3: Décider si on garde le voisin
                 // Si le voisin est MEILLEUR (fitness plus élevé)
                 if (neighborFitness > currentFitness)
                 {
                     // ACCEPTER: garder cette nouvelle configuration
                     currentFitness = neighborFitness;  // Mettre à jour le fitness actuel
                     improvements++;                     // Incrémenter le compteur d'améliorations
                     
                     // Afficher un message de succès
                     Console.WriteLine($"Itération {iteration + 1}: Amélioration trouvée! Fitness: {currentFitness}/{Width - 1} ({100.0 * currentFitness / (Width - 1):F1}%)");
                     lastReportIteration = iteration;   // Mémoriser cette itération
                 }
                 else
                 {
                     // REJETER: le voisin n'est pas meilleur
                     // Annuler l'échange pour revenir à l'état précédent
                     temp = TileMap[swapIndex];              // Ré-échanger dans l'autre sens
                     TileMap[swapIndex] = TileMap[swapIndex + 1];
                     TileMap[swapIndex + 1] = temp;
                     // Maintenant on est revenu à la configuration précédente
                 }
                 
                 // Afficher un rapport de progression tous les 100 itérations
                 // (seulement si pas d'amélioration à cette itération)
                 if ((iteration + 1) % 100 == 0 && iteration != lastReportIteration)
                 {
                     Console.WriteLine($"Itération {iteration + 1}: Pas d'amélioration (fitness actuel: {currentFitness}/{Width - 1})");
                 }
                 
                 // Condition d'arrêt anticipé: si on atteint le fitness optimal
                 // (toutes les paires de colonnes adjacentes respectent la contrainte)
                 if (currentFitness == Width - 1)
                 {
                     Console.WriteLine($"Fitness optimal atteint à l'itération {iteration + 1}!");
                     break;  // Sortir de la boucle, pas besoin de continuer
                 }
             }
             
             // Afficher le résumé final
             Console.WriteLine($"\nRésumé:");
             Console.WriteLine($"  Nombre d'améliorations: {improvements}");
             Console.WriteLine($"  Fitness final: {currentFitness}/{Width - 1} ({100.0 * currentFitness / (Width - 1):F1}%)");
             
             // Sauvegarder la configuration finale dans le XML
             WriteCSVLayerData();
         }
         
         // Écrit les données de la couche dans le format CSV du fichier XML
         private void WriteCSVLayerData(){
            String s = "";  // Chaîne qui contiendra toutes les données CSV
            
            // Parcourir ligne par ligne (y), puis colonne par colonne (x)
            // (même ordre que lors du chargement)
            for(int y = 0 ; y < Height ; ++y)
            {
                for(int x = 0 ; x < Width ; ++x)
                {
                    // Ajouter la valeur de la tile à la position (x, y)
                    s+=TileMap[x].Column[y].ToString();
                    
                    // Ajouter une virgule après chaque tile sauf la dernière
                    if (y!=Height-1 || x!=Width-1)
                        s+=",";
                }
                // Ajouter un saut de ligne après chaque ligne sauf la dernière
                if (y!=Height-1)
                    s+="\n";  
            }
            
            // Récupérer l'élément "data" du XML
            var data = Layer.Element("data");
            
            // Remplacer son contenu par la nouvelle chaîne CSV
            data.Value = s;
         }
    }
}