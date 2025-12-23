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

    public void RenderWire(int x, int y, WireType type)
    {
        if (wireObjects[x, y] != null)
            Destroy(wireObjects[x, y]);
        if (type == WireType.None)
            return; 
        GameObject prefab = GetPrefab(type);
        Vector3 pos = new Vector3((x-0.5f) * tileSize, 0.1f, (y - 0.5f) * tileSize);

        wireObjects[x, y] = Instantiate(prefab, pos, Quaternion.identity, transform);
        Debug.Log($"Rendered wire of type {type} at ({x}, {y})");
    }

    GameObject GetPrefab(WireType type)
    {
        switch (type)
        {
            //case WireType.None:
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


