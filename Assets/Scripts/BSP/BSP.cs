using System.Collections.Generic;
using UnityEngine;

public class BSPDungeonGenerator : MonoBehaviour
{
    // A class to represent a single room
    [System.Serializable]
    public class Room
    {
        public RectInt Bounds; // Rectangle bounds of the room

        // Returns the center of the room
        public Vector2Int Center => new Vector2Int(
            Bounds.x + Bounds.width / 2,
            Bounds.y + Bounds.height / 2
        );
    }

    [Header("Dungeon Settings")]
    public int dungeonWidth = 64;
    public int dungeonHeight = 64;
    public int minRoomSize = 8;
    public int maxIterations = 5;

    [Header("Tile Prefabs")]
    public GameObject floorTilePrefab;  // Assign a floor sprite prefab
    public GameObject wallTilePrefab;   // Assign a wall sprite prefab

    // Keeps track of placed rooms
    private List<Room> rooms = new List<Room>();

    // Stores all floor tile positions for corridor/wall generation
    private HashSet<Vector2Int> floorPositions = new HashSet<Vector2Int>();

    // Entry point
    void Start()
    {
        GenerateDungeon();
    }

    // Main dungeon generation logic
    void GenerateDungeon()
    {
        rooms.Clear();
        floorPositions.Clear();

        // Start with one big rectangle for the whole dungeon space
        RectInt rootSpace = new RectInt(0, 0, dungeonWidth, dungeonHeight);

        // Recursively split it
        List<RectInt> leafSpaces = SplitSpace(rootSpace, maxIterations);

        // Create a room inside each leaf space
        foreach (var space in leafSpaces)
        {
            Room room = CreateRoom(space);
            rooms.Add(room);
            DrawRoom(room.Bounds); // Instantiate floor tiles
        }

        // Connect rooms with corridors
        CreateCorridors();

        // Place walls around the dungeon
        PlaceWalls();
    }

    // Recursively splits a space into smaller leaf spaces
    List<RectInt> SplitSpace(RectInt space, int iterations)
    {
        Queue<RectInt> queue = new Queue<RectInt>();
        List<RectInt> result = new List<RectInt>();
        queue.Enqueue(space);

        for (int i = 0; i < iterations; i++)
        {
            if (queue.Count == 0) break;

            RectInt current = queue.Dequeue();
            bool splitHorizontally = Random.value > 0.5f;

            // Bias splits to balance aspect ratios
            if (current.width > current.height && current.width / current.height >= 1.25f)
                splitHorizontally = false;
            else if (current.height > current.width && current.height / current.width >= 1.25f)
                splitHorizontally = true;

            // Perform split
            if (splitHorizontally)
            {
                // Avoid splitting too small
                if (current.height < minRoomSize * 2) continue;

                int splitY = Random.Range(minRoomSize, current.height - minRoomSize);
                RectInt top = new RectInt(current.x, current.y + splitY, current.width, current.height - splitY);
                RectInt bottom = new RectInt(current.x, current.y, current.width, splitY);
                queue.Enqueue(top);
                queue.Enqueue(bottom);
            }
            else
            {
                if (current.width < minRoomSize * 2) continue;

                int splitX = Random.Range(minRoomSize, current.width - minRoomSize);
                RectInt left = new RectInt(current.x, current.y, splitX, current.height);
                RectInt right = new RectInt(current.x + splitX, current.y, current.width - splitX, current.height);
                queue.Enqueue(left);
                queue.Enqueue(right);
            }
        }

        // Any remaining unsplit nodes become final leaves
        while (queue.Count > 0)
            result.Add(queue.Dequeue());

        return result;
    }

    // Creates a room inside the given space with random size
    Room CreateRoom(RectInt space)
    {
        int roomWidth = Random.Range(minRoomSize, space.width);
        int roomHeight = Random.Range(minRoomSize, space.height);
        int roomX = Random.Range(space.x, space.xMax - roomWidth);
        int roomY = Random.Range(space.y, space.yMax - roomHeight);

        return new Room { Bounds = new RectInt(roomX, roomY, roomWidth, roomHeight) };
    }

    // Instantiates floor tiles for a room
    void DrawRoom(RectInt rect)
    {
        for (int x = rect.x; x < rect.xMax; x++)
        {
            for (int y = rect.y; y < rect.yMax; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (floorPositions.Add(pos)) // Avoid duplicates
                {
                    Instantiate(floorTilePrefab, new Vector3(x, y, 0), Quaternion.identity, transform);
                }
            }
        }
    }

    // Connects each room to the next with L-shaped corridors
    void CreateCorridors()
    {
        for (int i = 0; i < rooms.Count - 1; i++)
        {
            Vector2Int a = rooms[i].Center;
            Vector2Int b = rooms[i + 1].Center;

            // First move horizontally, then vertically
            foreach (Vector2Int pos in GetLine(a, new Vector2Int(b.x, a.y)))
            {
                if (floorPositions.Add(pos))
                    Instantiate(floorTilePrefab, new Vector3(pos.x, pos.y, 0), Quaternion.identity, transform);
            }

            foreach (Vector2Int pos in GetLine(new Vector2Int(b.x, a.y), b))
            {
                if (floorPositions.Add(pos))
                    Instantiate(floorTilePrefab, new Vector3(pos.x, pos.y, 0), Quaternion.identity, transform);
            }
        }
    }

    // Bresenham's line algorithm for corridor creation
    IEnumerable<Vector2Int> GetLine(Vector2Int from, Vector2Int to)
    {
        Vector2Int direction = to - from;
        int dx = Mathf.Abs(direction.x);
        int dy = Mathf.Abs(direction.y);
        int sx = direction.x > 0 ? 1 : -1;
        int sy = direction.y > 0 ? 1 : -1;

        int x = from.x;
        int y = from.y;

        if (dx > dy)
        {
            int err = dx / 2;
            for (int i = 0; i <= dx; i++)
            {
                yield return new Vector2Int(x, y);
                x += sx;
                err -= dy;
                if (err < 0)
                {
                    y += sy;
                    err += dx;
                }
            }
        }
        else
        {
            int err = dy / 2;
            for (int i = 0; i <= dy; i++)
            {
                yield return new Vector2Int(x, y);
                y += sy;
                err -= dx;
                if (err < 0)
                {
                    x += sx;
                    err += dy;
                }
            }
        }
    }

    // Places wall tiles around the edges of floor tiles
    void PlaceWalls()
    {
        HashSet<Vector2Int> wallPositions = new HashSet<Vector2Int>();

        foreach (var pos in floorPositions)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    Vector2Int neighbor = new Vector2Int(pos.x + dx, pos.y + dy);

                    // Place a wall only if it's not already a floor tile or wall
                    if (!floorPositions.Contains(neighbor) && wallPositions.Add(neighbor))
                    {
                        Instantiate(wallTilePrefab, new Vector3(neighbor.x, neighbor.y, 0), Quaternion.identity, transform);
                    }
                }
            }
        }
    }
}