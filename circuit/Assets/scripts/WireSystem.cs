using System.Collections.Generic;
using UnityEngine;

public enum WireType
{
    None,
    Normal,
    EnergyLoss,
    Recharge,
    Teleport
}

public class WireSystem : MonoBehaviour
{
    public MapSystem mapSystem;
    public WireRenderer wireRenderer;

    public int width;
    public int height;

    public WireType[,] wireGrid;

    void Awake()
    {
        width = mapSystem.width;
        height = mapSystem.height;

        wireGrid = new WireType[width, height];
        wireRenderer.Init(width, height);
    }

    public bool PlaceWire(int x, int y, WireType type)
    {
        if (!IsValidWirePlacement(x, y))
            return false;

        wireGrid[x, y] = type;
        return true;
    }

    bool IsValidWirePlacement(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return false;

        return mapSystem.grid[x, y].type == TileType.Floor;
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

                bool isWire = wireGrid[nx, ny] != WireType.None;
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
}
