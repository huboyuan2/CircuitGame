using System.Collections.Generic;
using UnityEngine;

// Enum values now fit in 8 bits (supports up to 256 types)
public enum WireType
{
    None = 0,
    Normal = 1,
    EnergyLoss = 2,
    Recharge = 3,
    Teleport = 4,
    Xwire = 5,
    Twire = 6,
    Lwire = 7,
    Iwire = 8
    // Can add up to 247 more types (9-255)
}

public class WireSystem : MonoBehaviour
{
    public MapSystem mapSystem;
    public WireRenderer wireRenderer;

    public int width;
    public int height;

    // Changed from WireType[,] to int[] for bit-packed storage
    // Format: [22 bits: reserved] [2 bits: rotation] [8 bits: WireType]
    public int[] wireGrid; // Flattened 1D array

    // Bit manipulation constants
    private const int WIRE_TYPE_MASK = 0xFF;       // 0b11111111 - bits 0-7 (WireType)
    private const int ROTATION_MASK = 0x300;       // 0b1100000000 - bits 8-9 (rotation)
    private const int ROTATION_SHIFT = 8;

    void Awake()
    {
        width = mapSystem.width;
        height = mapSystem.height;

        wireGrid = new int[width * height]; // 1D array for better cache performance
        wireRenderer.Init(width, height);
    }

    // Encode wire type and rotation into single int
    private int EncodeTile(WireType type, int rotation)
    {
        return ((int)type & WIRE_TYPE_MASK) | ((rotation & 0x3) << ROTATION_SHIFT);
    }

    // Decode wire type from tile int
    private WireType DecodeWireType(int tile)
    {
        return (WireType)(tile & WIRE_TYPE_MASK);
    }

    // Decode rotation from tile int
    private int DecodeRotation(int tile)
    {
        return (tile & ROTATION_MASK) >> ROTATION_SHIFT;
    }

    // Convert 2D coordinates to 1D array index
    private int GetIndex(int x, int y)
    {
        return y * width + x;
    }

    // Check if position is valid
    private bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    // Place wire with optional rotation
    public bool PlaceWire(int x, int y, WireType type, int rotation = 0)
    {
        if (!IsValidWirePlacement(x, y))
            return false;

        wireGrid[GetIndex(x, y)] = EncodeTile(type, rotation);
        return true;
    }

    bool IsValidWirePlacement(int x, int y)
    {
        if (!IsValidPosition(x, y))
            return false;

        return mapSystem.grid[x, y].type == TileType.Floor;
    }

    // Get wire type at position
    public WireType GetWireType(int x, int y)
    {
        if (!IsValidPosition(x, y))
            return WireType.None;
        
        return DecodeWireType(wireGrid[GetIndex(x, y)]);
    }

    // Get rotation at position
    public int GetRotation(int x, int y)
    {
        if (!IsValidPosition(x, y))
            return 0;
        
        return DecodeRotation(wireGrid[GetIndex(x, y)]);
    }

    // Set rotation at position (preserves wire type)
    public void SetRotation(int x, int y, int rotation)
    {
        if (!IsValidPosition(x, y))
            return;

        int index = GetIndex(x, y);
        WireType type = DecodeWireType(wireGrid[index]);
        wireGrid[index] = EncodeTile(type, rotation % 4);
    }

    // Rotate wire at position
    public void RotateWire(int x, int y, bool clockwise = true)
    {
        if (!IsValidPosition(x, y))
            return;

        int index = GetIndex(x, y);
        WireType type = DecodeWireType(wireGrid[index]);
        
        if (type == WireType.None)
            return;

        int rotation = DecodeRotation(wireGrid[index]);
        rotation = clockwise ? (rotation + 1) % 4 : (rotation + 3) % 4;
        
        wireGrid[index] = EncodeTile(type, rotation);
    }

    // Remove wire at position
    public void RemoveWire(int x, int y)
    {
        if (IsValidPosition(x, y))
        {
            wireGrid[GetIndex(x, y)] = 0; // Set to None with 0 rotation
        }
    }

