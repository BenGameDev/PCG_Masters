using System.Collections.Generic;

using UnityEngine;

/// <summary>
/// Generates random rooms connected by corridors, ensuring
/// only one corridor per side of each room (incoming or outgoing).
/// Also generates walls around floors.
/// </summary>
public class RoomGenerationWithConstraints : MonoBehaviour
{
    [Header("Map Settings")]
    public int mapWidth = 100;
    public int mapHeight = 100;

    [Header("Room Settings")]
    public int roomCount = 10;
    public int minRoomWidth = 5;
    public int maxRoomWidth = 15;
    public int minRoomHeight = 5;
    public int maxRoomHeight = 15;

    [Header("Tile Prefabs")]
    public GameObject floorTilePrefab;
    public GameObject wallTilePrefab;

    // List to store all generated rooms
    private List<Rect> rooms = new List<Rect>();

    // Set of all floor positions to help with wall placement
    private HashSet<Vector2Int> floorPositions = new HashSet<Vector2Int>();

    // Enum representing room sides for corridor connections
    private enum Side { Top, Bottom, Left, Right }

    // Tracks which sides of each room already have corridors (incoming or outgoing)
    private Dictionary<Rect, HashSet<Side>> usedSides = new Dictionary<Rect, HashSet<Side>>();

    void Start()
    {
        GenerateDungeon();
    }

    /// <summary>
    /// Runs the full dungeon generation pipeline:
    /// room placement, corridor connections, and wall generation.
    /// </summary>
    void GenerateDungeon()
    {
        GenerateRooms();
        ConnectRoomsWithSideConstraints();
        GenerateWalls();
    }

    /// <summary>
    /// Generates non-overlapping rooms within map bounds.
    /// </summary>
    void GenerateRooms()
    {
        int attempts = 0;
        int maxAttempts = roomCount * 5;

        while (rooms.Count < roomCount && attempts < maxAttempts)
        {
            attempts++;

            // Random room size within given constraints
            int width = Random.Range(minRoomWidth, maxRoomWidth + 1);
            int height = Random.Range(minRoomHeight, maxRoomHeight + 1);

            // Random position ensuring room fits inside the map
            int x = Random.Range(1, mapWidth - width - 1);
            int y = Random.Range(1, mapHeight - height - 1);

            Rect newRoom = new Rect(x, y, width, height);

            // Check overlap with existing rooms
            bool overlaps = false;
            foreach (Rect room in rooms)
            {
                if (newRoom.Overlaps(room))
                {
                    overlaps = true;
                    break;
                }
            }

            // If no overlap, add the room and initialize side tracking
            if (!overlaps)
            {
                rooms.Add(newRoom);
                usedSides[newRoom] = new HashSet<Side>();
                DrawRoom(newRoom);
            }
        }

        Debug.Log($"Generated {rooms.Count} rooms after {attempts} attempts.");
    }

