using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class RandomRoomGeneration_Ben : MonoBehaviour
{

    /* Room size constraints
     * Full dungeon size constraints
     * corridors connecting rooms
     * prefabs to fill walls/floor
     * symbols to say which rooms are which
     */

    [Header("Map Settings")]
    public int mapHeight;
    public int mapWidth;

    [Header("Room Settings")]
    public int roomCount;
    public int minRoomHeight;
    public int minRoomWidth;
    public int maxRoomHeight;
    public int maxRoomWidth;

    [Header("Tile Prefabs")]
    public GameObject floorTile;
    public GameObject wallTile;

    //List to hold the rooms
    private List<Rect> rooms = new List<Rect>();

    // Set of all floor positions to help with wall placement
    private HashSet<Vector2Int> floorPositions = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> wallPositions = new HashSet<Vector2Int>();

    private void Start()
    {
        GenerateDungeon();
    }
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            rooms.Clear();
            floorPositions.Clear();
            wallPositions.Clear();
            foreach(Transform child in transform)
            {
                Destroy(child.gameObject);
            }
            GenerateDungeon();

        }
    }

    public void GenerateDungeon()
    {
        GenerateRooms();
        GenerateWalls();
    }

    public void GenerateRooms()
    {
        int attempts = 0;
        int maxAttempts = roomCount * 5;

        while (rooms.Count < roomCount && attempts < maxAttempts)
        {
            attempts++;

            int width = Random.Range(minRoomWidth, maxRoomWidth + 1);
            int height = Random.Range(minRoomHeight, maxRoomHeight + 1);

            int x = Random.Range(1, mapWidth - width - 1);
            int y = Random.Range(1, mapHeight - height - 1);

            Rect newRoom = new Rect(x, y, width, height);

            bool overlap = false;

            foreach(Rect room in rooms)
            {
                if(newRoom.Overlaps(room))
                {
                    overlap = true;
                    break;
                }
            }
            if (!overlap)
            {
                rooms.Add(newRoom);
                GenerateFloor(newRoom);
            }
        }
        Debug.Log($"Generated {rooms.Count} rooms after {attempts} attempts.");
    }

    public void GenerateFloor(Rect room)
    {
        for (int x = (int)room.xMin; x < (int)room.xMax; x++)
        {
            for (int y = (int)room.yMin; y < (int)room.yMax; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);

                // Track floor position
                floorPositions.Add(pos);

                // Instantiate floor tile prefab at position
                if (floorTile != null)
                {
                    Instantiate(floorTile, new Vector3(x, y, 0), Quaternion.identity, transform);
                }
            }
        }
    }

    public void GenerateWalls()
    {
        Vector2Int[] directions = new Vector2Int[]
        {
            Vector2Int.up, Vector2Int.down,
            Vector2Int.left, Vector2Int.right,
            new Vector2Int(1,1), new Vector2Int(-1,1),
            new Vector2Int(1,-1), new Vector2Int(-1,-1)
        };

        foreach (Vector2Int floorPos in floorPositions)
        {
            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighbor = floorPos + dir;

                // If no floor and no wall here, place a wall tile
                if (!floorPositions.Contains(neighbor) && !wallPositions.Contains(neighbor))
                {
                    wallPositions.Add(neighbor);
                    if (wallTile != null)
                        Instantiate(wallTile, new Vector3(neighbor.x, neighbor.y, 0), Quaternion.identity, transform);
                }
            }
        }
    }

}