    // Swap two wire blocks (including their rotations)
    public bool SwapWires(int x1, int y1, int x2, int y2)
    {
        if (!IsValidPosition(x1, y1) || !IsValidPosition(x2, y2))
            return false;

        int index1 = GetIndex(x1, y1);
        int index2 = GetIndex(x2, y2);

        // Swap entire encoded values (type + rotation)
        (wireGrid[index1], wireGrid[index2]) = (wireGrid[index2], wireGrid[index1]);

        return true;
    }

    // Move wire from source to target (target must be empty floor)
    public bool MoveWire(int sourceX, int sourceY, int targetX, int targetY)
    {
        // Validate source position
        if (!IsValidPosition(sourceX, sourceY))
            return false;

        int sourceIndex = GetIndex(sourceX, sourceY);
        WireType sourceType = DecodeWireType(wireGrid[sourceIndex]);

        // Source must have a wire
        if (sourceType == WireType.None)
            return false;

        // Validate target is a valid floor tile
        if (!IsValidWirePlacement(targetX, targetY))
            return false;

        int targetIndex = GetIndex(targetX, targetY);

        // Target must be empty
        if (DecodeWireType(wireGrid[targetIndex]) != WireType.None)
            return false;

        // Move wire data (type + rotation) from source to target
        wireGrid[targetIndex] = wireGrid[sourceIndex];

        // Clear source position
        wireGrid[sourceIndex] = 0;

        return true;
    }

    // Convert world position to grid coordinates
    public bool WorldToGrid(Vector3 worldPos, float tileSize, out int x, out int y)
    {
        x = Mathf.FloorToInt(worldPos.x / tileSize) + 1;
        y = Mathf.FloorToInt(worldPos.z / tileSize) + 1;

        return IsValidPosition(x, y);
    }

    Vector2Int FindTile(TileType type)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (mapSystem.grid[x, y].type == type)
                    return new Vector2Int(x, y);
            }
        }
        return new Vector2Int(-1, -1);
    }

    public bool CheckWin()
    {
        Vector2Int start = FindTile(TileType.Start);
        Vector2Int end = FindTile(TileType.End);

        if (start.x < 0 || end.x < 0)
            return false;

        bool[,] visited = new bool[width, height];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        queue.Enqueue(start);
        visited[start.x, start.y] = true;

        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };

        while (queue.Count > 0)
        {
            var pos = queue.Dequeue();

            if (pos == end)
                return true;

            for (int i = 0; i < 4; i++)
            {
                int nx = pos.x + dx[i];
                int ny = pos.y + dy[i];

                if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                    continue;

                if (visited[nx, ny])
                    continue;

                // Use GetWireType instead of direct array access
                WireType wireType = GetWireType(nx, ny);
                bool isWire = wireType != WireType.None;
                bool isStartOrEnd =
                    mapSystem.grid[nx, ny].type == TileType.Start ||
                    mapSystem.grid[nx, ny].type == TileType.End;

                if (isWire || isStartOrEnd)
                {
                    visited[nx, ny] = true;
                    queue.Enqueue(new Vector2Int(nx, ny));
                }
            }
        }

        return false;
    }

    // Utility: Check if wire type needs rotation (L/T/I shapes)
    public bool WireNeedsRotation(WireType type)
    {
        return type == WireType.Lwire || 
               type == WireType.Twire || 
               type == WireType.Iwire;
    }

    // Debug: Print tile information
    public void DebugPrintTile(int x, int y)
    {
        if (!IsValidPosition(x, y))
        {
            Debug.LogWarning($"Invalid position: ({x}, {y})");
            return;
        }
        
        int tile = wireGrid[GetIndex(x, y)];
        WireType type = DecodeWireType(tile);
        int rotation = DecodeRotation(tile);
        
        Debug.Log($"Tile ({x},{y}): Type={type}, Rotation={rotation * 90}бу, Raw=0x{tile:X} ({tile})");
    }
}