    /// <summary>
    /// Instantiates floor tiles for the given room and tracks floor positions.
    /// </summary>
    /// <param name="room">Room rectangle to draw.</param>
    void DrawRoom(Rect room)
    {
        for (int x = (int)room.xMin; x < (int)room.xMax; x++)
        {
            for (int y = (int)room.yMin; y < (int)room.yMax; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);

                // Track floor position
                floorPositions.Add(pos);

                // Instantiate floor tile prefab at position
                if (floorTilePrefab != null)
                    Instantiate(floorTilePrefab, new Vector3(x, y, 0), Quaternion.identity, transform);
            }
        }
    }

    /// <summary>
    /// Connects rooms with L-shaped corridors, ensuring no more than
    /// one corridor per side per room (both incoming and outgoing).
    /// </summary>
    void ConnectRoomsWithSideConstraints()
    {
        for (int i = 1; i < rooms.Count; i++)
        {
            Rect roomA = rooms[i - 1];
            Rect roomB = rooms[i];

            Vector2Int centerA = GetRoomCenter(roomA);
            Vector2Int centerB = GetRoomCenter(roomB);

            // Determine which side on roomA faces roomB
            Side sideA = GetDirection(centerA, centerB);

            // Opposite side on roomB where corridor will connect
            Side sideB = GetOppositeSide(sideA);

            // Skip connection if either room's side already used by a corridor
            if (usedSides[roomA].Contains(sideA) || usedSides[roomB].Contains(sideB))
                continue;

            // Mark these sides as used
            usedSides[roomA].Add(sideA);
            usedSides[roomB].Add(sideB);

            // Create corridor in L-shape (random horizontal-then-vertical or vice versa)
            if (Random.value < 0.5f)
            {
                CreateHorizontalCorridor(centerA.x, centerB.x, centerA.y);
                CreateVerticalCorridor(centerA.y, centerB.y, centerB.x);
            }
            else
            {
                CreateVerticalCorridor(centerA.y, centerB.y, centerA.x);
                CreateHorizontalCorridor(centerA.x, centerB.x, centerB.y);
            }
        }
    }

    /// <summary>
    /// Gets center point of a room as integer grid coordinates.
    /// </summary>
    Vector2Int GetRoomCenter(Rect room)
    {
        int centerX = (int)(room.x + room.width / 2);
        int centerY = (int)(room.y + room.height / 2);
        return new Vector2Int(centerX, centerY);
    }

    /// <summary>
    /// Determines which side of the "from" position the "to" position lies on.
    /// </summary>
    Side GetDirection(Vector2Int from, Vector2Int to)
    {
        Vector2Int diff = to - from;
        if (Mathf.Abs(diff.x) > Mathf.Abs(diff.y))
            return diff.x > 0 ? Side.Right : Side.Left;
        else
            return diff.y > 0 ? Side.Top : Side.Bottom;
    }

    /// <summary>
    /// Returns the opposite side of the given side.
    /// </summary>
    Side GetOppositeSide(Side side)
    {
        switch (side)
        {
            case Side.Top: return Side.Bottom;
            case Side.Bottom: return Side.Top;
            case Side.Left: return Side.Right;
            case Side.Right: return Side.Left;
            default: return Side.Top;
        }
    }

    /// <summary>
    /// Creates a horizontal corridor of floor tiles between two X coordinates at fixed Y.
    /// </summary>
    void CreateHorizontalCorridor(int xStart, int xEnd, int y)
    {
        for (int x = Mathf.Min(xStart, xEnd); x <= Mathf.Max(xStart, xEnd); x++)
        {
            Vector2Int pos = new Vector2Int(x, y);
            if (floorPositions.Add(pos))
                Instantiate(floorTilePrefab, new Vector3(x, y, 0), Quaternion.identity, transform);
        }
    }

    /// <summary>
    /// Creates a vertical corridor of floor tiles between two Y coordinates at fixed X.
    /// </summary>
    void CreateVerticalCorridor(int yStart, int yEnd, int x)
    {
        for (int y = Mathf.Min(yStart, yEnd); y <= Mathf.Max(yStart, yEnd); y++)
        {
            Vector2Int pos = new Vector2Int(x, y);
            if (floorPositions.Add(pos))
                Instantiate(floorTilePrefab, new Vector3(x, y, 0), Quaternion.identity, transform);
        }
    }

    /// <summary>
    /// Generates walls around all floor tiles by placing wall tiles
    /// adjacent to floors where no floor exists.
    /// </summary>
    void GenerateWalls()
    {
        Vector2Int[] directions = new Vector2Int[]
        {
            Vector2Int.up, Vector2Int.down,
            Vector2Int.left, Vector2Int.right,
            new Vector2Int(1,1), new Vector2Int(-1,1),
            new Vector2Int(1,-1), new Vector2Int(-1,-1)
        };

        HashSet<Vector2Int> wallPositions = new HashSet<Vector2Int>();

        foreach (Vector2Int floorPos in floorPositions)
        {
            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighbor = floorPos + dir;

                // If no floor and no wall here, place a wall tile
                if (!floorPositions.Contains(neighbor) && !wallPositions.Contains(neighbor))
                {
                    wallPositions.Add(neighbor);
                    if (wallTilePrefab != null)
                        Instantiate(wallTilePrefab, new Vector3(neighbor.x, neighbor.y, 0), Quaternion.identity, transform);
                }
            }
        }
    }
}