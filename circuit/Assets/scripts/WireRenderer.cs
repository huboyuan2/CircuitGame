using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WireRenderer : MonoBehaviour
{
    public GameObject normalWirePrefab;
    public GameObject energyLossWirePrefab;
    public GameObject rechargeWirePrefab;
    public GameObject teleportWirePrefab;
    public GameObject XWirePrefab;
    public GameObject TWirePrefab;
    public GameObject LWirePrefab;
    public GameObject IWirePrefab;

    public float tileSize = 3f;

    private GameObject[,] wireObjects;

    public void Init(int width, int height)
    {
        wireObjects = new GameObject[width, height];
    }

    // Updated to accept rotation parameter
    public void RenderWire(int x, int y, WireType type, int rotation = 0)
    {
        if (wireObjects[x, y] != null)
            Destroy(wireObjects[x, y]);

        if (type == WireType.None)
        {
            wireObjects[x, y] = null;
            return;
        }

        GameObject prefab = GetPrefab(type);
        Vector3 pos = new Vector3((x - 0.5f) * tileSize, 0.1f, (y - 0.5f) * tileSize);

        // Apply rotation (0 = 0бу, 1 = 90бу, 2 = 180бу, 3 = 270бу)
        Quaternion rotationQuat = Quaternion.Euler(0, rotation * 90, 0);

        wireObjects[x, y] = Instantiate(prefab, pos, rotationQuat, transform);
        wireObjects[x, y].name = $"Wire_{type}_{x}_{y}_R{rotation}";

        Debug.Log($"Rendered wire of type {type} at ({x}, {y}) with rotation {rotation * 90}бу");
    }

    // Get wire GameObject at grid position
    public GameObject GetWireObject(int x, int y)
    {
        if (x < 0 || x >= wireObjects.GetLength(0) ||
            y < 0 || y >= wireObjects.GetLength(1))
            return null;

        return wireObjects[x, y];
    }

    // Swap wire object references (for drag and drop)
    public void SwapWireObjects(int x1, int y1, int x2, int y2)
    {
        if (x1 < 0 || x1 >= wireObjects.GetLength(0) ||
            y1 < 0 || y1 >= wireObjects.GetLength(1) ||
            x2 < 0 || x2 >= wireObjects.GetLength(0) ||
            y2 < 0 || y2 >= wireObjects.GetLength(1))
            return;

        (wireObjects[x1, y1], wireObjects[x2, y2]) = (wireObjects[x2, y2], wireObjects[x1, y1]);
    }

    // Move wire object from source to target (target must be empty)
    public void MoveWireObject(int sourceX, int sourceY, int targetX, int targetY)
    {
        if (sourceX < 0 || sourceX >= wireObjects.GetLength(0) ||
            sourceY < 0 || sourceY >= wireObjects.GetLength(1) ||
            targetX < 0 || targetX >= wireObjects.GetLength(0) ||
            targetY < 0 || targetY >= wireObjects.GetLength(1))
            return;

        // Move wire object reference
        wireObjects[targetX, targetY] = wireObjects[sourceX, sourceY];

        // Clear source reference
        wireObjects[sourceX, sourceY] = null;
    }

    // Re-render wire at position (useful after rotation in WireSystem)
    public void RefreshWire(int x, int y, WireType type, int rotation)
    {
        RenderWire(x, y, type, rotation);
    }

    // Highlight single tile with color
    public void HighlightTile(Vector2Int pos, Color color)
    {
        GameObject tile = GetWireObject(pos.x, pos.y);
        if (tile != null)
        {
            CircuitBlock circuitBlock = tile.GetComponent<CircuitBlock>();
            if (circuitBlock != null)
            {
                circuitBlock.SetColor(color);
            }
        }
    }

    // Set all circuit blocks to a specific color
    public void SetAllCircuitBlocksColor(Color color)
    {
        for (int x = 0; x < wireObjects.GetLength(0); x++)
        {
            for (int y = 0; y < wireObjects.GetLength(1); y++)
            {
                GameObject tile = wireObjects[x, y];
                if (tile != null)
                {
                    CircuitBlock circuitBlock = tile.GetComponent<CircuitBlock>();
                    if (circuitBlock != null)
                    {
                        circuitBlock.SetColor(color);
                    }
                }
            }
        }
    }

    // Animate path with color sequence
    public IEnumerator AnimatePath(List<Vector2Int> path, Color color, float delayBetweenSteps = 0.3f)
    {
        foreach (Vector2Int pos in path)
        {
            HighlightTile(pos, color);
            yield return new WaitForSeconds(delayBetweenSteps);
        }

        Debug.Log("Finished animating path");
    }

    GameObject GetPrefab(WireType type)
    {
        switch (type)
        {
            case WireType.Normal: return normalWirePrefab;
            case WireType.EnergyLoss: return energyLossWirePrefab;
            case WireType.Recharge: return rechargeWirePrefab;
            case WireType.Teleport: return teleportWirePrefab;
            case WireType.Xwire: return XWirePrefab;
            case WireType.Twire: return TWirePrefab;
            case WireType.Lwire: return LWirePrefab;
            case WireType.Iwire: return IWirePrefab;
            default: return null;
        }
    }
}