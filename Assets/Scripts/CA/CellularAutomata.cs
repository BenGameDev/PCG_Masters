using UnityEngine;

public class CellularAutomata : MonoBehaviour
{
    [Header("Map Size")]
    public int width = 64;  // Width of the map in tiles
    public int height = 64; // Height of the map in tiles

    [Header("Generation Settings")]
    [Range(0, 100)]
    public int fillPercent = 45; // Initial percentage chance a tile is wall (0–100)

    public int smoothingIterations = 5;  // Number of times to smooth the map
    public int wallThreshold = 4;        // Number of surrounding wall tiles to decide wall/floor

    [Header("Tile Prefabs")]
    public GameObject floorTilePrefab;   // Assign your floor tile prefab here
    public GameObject wallTilePrefab;    // Assign your wall tile prefab here

    // Internal representation of the map:
    // 0 = floor, 1 = wall
    private int[,] map;

    // Called automatically by Unity when the scene starts
    void Start()
    {
        GenerateMap(); // Start the generation process
    }

    // Entry point to generate the map
    void GenerateMap()
    {
        // Initialize the map array with given width and height
        map = new int[width, height];

        // Step 1: Fill the map randomly with walls and floors
        RandomFillMap();

        // Step 2: Smooth the map multiple times using the automata rules
        for (int i = 0; i < smoothingIterations; i++)
        {
            SmoothMap();
        }

        // Step 3: Instantiate visual tiles in the scene
        DrawTiles();
    }

    // Randomly fills the map array with walls and floors
    void RandomFillMap()
    {
        // Use a seedable random number generator (optional)
        System.Random rand = new System.Random();

        // Loop through every tile
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Always make the border tiles walls
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                {
                    map[x, y] = 1; // Wall
                }
                else
                {
                    // Fill randomly based on fillPercent
                    map[x, y] = rand.Next(0, 100) < fillPercent ? 1 : 0;
                }
            }
        }
    }

    // Applies one iteration of the cellular automata smoothing algorithm
    void SmoothMap()
    {
        // Make a copy of the current map to apply changes
        int[,] newMap = (int[,])map.Clone();

        // Loop through the map (excluding borders)
        for (int x = 1; x < width - 1; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                // Count how many neighboring tiles are walls
                int wallCount = GetWallNeighborCount(x, y);

                // Rule:
                // If surrounded by enough walls: become wall
                // If surrounded by fewer: become floor
                if (wallCount > wallThreshold)
                {
                    newMap[x, y] = 1; // Become wall
                }
                else if (wallCount < wallThreshold)
                {
                    newMap[x, y] = 0; // Become floor
                }
                // Else: keep current value (do nothing)
            }
        }

        // Update the main map with the smoothed one
        map = newMap;
    }

    // Counts how many of the 8 neighbors around (x, y) are walls
    int GetWallNeighborCount(int x, int y)
    {
        int count = 0;

        // Check all 8 neighbors (from -1 to +1 in x and y)
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                // Skip the current tile itself
                if (dx == 0 && dy == 0)
                    continue;

                int nx = x + dx;
                int ny = y + dy;

                // If neighbor is out of bounds, treat it as a wall
                if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                {
                    count++;
                }
                // If it's inside bounds and is a wall
                else if (map[nx, ny] == 1)
                {
                    count++;
                }
            }
        }

        return count;
    }

    // Instantiates wall and floor prefabs at the appropriate map positions
    void DrawTiles()
    {
        // Optional: create a parent GameObject to organize tiles in the hierarchy
        Transform tileParent = new GameObject("Tiles").transform;
        tileParent.SetParent(transform);

        // Loop through the entire map
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Pick correct prefab for tile type
                GameObject prefab = map[x, y] == 1 ? wallTilePrefab : floorTilePrefab;

                if (prefab != null)
                {
                    // Instantiate tile at (x, y) in world space
                    Instantiate(prefab, new Vector3(x, y, 0), Quaternion.identity, tileParent);
                }
            }
        }
    }
}
