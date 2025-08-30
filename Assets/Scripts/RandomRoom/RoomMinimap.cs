using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoomMinimap : MonoBehaviour
{
    [Header("Minimap UI")]
    public RectTransform minimapContainer;  // Assign a UI Panel RectTransform
    public GameObject roomIconPrefab;        // Prefab with Image + optional Text
    public float roomIconSize = 20f;

    [Header("Room Type Colors")]
    public Color startColor = Color.green;
    public Color bossColor = Color.red;
    public Color exitColor = Color.yellow;
    public Color shopColor = Color.cyan;
    public Color healColor = Color.magenta;
    public Color normalColor = Color.gray;

    // Reference your room generation script here (or set via inspector)
    public RandomRoomGeneration_Ben roomGenerator;

    private List<GameObject> icons = new List<GameObject>();

    void Start()
    {
        if (roomGenerator == null)
        {
            Debug.LogError("RoomMinimap: Assign a reference to the roomGenerator script!");
            return;
        }
        //GenerateMinimap();
    }

    public void GenerateMinimap()
    {
        // Clear previous icons
        foreach (var icon in icons)
            Destroy(icon);
        icons.Clear();

        if (roomGenerator.rooms.Count == 0)
        {
            Debug.LogWarning("No rooms to show on minimap.");
            return;
        }

        // Find bounding box of all rooms
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        foreach (var room in roomGenerator.rooms)
        {
            Vector2Int pos = room.gridPos;
            if (pos.x < minX) minX = pos.x;
            if (pos.x > maxX) maxX = pos.x;
            if (pos.y < minY) minY = pos.y;
            if (pos.y > maxY) maxY = pos.y;
        }

        float width = maxX - minX;
        float height = maxY - minY;

        RectTransform panelRect = minimapContainer.GetComponent<RectTransform>();
        Vector2 panelSize = panelRect.rect.size;

        float padding = 10f; // Padding inside panel
        float usableWidth = panelSize.x - padding * 2;
        float usableHeight = panelSize.y - padding * 2;

        int roomCount = roomGenerator.rooms.Count;

        // Estimate grid to place icons without overlap:
        // Calculate approx rows and columns (assuming square-ish layout)
        int approxCountX = Mathf.CeilToInt(Mathf.Sqrt(roomCount));
        int approxCountY = approxCountX;

        // Calculate max icon size per cell in the grid
        float maxIconWidth = usableWidth / approxCountX;
        float maxIconHeight = usableHeight / approxCountY;

        // Icon size limited by smaller dimension per cell
        float iconSize = Mathf.Min(maxIconWidth, maxIconHeight);

        // Clamp icon size to reasonable range
        iconSize = Mathf.Clamp(iconSize, 10f, 40f);

        foreach (var room in roomGenerator.rooms)
        {
            GameObject icon = Instantiate(roomIconPrefab, minimapContainer);
            icons.Add(icon);

            RectTransform rt = icon.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(iconSize, iconSize);

            Vector2Int pos = room.gridPos;

            // Normalize position between 0 and 1 to fit inside bounding box
            float normX = width == 0 ? 0.5f : (pos.x - minX) / width;
            float normY = height == 0 ? 0.5f : (pos.y - minY) / height;

            // Map normalized position to minimap panel space, offset to center pivot
            float anchoredX = padding + normX * usableWidth - panelSize.x / 2f;
            float anchoredY = padding + normY * usableHeight - panelSize.y / 2f;

            rt.anchoredPosition = new Vector2(anchoredX, anchoredY);

            Image img = icon.GetComponent<Image>();
            img.color = GetColorForRoomType(room.type);

            Text label = icon.GetComponentInChildren<Text>();
            if (label != null)
                label.text = GetLabelForRoomType(room.type);
        }
    }



    Color GetColorForRoomType(RoomType type)
    {
        return type switch
        {
            RoomType.Start => startColor,
            RoomType.Boss => bossColor,
            RoomType.Exit => exitColor,
            RoomType.Shop => shopColor,
            RoomType.Heal => healColor,
            _ => normalColor,
        };
    }

    string GetLabelForRoomType(RoomType type)
    {
        return type switch
        {
            RoomType.Start => "S",
            RoomType.Boss => "B",
            RoomType.Exit => "E",
            RoomType.Shop => "$",
            RoomType.Heal => "+",
            _ => "",
        };
    }
}
