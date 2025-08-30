using System.Collections.Generic;
using UnityEngine;

public enum RoomType
{
    Start,
    Boss,
    Exit,
    Shop,
    Heal,
    Normal
}

[System.Serializable]
public class RoomTypeConfig
{
    public RoomType type;
    public string label = "Room";
    public int minimumCount = 1;
}

public class RandomRoomGeneration_Ben : MonoBehaviour
{
    [Header("Map Settings")]
    public int mapWidth = 100;
    public int mapHeight = 100;

    [Header("Room Settings")]
    public int roomCount = 10;
    public int minRoomWidth = 5;
    public int maxRoomWidth = 10;
    public int minRoomHeight = 5;
    public int maxRoomHeight = 10;

    [Header("Tile Prefabs")]
    public GameObject floorTilePrefab;
    public GameObject wallTilePrefab;

    [Header("Room Type Requirements")]
    public List<RoomTypeConfig> roomTypeConfigs = new List<RoomTypeConfig>();

    public class RoomData
    {
        public RectInt rect;
        public bool top, bottom, left, right;
        public Vector2Int gridPos;
        public RoomType type = RoomType.Normal;

        public RoomData(RectInt rect, Vector2Int gridPos)
        {
            this.rect = rect;
            this.gridPos = gridPos;
            top = bottom = left = right = false;
        }
    }

    public List<RoomData> rooms = new List<RoomData>();
    public Dictionary<Vector2Int, RoomData> gridToRoom = new Dictionary<Vector2Int, RoomData>();
    public HashSet<Vector2Int> floorPositions = new HashSet<Vector2Int>();

    public readonly Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
    public readonly Dictionary<Vector2Int, string> dirToSide = new Dictionary<Vector2Int, string>()
    {
        { Vector2Int.up, "top" }, { Vector2Int.down, "bottom" }, { Vector2Int.left, "left" }, { Vector2Int.right, "right" }
    };
    public readonly Dictionary<string, string> oppositeSide = new Dictionary<string, string>()
    {
        { "top", "bottom" }, { "bottom", "top" }, { "left", "right" }, { "right", "left" }
    };

    public RoomMinimap minimap;

