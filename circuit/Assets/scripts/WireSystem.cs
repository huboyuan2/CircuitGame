using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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
        graphDirty = true;  // make graph dirty (needs rebuilding)
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
        graphDirty = true;  // make graph dirty (needs rebuilding)
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
        graphDirty = true;  // make graph dirty (needs rebuilding)
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
        graphDirty = true;  // make graph dirty (needs rebuilding)
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

    private WireGraph cachedGraph = null;
    private bool graphDirty = true;

    //// set dirty when placing wire
    //public bool PlaceWire(int x, int y, WireType type, int rotation = 0)
    //{
    //    if (!IsValidWirePlacement(x, y)) return false;
    //    wireGrid[GetIndex(x, y)] = EncodeTile(type, rotation);
    //    graphDirty = true;  //need to rebuild graph
    //    return true;
    //}

    public class PathResult
{
    public bool found;
    public List<Vector2Int> path;  // list of grid positions from start to end

        public PathResult(bool found, List<Vector2Int> path = null)
    {
        this.found = found;
        this.path = path ?? new List<Vector2Int>();
    }
}

public PathResult FindPath()
{
        // cache graph if dirty
        if (graphDirty)
    {
        cachedGraph = BuildGraph();
        graphDirty = false;
    }

    Vector2Int start = FindTile(TileType.Start);
    Vector2Int end = FindTile(TileType.End);

    if (!cachedGraph.nodes.ContainsKey(start) || !cachedGraph.nodes.ContainsKey(end))
        return new PathResult(false);

    // BFS Data Structures
    HashSet<WireNode> visited = new HashSet<WireNode>();
    Queue<WireNode> queue = new Queue<WireNode>();
    Dictionary<WireNode, WireNode> cameFrom = new Dictionary<WireNode, WireNode>();  // record parent

    WireNode startNode = cachedGraph.nodes[start];
    WireNode endNode = cachedGraph.nodes[end];
    
    queue.Enqueue(startNode);
    visited.Add(startNode);
    cameFrom[startNode] = null;  // start has no parent

    // BFS search
    while (queue.Count > 0)
    {
        WireNode current = queue.Dequeue();

        // found end node, reconstruct path
        if (current == endNode)
        {
            return new PathResult(true, ReconstructPath(cameFrom, startNode, endNode));
        }

        foreach (WireNode neighbor in current.neighbors)
        {
            if (!visited.Contains(neighbor))
            {
                visited.Add(neighbor);
                queue.Enqueue(neighbor);
                cameFrom[neighbor] = current;  // record parent for neighbor
                }
        }
    }

    return new PathResult(false);
}

    // ReconstructPath from end to start using cameFrom map
    private List<Vector2Int> ReconstructPath(
    Dictionary<WireNode, WireNode> cameFrom, 
    WireNode start, 
    WireNode end)
{
    List<Vector2Int> path = new List<Vector2Int>();
    WireNode current = end;

        // from end to start
        while (current != null)
    {
        path.Add(current.pos);
        cameFrom.TryGetValue(current, out current);
    }

        // reverse the path to get it from start to end
        path.Reverse();
    
    return path;
}

    // Only check connectivity for win condition
    public bool CheckWin()
{
        var result = FindPath();
        if (result.found)
        {
            StartCoroutine(wireRenderer.AnimatePath(result.path, Color.green));
        }
        else 
        {
            // When start and end are disconnected, set all CircuitBlocks to red
            wireRenderer.SetAllCircuitBlocksColor(Color.red);
        }
            return result.found;
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

    // Get connection mask for a wire type with a given rotation (0?).
    // Bits: Right, Left, Up, Down.
    // Start/End tiles will be handled separately as "connect all".
    private int GetConnectionMask(WireType type, int rotation)
    {
        Debug.Log($"Getting connection mask for type {type} with rotation {rotation}");
        // Normalize rotation just in case
        rotation = (rotation + 4) % 4;

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
                // Horizontal: Up <-> Down
                baseMask = DIR_UP | DIR_DOWN;
                break;

            case WireType.Lwire:
                // L shape: Up + Right (like L rotated 90?CCW visually)
                baseMask = DIR_UP | DIR_RIGHT;
                break;

            case WireType.Twire:
                // T shape: Down + Left + Right (missing UP)
                baseMask = DIR_DOWN | DIR_LEFT | DIR_RIGHT;
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
        //normalize rotation
        rotation = (rotation + 4) % 4;

        // Clockwise Rotate rule: Right ¡ú Down ¡ú Left ¡ú Up ¡ú Right
        int[,] rotationMap = new int[4, 4]
        {
        // rotation=0 (Clockwise rotate 0¡ã)
        { DIR_RIGHT, DIR_LEFT, DIR_UP, DIR_DOWN },

        // rotation=1 (Clockwise rotate 90¡ã)
        // Right¡úDown, Left¡úUp, Up¡úRight, Down¡úLeft
        { DIR_DOWN, DIR_UP, DIR_RIGHT, DIR_LEFT },

        // rotation=2 (Clockwise rotate 180¡ã)
        // Right¡úLeft, Left¡úRight, Up¡úDown, Down¡úUp
        { DIR_LEFT, DIR_RIGHT, DIR_DOWN, DIR_UP },

        // rotation=3 (Clockwise rotate 270¡ã)
        // Right¡úUp, Left¡úDown, Up¡úLeft, Down¡úRight
        { DIR_UP, DIR_DOWN, DIR_LEFT, DIR_RIGHT }
        };

        int result = 0;

        // check each direction in the original mask and map it
        if ((mask & DIR_RIGHT) != 0) result |= rotationMap[rotation, 0];
        if ((mask & DIR_LEFT) != 0) result |= rotationMap[rotation, 1];
        if ((mask & DIR_UP) != 0) result |= rotationMap[rotation, 2];
        if ((mask & DIR_DOWN) != 0) result |= rotationMap[rotation, 3];

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

    //private IEnumerator AnimatePath(List<Vector2Int> path)
    //{
    //    foreach (Vector2Int pos in path)
    //    {
    //        HighlightTile(pos, Color.green);
    //        //PlayPathEffect(pos);
    //        yield return new WaitForSeconds(0.3f);
    //    }

    //    Debug.Log("finished animating path");
    //}

    //private void HighlightTile(Vector2Int pos, Color color)
    //{
    //    //find tile GameObject and change its color
    //    GameObject tile = wireRenderer.GetWireObject(pos.x, pos.y);
    //    if (tile != null)
    //    {
    //        //Renderer rend = tile.GetComponent<Renderer>();
    //        //if (rend != null)
    //        //{
    //        //    rend.material.color = color;
    //        //}
    //        CircuitBlock circuitBlock = tile.GetComponent<CircuitBlock>();
    //        if (circuitBlock != null)
    //        {
    //            circuitBlock.SetColor(color);
    //        }
    //    }
    //}
    //private void SetAllCircuitBlocksColor(Color color)
    //{
    //    for (int x = 0; x < width; x++)
    //    {
    //        for (int y = 0; y < height; y++)
    //        {
    //            WireType wireType = GetWireType(x, y);
    //            if (wireType != WireType.None)
    //            {
    //                GameObject tile = wireRenderer.GetWireObject(x, y);
    //                if (tile != null)
    //                {
    //                    CircuitBlock circuitBlock = tile.GetComponent<CircuitBlock>();
    //                    if (circuitBlock != null)
    //                    {
    //                        circuitBlock.SetColor(color);
    //                    }
    //                }
    //            }
    //        }
    //    }
    //}

}