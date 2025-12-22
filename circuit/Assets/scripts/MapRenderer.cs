using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapRenderer : MonoBehaviour
{
    public MapSystem mapSystem;
    public GameObject floorPrefab;
    public GameObject wallPrefab;
    public GameObject riverPrefab;
    public GameObject startPrefab;
    public GameObject endPrefab;
    public float tileSize = 3f; // set this in Inspector

    private GameObject[,] spawnedTiles;

    public void RenderMap()
    {
        ClearMap();

        int width = mapSystem.width;
        int height = mapSystem.height;

        spawnedTiles = new GameObject[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                TileType type = mapSystem.grid[x, y].type;
                GameObject prefab = GetPrefab(type);

                Vector3 pos = new Vector3(x * tileSize, 0, y * tileSize);
                spawnedTiles[x, y] = Instantiate(prefab, pos, Quaternion.identity, transform);
            }
        }
    }

    GameObject GetPrefab(TileType type)
    {
        switch (type)
        {
            case TileType.Floor: return floorPrefab;
            case TileType.Wall: return wallPrefab;
            case TileType.River: return riverPrefab;
            case TileType.Start: return startPrefab;
            case TileType.End: return endPrefab;
            default: return floorPrefab;
        }
    }


    void ClearMap()
    {
        if (spawnedTiles == null) return;

        foreach (var obj in spawnedTiles)
        {
            if (obj != null) DestroyImmediate(obj);
            //if (obj != null) Destroy(obj);
        }
    }
}