    void Start() => Generate();

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            Generate();
    }

    void Generate()
    {
        rooms.Clear();
        gridToRoom.Clear();
        floorPositions.Clear();
        foreach (Transform child in transform) Destroy(child.gameObject);

        GenerateRooms();
        AssignRoomTypes();

        for (int i = 0; i < rooms.Count; i++)
        {
            DrawRoom(rooms[i].rect, i + 1, rooms[i].type);
        }

        ConnectRooms();
        GenerateWalls();
        minimap.GenerateMinimap();
    }

    void GenerateRooms()
    {
        Vector2Int startGridPos = Vector2Int.zero;
        RoomData firstRoom = CreateRandomRoomAtGridPos(startGridPos);
        rooms.Add(firstRoom);
        gridToRoom[startGridPos] = firstRoom;

        Queue<RoomData> expansionQueue = new Queue<RoomData>();
        expansionQueue.Enqueue(firstRoom);

        while (rooms.Count < roomCount && expansionQueue.Count > 0)
        {
            RoomData currentRoom = expansionQueue.Dequeue();
            List<Vector2Int> shuffledDirs = new List<Vector2Int>(directions);
            Shuffle(shuffledDirs);

            foreach (Vector2Int dir in shuffledDirs)
            {
                if (rooms.Count >= roomCount) break;

                Vector2Int neighborGridPos = currentRoom.gridPos + dir;
                if (gridToRoom.ContainsKey(neighborGridPos)) continue;

                string currentSide = dirToSide[dir];
                string neighborSide = oppositeSide[currentSide];

                if (IsSideUsed(currentRoom, currentSide)) continue;

                RoomData newRoom = CreateRandomRoomAtGridPos(neighborGridPos);
                bool overlaps = false;
                foreach (var existing in rooms)
                {
                    if (existing.rect.Overlaps(newRoom.rect))
                    {
                        overlaps = true;
                        break;
                    }
                }

                if (overlaps) continue;

                rooms.Add(newRoom);
                gridToRoom[neighborGridPos] = newRoom;

                SetSideUsed(currentRoom, currentSide, true);
                SetSideUsed(newRoom, neighborSide, true);

                expansionQueue.Enqueue(newRoom);
                break;
            }
        }
    }

    RoomData CreateRandomRoomAtGridPos(Vector2Int gridPos)
    {
        int width = Random.Range(minRoomWidth, maxRoomWidth + 1);
        int height = Random.Range(minRoomHeight, maxRoomHeight + 1);
        int x = mapWidth / 2 + gridPos.x * (maxRoomWidth + 2);
        int y = mapHeight / 2 + gridPos.y * (maxRoomHeight + 2);
        return new RoomData(new RectInt(x, y, width, height), gridPos);
    }

    void AssignRoomTypes()
    {
        if (rooms.Count == 0) return;

        foreach (var r in rooms) r.type = RoomType.Normal;

        RoomData start = rooms[0];
        start.type = RoomType.Start;

        RoomData farthest = null;
        float maxDist = 0;
        foreach (var r in rooms)
        {
            float dist = Vector2Int.Distance(start.gridPos, r.gridPos);
            if (dist > maxDist)
            {
                maxDist = dist;
                farthest = r;
            }
        }

        if (farthest != null)
            farthest.type = RoomType.Boss;

        HashSet<RoomData> reserved = new HashSet<RoomData> { start, farthest };
        List<RoomData> available = new List<RoomData>(rooms);
        available.RemoveAll(r => reserved.Contains(r));

        var filteredConfigs = roomTypeConfigs.FindAll(c => c.type != RoomType.Start && c.type != RoomType.Boss);
        int totalRequired = 0;
        foreach (var config in filteredConfigs)
            totalRequired += config.minimumCount;

        if (totalRequired > available.Count)
        {
            Debug.LogError($"[Room Generation] Not enough rooms to satisfy type requirements! " +
                           $"Have {available.Count}, need {totalRequired}. Increase roomCount.");
            return;
        }

        foreach (var config in filteredConfigs)
        {
            for (int i = 0; i < config.minimumCount; i++)
            {
                if (available.Count == 0) break;
                int index = Random.Range(0, available.Count);
                RoomData room = available[index];
                room.type = config.type;
                available.RemoveAt(index);
            }
        }

        foreach (var r in available)
            r.type = RoomType.Normal;
    }

    void DrawRoom(RectInt room, int roomIndex, RoomType type)
    {
        GameObject roomParent = new GameObject($"Room{roomIndex}_{type}");
        roomParent.transform.parent = transform;

        for (int x = room.xMin; x < room.xMax; x++)
        {
            for (int y = room.yMin; y < room.yMax; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                floorPositions.Add(pos);
                if (floorTilePrefab != null)
                {
                    GameObject tile = Instantiate(floorTilePrefab, new Vector3(x, y, 0), Quaternion.identity, roomParent.transform);
                }
            }
        }
    }

    void ConnectRooms()
    {
        foreach (var room in rooms)
        {
            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighborPos = room.gridPos + dir;
                if (!gridToRoom.ContainsKey(neighborPos)) continue;

                RoomData neighbor = gridToRoom[neighborPos];
                string side = dirToSide[dir];
                string opposite = oppositeSide[side];

                if (IsSideUsed(room, side) && IsSideUsed(neighbor, opposite))
                {
                    int roomIndexA = rooms.IndexOf(room) + 1;
                    int roomIndexB = rooms.IndexOf(neighbor) + 1;
                    CreateCorridor(room, neighbor, side, roomIndexA, roomIndexB);
                }
            }
        }
    }

    void CreateCorridor(RoomData roomA, RoomData roomB, string sideA, int indexA, int indexB)
    {
        string corridorName = $"Room{indexA}ConnectingToRoom{indexB}";
        GameObject corridorParent = new GameObject(corridorName);
        corridorParent.transform.parent = transform;

        Vector2Int start = Vector2Int.zero, end = Vector2Int.zero;

        if (sideA == "top")
        {
            int x = (Mathf.Max(roomA.rect.xMin, roomB.rect.xMin) + Mathf.Min(roomA.rect.xMax, roomB.rect.xMax)) / 2;
            start = new Vector2Int(x, roomA.rect.yMax - 1);
            end = new Vector2Int(x, roomB.rect.yMin);
        }
        else if (sideA == "bottom")
        {
            int x = (Mathf.Max(roomA.rect.xMin, roomB.rect.xMin) + Mathf.Min(roomA.rect.xMax, roomB.rect.xMax)) / 2;
            start = new Vector2Int(x, roomA.rect.yMin);
            end = new Vector2Int(x, roomB.rect.yMax - 1);
        }
        else if (sideA == "left")
        {
            int y = (Mathf.Max(roomA.rect.yMin, roomB.rect.yMin) + Mathf.Min(roomA.rect.yMax, roomB.rect.yMax)) / 2;
            start = new Vector2Int(roomA.rect.xMin, y);
            end = new Vector2Int(roomB.rect.xMax - 1, y);
        }
        else if (sideA == "right")
        {
            int y = (Mathf.Max(roomA.rect.yMin, roomB.rect.yMin) + Mathf.Min(roomA.rect.yMax, roomB.rect.yMax)) / 2;
            start = new Vector2Int(roomA.rect.xMax - 1, y);
            end = new Vector2Int(roomB.rect.xMin, y);
        }

        if (start.x == end.x)
        {
            for (int y = Mathf.Min(start.y, end.y); y <= Mathf.Max(start.y, end.y); y++)
            {
                Vector2Int pos = new Vector2Int(start.x, y);
                if (floorPositions.Add(pos) && floorTilePrefab != null)
                    Instantiate(floorTilePrefab, new Vector3(pos.x, pos.y, 0), Quaternion.identity, corridorParent.transform);
            }
        }
        else if (start.y == end.y)
        {
            for (int x = Mathf.Min(start.x, end.x); x <= Mathf.Max(start.x, end.x); x++)
            {
                Vector2Int pos = new Vector2Int(x, start.y);
                if (floorPositions.Add(pos) && floorTilePrefab != null)
                    Instantiate(floorTilePrefab, new Vector3(pos.x, pos.y, 0), Quaternion.identity, corridorParent.transform);
            }
        }
    }

    void GenerateWalls()
    {
        Vector2Int[] neighbors = {
            Vector2Int.up, Vector2Int.down,
            Vector2Int.left, Vector2Int.right,
            new Vector2Int(-1,1), new Vector2Int(1,1),
            new Vector2Int(-1,-1), new Vector2Int(1,-1)
        };

        HashSet<Vector2Int> wallPositions = new HashSet<Vector2Int>();

        foreach (Vector2Int floor in floorPositions)
        {
            foreach (Vector2Int dir in neighbors)
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

    void SetSideUsed(RoomData room, string side, bool used)
    {
        switch (side)
        {
            case "top": room.top = used; break;
            case "bottom": room.bottom = used; break;
            case "left": room.left = used; break;
            case "right": room.right = used; break;
        }
    }

    void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = Random.Range(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
