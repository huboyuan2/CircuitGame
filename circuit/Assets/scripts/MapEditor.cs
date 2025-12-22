using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapEditor : MonoBehaviour
{
    public Camera cam;
    public MapSystem mapSystem;
    public MapRenderer mapRenderer;

    private int tileTypeIndex = 0;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }
    }

    void HandleClick()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Debug.DrawRay(ray.origin, ray.direction * 1000f, Color.red, 1f);

        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            Debug.Log("Raycast did NOT hit anything");
            return;
        }

        Debug.Log($"Raycast HIT: {hit.collider.gameObject.name} at {hit.point}");

        Vector3 pos = hit.point;

        int x = Mathf.FloorToInt(pos.x / mapRenderer.tileSize)+1;
        int y = Mathf.FloorToInt(pos.z / mapRenderer.tileSize)+1;

        Debug.Log($"Clicked on tile coordinates: ({x}, {y})");

        if (!IsInsideMap(x, y))
        {
            Debug.Log("Clicked outside of map bounds");
            return;
        }
        
        // Cycle tile type
        tileTypeIndex = (tileTypeIndex + 1) %
            System.Enum.GetValues(typeof(TileType)).Length;

        mapSystem.grid[x, y].type = (TileType)tileTypeIndex;

        System.Console.WriteLine($"Changed tile at ({x}, {y}) to {(TileType)tileTypeIndex}");

        // Re-render map
        mapRenderer.RenderMap();
        
    }

    bool IsInsideMap(int x, int y)
    {
        return x >= 0 && y >= 0 &&
               x < mapSystem.width &&
               y < mapSystem.height;
    }
}


