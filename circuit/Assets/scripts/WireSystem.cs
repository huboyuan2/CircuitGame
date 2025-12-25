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

public class WireNode { public Vector2Int pos; public List<WireNode> neighbors = new List<WireNode>(); public WireNode(int x, int y) { pos = new Vector2Int(x, y); } }
public class WireGraph { public Dictionary<Vector2Int, WireNode> nodes = new Dictionary<Vector2Int, WireNode>(); }

public class WireSystem : MonoBehaviour
{
    // Directions: indices must match dx, dy
    private static readonly int[] dx = { 1, -1, 0, 0 }; // Right, Left, Up, Down
    private static readonly int[] dy = { 0, 0, 1, -1 }; // Right, Left, Up, Down

    // Bit for each direction in a 4-bit mask
    // bit 0: Right, bit 1: Left, bit 2: Up, bit 3: Down
    private const int DIR_RIGHT = 1 << 0;
    private const int DIR_LEFT = 1 << 1;
    private const int DIR_UP = 1 << 2;
    private const int DIR_DOWN = 1 << 3;

    private int GetOppositeDirBit(int dirBit)
    {
        if (dirBit == DIR_RIGHT) return DIR_LEFT;
        if (dirBit == DIR_LEFT) return DIR_RIGHT;
        if (dirBit == DIR_UP) return DIR_DOWN;
        if (dirBit == DIR_DOWN) return DIR_UP;
        return 0;
    }


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
        
        Debug.Log($"Tile ({x},{y}): Type={type}, Rotation={rotation * 90}¡ã, Raw=0x{tile:X} ({tile})");
    }

    // convert to Wire Graph
    public WireGraph BuildGraph()
    {
        WireGraph graph = new WireGraph();

        // 1. Create nodes for all valid tiles
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                TileType tileType = mapSystem.grid[x, y].type;
                WireType wireType = GetWireType(x, y);

                if (tileType == TileType.Start ||
                    tileType == TileType.End ||
                    wireType != WireType.None)
                {
                    graph.nodes[new Vector2Int(x, y)] = new WireNode(x, y);
                }
            }
        }

        // 2. Build adjacency (normal wires)
        foreach (var kv in graph.nodes)
        {
            Vector2Int pos = kv.Key;
            WireNode node = kv.Value;

            int mask = GetConnectionMaskAt(pos.x, pos.y);

            for (int dir = 0; dir < 4; dir++)
            {
                int nx = pos.x + dx[dir];
                int ny = pos.y + dy[dir];

                Vector2Int np = new Vector2Int(nx, ny);
                if (!graph.nodes.ContainsKey(np))
                    continue;

                int dirBit = 1 << dir;
                if ((mask & dirBit) == 0)
                    continue;

                int neighborMask = GetConnectionMaskAt(nx, ny);
                int oppositeBit = GetOppositeDirBit(dirBit);

                if ((neighborMask & oppositeBit) == 0)
                    continue;

                node.neighbors.Add(graph.nodes[np]);
            }
        }

        // 3. Teleport edges
        List<Vector2Int> teleports = new List<Vector2Int>();
        foreach (var kv in graph.nodes)
        {
            if (GetWireType(kv.Key.x, kv.Key.y) == WireType.Teleport)
                teleports.Add(kv.Key);
        }

        // All teleports connect to all teleports
        for (int i = 0; i < teleports.Count; i++)
        {
            for (int j = 0; j < teleports.Count; j++)
            {
                if (i == j) continue;

                var a = graph.nodes[teleports[i]];
                var b = graph.nodes[teleports[j]];

                a.neighbors.Add(b);
            }
        }

        return graph;
    }

    // Get connection mask for a wire type with a given rotation (0–3).
    // Bits: Right, Left, Up, Down.
    // Start/End tiles will be handled separately as "connect all".
    private int GetConnectionMask(WireType type, int rotation)
    {
        // Normalize rotation just in case
        rotation = ((rotation % 4) + 4) % 4;

        // Basic wires: treat as 4-way (you can change this later if needed)
        if (type == WireType.Normal ||
            type == WireType.EnergyLoss ||
            type == WireType.Recharge ||
            type == WireType.Teleport ||
            type == WireType.Xwire)
        {
            // All directions open
            return DIR_RIGHT | DIR_LEFT | DIR_UP | DIR_DOWN;
        }

        // Base masks defined for rotation = 0
        int baseMask = 0;

        switch (type)
        {
            case WireType.Iwire:
                // Horizontal: Left <-> Right
                baseMask = DIR_RIGHT | DIR_LEFT;
                break;

            case WireType.Lwire:
                // L shape: Up + Right (like L rotated 90° CCW visually)
                baseMask = DIR_UP | DIR_RIGHT;
                break;

            case WireType.Twire:
                // T shape: Up + Left + Right (missing Down)
                baseMask = DIR_UP | DIR_LEFT | DIR_RIGHT;
                break;

            default:
                // No connections for None or unknown
                return 0;
        }

        // Apply rotation steps
        return RotateMask(baseMask, rotation);
    }

    private int RotateMask(int mask, int rotation)
    {
        // We model directions as indices: 0=Right,1=Left,2=Up,3=Down.
        // Rotation: each step rotates 90° clockwise.
        bool[] dirs = new bool[4];
        dirs[0] = (mask & DIR_RIGHT) != 0;
        dirs[1] = (mask & DIR_LEFT) != 0;
        dirs[2] = (mask & DIR_UP) != 0;
        dirs[3] = (mask & DIR_DOWN) != 0;

        bool[] rotated = new bool[4];

        for (int i = 0; i < 4; i++)
        {
            if (!dirs[i]) continue;

            // Map index -> mask bit
            int bit = 0;
            if (i == 0) bit = DIR_RIGHT;
            if (i == 1) bit = DIR_LEFT;
            if (i == 2) bit = DIR_UP;
            if (i == 3) bit = DIR_DOWN;

            // Apply rotation steps
            int newIndex = (i + rotation) % 4;

            if (newIndex == 0) rotated[0] = true; // Right
            if (newIndex == 1) rotated[1] = true; // Left
            if (newIndex == 2) rotated[2] = true; // Up
            if (newIndex == 3) rotated[3] = true; // Down
        }

        int result = 0;
        if (rotated[0]) result |= DIR_RIGHT;
        if (rotated[1]) result |= DIR_LEFT;
        if (rotated[2]) result |= DIR_UP;
        if (rotated[3]) result |= DIR_DOWN;

        return result;
    }

    // Convenience: get connection mask at a grid position
    private int GetConnectionMaskAt(int x, int y)
    {
        if (!IsValidPosition(x, y))
            return 0;

        TileType tileType = mapSystem.grid[x, y].type;

        // Start and End act as fully connectable nodes
        if (tileType == TileType.Start || tileType == TileType.End)
        {
            return DIR_RIGHT | DIR_LEFT | DIR_UP | DIR_DOWN;
        }

        WireType wireType = GetWireType(x, y);
        if (wireType == WireType.None)
            return 0;

        int rotation = GetRotation(x, y);
        return GetConnectionMask(wireType, rotation);
    }


}