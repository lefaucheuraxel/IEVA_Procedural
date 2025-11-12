using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using TMPro;

public class ProceduralLevelGenerator : MonoBehaviour
{
    [Header("Réglages du niveau")]
    public int width;
    public int maxHeight;
    public int maxIterations; // tu peux augmenter maintenant
    public int iterationsPerFrame; // nombre d’itérations par frame

    [Header("Références Unity")]
    public Tilemap tilemap;
    public TileBase groundTile;
    public TileBase topTile;

    [Header("UI")]
    public TextMeshProUGUI infoText; // référence vers le texte à l’écran

    private List<int> currentLevel;
    private int currentScore;
    private int iteration = 0;

    void Start()
    {

        // Génère le niveau de départ
        currentLevel = GenerateRandomLevel();
        currentScore = Fitness(currentLevel);

        // Affiche le niveau initial
        DrawLevel(currentLevel);
    }

    void Update()
    {
        if (iteration < maxIterations)
        {
            // On fait plusieurs itérations de Hill Climbing par frame
            for (int step = 0; step < iterationsPerFrame && iteration < maxIterations; step++)
            {
                List<int> neighbor = GenerateNeighbor(currentLevel);
                int neighborScore = Fitness(neighbor);

                // Probabilité d'accepter une moins bonne solution (pour éviter les blocages)
                float randomChance = 0.01f;

                if (neighborScore > currentScore || Random.value < randomChance)
                {
                    currentLevel = neighbor;
                    currentScore = neighborScore;
                }

                iteration++;
            }

            // On redessine le niveau une seule fois par frame (gain énorme)
            DrawLevel(currentLevel);
        }
        else
        {
            if (infoText != null)
                infoText.text = $"Terminé !\nScore final : {currentScore}";
            else
                Debug.Log("Hill Climbing terminé ! Score final : " + currentScore);
        }

        // Met à jour le texte à chaque frame
        if (infoText != null)
        {
            infoText.text = $"Itération : {iteration}/{maxIterations}\nScore : {currentScore}";
        }
    }

    List<int> GenerateRandomLevel()
    {
        List<int> level = new List<int>();
        for (int i = 0; i < width; i++)
            level.Add(Random.Range(1, maxHeight + 1));
        return level;
    }

    int Fitness(List<int> level)
    {
        int score = 0;
        for (int i = 0; i < level.Count - 1; i++)
        {
            int diff = Mathf.Abs(level[i] - level[i + 1]);
            if (diff <= 2) score++;
        }
        return score;
    }

    List<int> GenerateNeighbor(List<int> level)
    {
        List<int> neighbor = new List<int>(level);
        int index = Random.Range(0, level.Count);
        int newHeight = Mathf.Clamp(neighbor[index] + Random.Range(-1, 2), 1, maxHeight);
        neighbor[index] = newHeight;
        return neighbor;
    }

    void DrawLevel(List<int> level)
    {
        tilemap.ClearAllTiles();

        for (int x = 0; x < level.Count; x++)
        {
            int columnHeight = level[x];

            // Placer les tuiles de sol
            for (int y = 0; y < columnHeight - 1; y++)
            {
                tilemap.SetTile(new Vector3Int(x, y, 0), groundTile);
            }

            // Placer la tuile du dessus
            tilemap.SetTile(new Vector3Int(x, columnHeight - 1, 0), topTile);
        }
    }
}
