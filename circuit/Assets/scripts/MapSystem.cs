using System.Collections;
using System.Collections.Generic;
using System.IO;          // Needed for File.ReadAllText / WriteAllText
using UnityEngine;

public enum TileType
{
    Start,
    Floor,
    Wall,
    River,
    End
}

[System.Serializable]
public class TileData
{
    public TileType type;
}

[System.Serializable]
public class Map
{
    public int width;
    public int height;
    public TileType[] tiles; // Flattened array
}

public class MapSystem : MonoBehaviour
{
    public int width = 10;
    public int height = 10;

    public TileData[,] grid;

    void Awake()
    {
        GenerateEmptyMap();
    }

    public void GenerateEmptyMap()
    {
        grid = new TileData[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = new TileData { type = TileType.Floor };
            }
        }
    }

    // Save map to JSON
    public void SaveMap(string path)
    {
        Map data = new Map();
        data.width = width;
        data.height = height;

        data.tiles = new TileType[width * height];

        int index = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                data.tiles[index++] = grid[x, y].type;
            }
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);
    }

    // Load map from JSON
    public void LoadMap(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError("Map file not found: " + path);
            return;
        }

        string json = File.ReadAllText(path);
        Map data = JsonUtility.FromJson<Map>(json);



        width = data.width;
        height = data.height;

        Debug.Log("Loaded map of size: " + width + "x" + height);

        grid = new TileData[width, height];

        int index = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = new TileData { type = data.tiles[index++] };
            }
        }
    }
}
