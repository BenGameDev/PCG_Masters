using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Procedurally generates rooms and corridors with one connection per side.
/// </summary>
public class RoomGeneratorWithSideConstraints : MonoBehaviour
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

    private class RoomData
    {
        public RectInt rect;
        public bool top, bottom, left, right;

        public RoomData(RectInt rect)
        {
            this.rect = rect;
            top = bottom = left = right = false;
        }
    }

    private List<RoomData> rooms = new List<RoomData>();
    private HashSet<Vector2Int> floorPositions = new HashSet<Vector2Int>();

    void Start()
    {
        Generate();
    }


    void Generate()
    {
        GenerateRooms();
        ConnectRooms();
        GenerateWalls();
    }

    void GenerateRooms()
    {
        int attempts = 0;

        while (rooms.Count < roomCount && attempts < roomCount * 10)
        {
            attempts++;

            int width = Random.Range(minRoomWidth, maxRoomWidth + 1);
            int height = Random.Range(minRoomHeight, maxRoomHeight + 1);
            int x = Random.Range(1, mapWidth - width - 1);
            int y = Random.Range(1, mapHeight - height - 1);

            RectInt newRect = new RectInt(x, y, width, height);
            bool overlaps = false;

            foreach (RoomData r in rooms)
            {
                if (r.rect.Overlaps(newRect))
                {
                    overlaps = true;
                    break;
                }
            }

            if (!overlaps)
            {
                RoomData newRoom = new RoomData(newRect);
                rooms.Add(newRoom);
                DrawRoom(newRect);
            }
        }
    }

    void DrawRoom(RectInt room)
    {
        for (int x = room.xMin; x < room.xMax; x++)
        {
            for (int y = room.yMin; y < room.yMax; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                floorPositions.Add(pos);
                if (floorTilePrefab != null)
                    Instantiate(floorTilePrefab, new Vector3(x, y, 0), Quaternion.identity, transform);
            }
        }
    }

    void ConnectRooms()
    {
        for (int i = 1; i < rooms.Count; i++)
        {
            RoomData roomA = rooms[i - 1];
            RoomData roomB = rooms[i];

            Vector2Int centerA = GetCenter(roomA.rect);
            Vector2Int centerB = GetCenter(roomB.rect);

            List<Vector2Int> corridor = new List<Vector2Int>();
            bool horizontalFirst = Random.value < 0.5f;

            if (horizontalFirst)
            {
                corridor.AddRange(GetHorizontalCorridor(centerA.x, centerB.x, centerA.y));
                corridor.AddRange(GetVerticalCorridor(centerA.y, centerB.y, centerB.x));
            }
            else
            {
                corridor.AddRange(GetVerticalCorridor(centerA.y, centerB.y, centerA.x));
                corridor.AddRange(GetHorizontalCorridor(centerA.x, centerB.x, centerB.y));
            }

            Vector2Int entryPoint = corridor[0];
            Vector2Int exitPoint = corridor[corridor.Count - 1];

            Vector2Int contactA = GetClosestBoundaryPoint(roomA.rect, entryPoint);
            Vector2Int contactB = GetClosestBoundaryPoint(roomB.rect, exitPoint);

            string sideA = GetSideFromContact(roomA.rect, contactA);
            string sideB = GetSideFromContact(roomB.rect, contactB);

            if (IsSideUsed(roomA, sideA) || IsSideUsed(roomB, sideB))
                continue;

            SetSideUsed(roomA, sideA, true);
            SetSideUsed(roomB, sideB, true);

            foreach (Vector2Int pos in corridor)
            {
                if (floorPositions.Add(pos) && floorTilePrefab != null)
                    Instantiate(floorTilePrefab, new Vector3(pos.x, pos.y, 0), Quaternion.identity, transform);
            }
        }
    }

    Vector2Int GetCenter(RectInt room)
    {
        return new Vector2Int(room.x + room.width / 2, room.y + room.height / 2);
    }

    List<Vector2Int> GetHorizontalCorridor(int xStart, int xEnd, int y)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        for (int x = Mathf.Min(xStart, xEnd); x <= Mathf.Max(xStart, xEnd); x++)
            result.Add(new Vector2Int(x, y));
        return result;
    }

    List<Vector2Int> GetVerticalCorridor(int yStart, int yEnd, int x)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        for (int y = Mathf.Min(yStart, yEnd); y <= Mathf.Max(yStart, yEnd); y++)
            result.Add(new Vector2Int(x, y));
        return result;
    }

    Vector2Int GetClosestBoundaryPoint(RectInt room, Vector2Int point)
    {
        int x = Mathf.Clamp(point.x, room.xMin, room.xMax - 1);
        int y = Mathf.Clamp(point.y, room.yMin, room.yMax - 1);
        return new Vector2Int(x, y);
    }

    string GetSideFromContact(RectInt room, Vector2Int point)
    {
        if (point.y >= room.yMax) return "top";
        if (point.y < room.yMin) return "bottom";
        if (point.x >= room.xMax) return "right";
        if (point.x < room.xMin) return "left";
        return "unknown";
    }

    bool IsSideUsed(RoomData room, string side)
    {
        return side switch
        {
            "top" => room.top,
            "bottom" => room.bottom,
            "left" => room.left,
            "right" => room.right,
            _ => true,
        };
    }

    void SetSideUsed(RoomData room, string side, bool value)
    {
        switch (side)
        {
            case "top": room.top = value; break;
            case "bottom": room.bottom = value; break;
            case "left": room.left = value; break;
            case "right": room.right = value; break;
        }
    }

    void GenerateWalls()
    {
        Vector2Int[] directions = new Vector2Int[]
        {
            Vector2Int.up, Vector2Int.down,
            Vector2Int.left, Vector2Int.right,
            new Vector2Int(-1,1), new Vector2Int(1,1),
            new Vector2Int(-1,-1), new Vector2Int(1,-1)
        };

        HashSet<Vector2Int> wallPositions = new HashSet<Vector2Int>();

        foreach (Vector2Int floor in floorPositions)
        {
            foreach (var dir in directions)
            {
                Vector2Int neighbor = floor + dir;
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
