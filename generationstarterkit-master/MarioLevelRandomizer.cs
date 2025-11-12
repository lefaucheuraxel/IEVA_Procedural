// Importation des bibliothèques nécessaires
using System;                          // Fonctions de base (Console, Exception, etc.)
using System.Xml;                       // Manipulation de fichiers XML
using System.Collections.Generic;      // Listes, dictionnaires, etc.
using System.Xml.Linq;                  // Manipulation XML avec LINQ (plus moderne)
using System.Linq;                      // Requêtes LINQ (Where, OrderBy, etc.)

// Espace de noms pour organiser le code
namespace mariogenerator
{
    // Classe principale du programme
    class MarioLevelRandomizer
    {
        // Propriétés pour stocker les dimensions du niveau
        public int Width {get; private set;}    // Largeur du niveau (nombre de colonnes)
        public int Height {get; private set;}   // Hauteur du niveau (nombre de lignes)

        // Point d'entrée du programme (méthode appelée au démarrage)
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("  Mode aléatoire: MarioLevelRandomizer.exe <source.tmx> <target.tmx>");
                Console.WriteLine("  Mode Hill Climbing: MarioLevelRandomizer.exe <source.tmx> <target.tmx> hillclimbing [iterations]");
                Environment.Exit(1);
            }
            
            // Afficher les fichiers d'entrée et de sortie
            Console.WriteLine("fichier source: " + args[0]);  // args[0] = premier argument
            Console.WriteLine("fichier cible: " + args[1]);   // args[1] = deuxième argument
            
            // Créer une instance de la classe MarioLevelRandomizer
            var mlr = new MarioLevelRandomizer();
            
            // Bloc try-catch pour gérer les erreurs potentielles
            try
            {
                // Vérifier si le mode Hill Climbing est demandé (3ème argument = "hillclimbing")
                if (args.Length >= 3 && args[2].ToLower() == "hillclimbing")
                {
                    int maxIterations = 1000; // Valeur par défaut
                    if (args.Length >= 4)
                    {
                        if (!int.TryParse(args[3], out maxIterations))
                        {
                            Console.WriteLine("Nombre d'itérations invalide, utilisation de la valeur par défaut: 1000");
                            maxIterations = 1000;
                        }
                    }
                    mlr.HillClimbingGeneration(args[0], args[1], maxIterations);
                }
                else
                {
                    // Si pas de 3ème argument ou différent de "hillclimbing"
                    // Utiliser le mode aléatoire par défaut
                    mlr.ShuffleLayers(args[0], args[1]);
                }
            }
            catch(Exception e)  // Attraper toute exception qui pourrait survenir
            {
               // Afficher le message d'erreur complet
               Console.WriteLine(e.ToString());
               // Quitter avec code d'erreur
               Environment.Exit(1);
           }
        }
        // Méthode pour mélanger aléatoirement les colonnes du niveau
        private void ShuffleLayers(String sourceFile, String targetFile)
        {
            // Charger le fichier TMX (format XML)
            var xDocument = XDocument.Load(sourceFile);
            
            // Récupérer l'élément racine "map" du document XML
            var xMap = xDocument.Element("map");
            
            // Lire les dimensions du niveau depuis les attributs XML
            Width = (int) xMap.Attribute("width");    // Convertir l'attribut en entier
            Height = (int) xMap.Attribute("height");
            
            // Parcourir tous les éléments "layer" (couches) du niveau
            foreach (var e in xMap.Elements().Where(x => x.Name == "layer"))
            {
                // Créer un objet TileLayer pour cette couche
                var tl = new TileLayer(e, Width, Height);
                
                // Mélanger les colonnes de manière aléatoire
                tl.ShuffleColumns();
            }
            
            // Sauvegarder le document XML modifié dans le fichier cible
            xDocument.Save(targetFile);
        }
        
        // Méthode pour générer un niveau optimisé avec l'algorithme Hill Climbing
        private void HillClimbingGeneration(String sourceFile, String targetFile, int maxIterations)
        {
            // Charger le fichier TMX source
            var xDocument = XDocument.Load(sourceFile);
            
            // Récupérer l'élément racine "map"
            var xMap = xDocument.Element("map");
            
            // Lire les dimensions du niveau
            Width = (int) xMap.Attribute("width");
            Height = (int) xMap.Attribute("height");
            
            // Afficher les informations de génération
            Console.WriteLine($"Génération de niveau avec Hill Climbing (max {maxIterations} itérations)");
            Console.WriteLine($"Dimensions: {Width}x{Height}");
            
            // Parcourir toutes les couches (layers) du niveau
            foreach (var e in xMap.Elements().Where(x => x.Name == "layer"))
            {
                // Créer un objet TileLayer pour cette couche
                var tl = new TileLayer(e, Width, Height);
                
                // Étape 1: Créer une configuration initiale désordonnée
                // (pour avoir un point de départ avec un fitness bas)
                Console.WriteLine("Création d'une configuration initiale aléatoire...");
                tl.CreateBadConfiguration();
                
                // Étape 2: Appliquer l'algorithme Hill Climbing pour optimiser
                Console.WriteLine("\nDébut de l'optimisation par Hill Climbing...\n");
                tl.HillClimbing(maxIterations);
            }
            
            // Sauvegarder le niveau optimisé dans le fichier cible
            xDocument.Save(targetFile);
            Console.WriteLine($"Niveau généré et sauvegardé dans: {targetFile}");
        }
    }
}
