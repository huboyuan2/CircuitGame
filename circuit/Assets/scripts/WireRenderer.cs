using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// Data container for each wire type configuration
[System.Serializable]
public class WireTypeData
{
    [Header("Basic Configuration")]
    public WireType wireType;
    public GameObject prefab;
    
    [Header("Icon Settings")]
    [Tooltip("Pre-baked icon texture (optional, auto-generates if null)")]
    public Texture2D prebakedIcon;
    
    [Tooltip("Auto-generated RenderTexture (filled at runtime)")]
    [HideInInspector]
    public RenderTexture iconRenderTexture;
    
    [Header("Capture Settings")]
    [Tooltip("Use runtime capture even if prebaked icon exists")]
    public bool forceRuntimeCapture = false;
    
    [Tooltip("Camera rotation when capturing icon")]
    public Vector3 captureRotation = new Vector3(15, 45, 0);
    
    [Tooltip("Camera distance from prefab")]
    public float captureDistance = 5f;
}

public class WireRenderer : MonoBehaviour
{
    [Header("Wire Type Configuration")]
    [Tooltip("Configure all wire types here (order matches WireType enum)")]
    public List<WireTypeData> wireTypeDataList = new List<WireTypeData>();
    
    [Header("Icon Generation Settings")]
    [Tooltip("Resolution of generated icons (width and height)")]
    public int iconResolution = 256;
    
    [Tooltip("Layer used for icon capture (should be isolated)")]
    public LayerMask iconCaptureLayer = 1 << 8; // Default to layer 8
    
    [Tooltip("Background color for icon rendering (alpha=0 for transparency)")]
    public Color iconBackgroundColor = new Color(0, 0, 0, 0);
    
    [Tooltip("Field of view for icon camera")]
    public float iconCameraFOV = 30f;
    
    [Header("Gameplay Settings")]
    public float tileSize = 3f;
    
    // Runtime data
    private GameObject[,] wireObjects;
    private Dictionary<WireType, GameObject> wirePrefabDict;
    public Dictionary<WireType, RenderTexture> wireIconDict;
    
    // Icon generation system
    private Camera iconCamera;
    private GameObject iconStage;
    private bool iconsReady = false;
    public static Action OnIconsReady;
    // Public property to check if icons are ready
    public bool IconsReady => iconsReady;
    // Singleton instance
    private static WireRenderer _instance;
    public static WireRenderer Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<WireRenderer>();

                // If still not found, create a new GameObject with WireRenderer
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject("WireRenderer");
                    _instance = singletonObject.AddComponent<WireRenderer>();
                    DontDestroyOnLoad(singletonObject);
                }
            }
            return _instance;
        }
    }
    void Awake()
    {
        BuildPrefabDictionary();
        InitializeIcons();
        //Invoke("InitializeIcons", 0.5f);
    }
    
    // Initialize icon generation (call this after scene is ready)
    public void InitializeIcons()
    {
        StartCoroutine(GenerateWireIcons());
    }
    
    public void Init(int width, int height)
    {
        wireObjects = new GameObject[width, height];
    }
    
    // Build prefab lookup dictionary
    private void BuildPrefabDictionary()
    {
        wirePrefabDict = new Dictionary<WireType, GameObject>();
        
        foreach (var data in wireTypeDataList)
        {
            if (!wirePrefabDict.ContainsKey(data.wireType))
            {
                wirePrefabDict[data.wireType] = data.prefab;
            }
            else
            {
                Debug.LogWarning($"WireRenderer: Duplicate WireType entry found: {data.wireType}");
            }
        }
    }
    
    // Generate icons for all wire types
    private IEnumerator GenerateWireIcons()
    {
        Debug.Log("WireRenderer: Starting icon generation...");
        
        // Setup icon capture environment
        SetupIconCaptureEnvironment();
        
        // Ensure icon dictionary exists
        wireIconDict = new Dictionary<WireType, RenderTexture>();
        
        int generatedCount = 0;
        int prebakedCount = 0;
        
        foreach (var data in wireTypeDataList)
        {
            if (data.wireType == WireType.None) continue;
            
            RenderTexture icon = null;
            
            // Use prebaked icon if available and runtime capture not forced
            if (data.prebakedIcon != null && !data.forceRuntimeCapture)
            {
                icon = ConvertTexture2DToRenderTexture(data.prebakedIcon);
                prebakedCount++;
                Debug.Log($"WireRenderer: Using prebaked icon for {data.wireType}");
            }
            else
            {
                // Runtime capture
                if (data.prefab != null)
                {
                    icon = CaptureWireIcon(data);
                    generatedCount++;
                    Debug.Log($"WireRenderer: Generated runtime icon for {data.wireType}");
                }
                else
                {
                    Debug.LogWarning($"WireRenderer: No prefab assigned for {data.wireType}, skipping icon generation");
                }
            }
            
            // Store the icon
            if (icon != null)
            {
                data.iconRenderTexture = icon;
                wireIconDict[data.wireType] = icon;
            }
            
            // Yield to avoid blocking main thread
            yield return null;
        }
        
        // Cleanup
        CleanupIconCaptureEnvironment();
        
        iconsReady = true;
        OnIconsReady.Invoke();
        Debug.Log($"WireRenderer: Icon generation complete! (Generated: {generatedCount}, Prebaked: {prebakedCount})");
    }
    
    // Setup isolated rendering environment for icon capture
    private void SetupIconCaptureEnvironment()
    {
        // Create icon stage (isolated container)
        iconStage = new GameObject("IconCaptureStage");
        iconStage.transform.position = new Vector3(10000, 10000, 10000); // Far away from gameplay
        
        // Create icon camera
        GameObject cameraObj = new GameObject("IconCamera");
        cameraObj.transform.SetParent(iconStage.transform);
        iconCamera = cameraObj.AddComponent<Camera>();
        
        // Configure camera
        iconCamera.clearFlags = CameraClearFlags.SolidColor;
        iconCamera.backgroundColor = iconBackgroundColor;
        iconCamera.cullingMask = iconCaptureLayer;
        iconCamera.orthographic = false;
        iconCamera.fieldOfView = iconCameraFOV;
        iconCamera.nearClipPlane = 0.1f;
        iconCamera.farClipPlane = 100f;
        iconCamera.enabled = false; // Manual rendering only
    }
    
    // Capture icon for a specific wire type
    private RenderTexture CaptureWireIcon(WireTypeData data)
    {
        // Instantiate prefab on icon stage
        GameObject instance = Instantiate(data.prefab, iconStage.transform);
        
        // Set layer for all children
        SetLayerRecursively(instance, GetLayerFromMask(iconCaptureLayer));
        
        // Calculate bounds for proper framing
        Bounds bounds = CalculateBounds(instance);
        
        // Position camera
        Vector3 cameraOffset = Quaternion.Euler(data.captureRotation) * Vector3.back * data.captureDistance;
        iconCamera.transform.position = bounds.center + cameraOffset;
        iconCamera.transform.LookAt(bounds.center);
        
        // Create RenderTexture
        RenderTexture rt = new RenderTexture(iconResolution, iconResolution, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 4; // 4x MSAA for better quality
        rt.Create();
        
        // Capture
        iconCamera.targetTexture = rt;
        iconCamera.Render();
        iconCamera.targetTexture = null;
        
        // Cleanup
        Destroy(instance);
        
        return rt;
    }
    
    // Convert Texture2D to RenderTexture
    private RenderTexture ConvertTexture2DToRenderTexture(Texture2D texture)
    {
        RenderTexture rt = new RenderTexture(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
        rt.Create();
        
        Graphics.Blit(texture, rt);
        
        return rt;
    }
    
    // Calculate bounds of a GameObject and its children
    private Bounds CalculateBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return new Bounds(obj.transform.position, Vector3.one);
        }
        
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        
        return bounds;
    }
    
    // Set layer recursively
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
    
    // Get first active layer from LayerMask
    private int GetLayerFromMask(LayerMask mask)
    {
        int layerValue = mask.value;
        for (int i = 0; i < 32; i++)
        {
            if ((layerValue & (1 << i)) != 0)
                return i;
        }
        return 0;
    }
    
    // Cleanup icon capture environment
    private void CleanupIconCaptureEnvironment()
    {
        if (iconStage != null)
        {
            Destroy(iconStage);
            iconStage = null;
        }
        iconCamera = null;
    }
    
    // Public API: Get icon RenderTexture for a wire type
    public RenderTexture GetWireIcon(WireType wireType)
    {
        if (wireIconDict != null && wireIconDict.TryGetValue(wireType, out RenderTexture rt))
        {
            return rt;
        }
        
        Debug.LogWarning($"WireRenderer: No icon found for {wireType}");
        return null;
    }
    
    // Public API: Get prefab for a wire type
    public GameObject GetWirePrefab(WireType wireType)
    {
        if (wirePrefabDict != null && wirePrefabDict.TryGetValue(wireType, out GameObject prefab))
        {
            return prefab;
        }
        
        Debug.LogWarning($"WireRenderer: No prefab found for {wireType}");
        return null;
    }
    
    // Render wire at grid position
    public void RenderWire(int x, int y, WireType type, int rotation = 0)
    {
        if (wireObjects[x, y] != null)
            Destroy(wireObjects[x, y]);

        if (type == WireType.None)
        {
            wireObjects[x, y] = null;
            return;
        }

        GameObject prefab = GetWirePrefab(type);
        if (prefab == null)
        {
            Debug.LogError($"WireRenderer: Cannot render {type}, prefab not found");
            return;
        }
        
        Vector3 pos = new Vector3((x - 0.5f) * tileSize, 0.1f, (y - 0.5f) * tileSize);
        Quaternion rotationQuat = Quaternion.Euler(0, rotation * 90, 0);

        wireObjects[x, y] = Instantiate(prefab, pos, rotationQuat, transform);
        wireObjects[x, y].name = $"Wire_{type}_{x}_{y}_R{rotation}";
    }

    // Get wire GameObject at grid position
    public GameObject GetWireObject(int x, int y)
    {
        if (x < 0 || x >= wireObjects.GetLength(0) ||
            y < 0 || y >= wireObjects.GetLength(1))
            return null;

        return wireObjects[x, y];
    }

    // Swap wire object references
    public void SwapWireObjects(int x1, int y1, int x2, int y2)
    {
        if (x1 < 0 || x1 >= wireObjects.GetLength(0) ||
            y1 < 0 || y1 >= wireObjects.GetLength(1) ||
            x2 < 0 || x2 >= wireObjects.GetLength(0) ||
            y2 < 0 || y2 >= wireObjects.GetLength(1))
            return;

        (wireObjects[x1, y1], wireObjects[x2, y2]) = (wireObjects[x2, y2], wireObjects[x1, y1]);
    }

    // Move wire object from source to target
    public void MoveWireObject(int sourceX, int sourceY, int targetX, int targetY)
    {
        if (sourceX < 0 || sourceX >= wireObjects.GetLength(0) ||
            sourceY < 0 || sourceY >= wireObjects.GetLength(1) ||
            targetX < 0 || targetX >= wireObjects.GetLength(0) ||
            targetY < 0 || targetY >= wireObjects.GetLength(1))
            return;

        wireObjects[targetX, targetY] = wireObjects[sourceX, sourceY];
        wireObjects[sourceX, sourceY] = null;
    }

    // Re-render wire at position
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
    
    // Editor helper: Initialize wire type data list
    [ContextMenu("Initialize Wire Type Data List")]
    private void InitializeWireTypeDataList()
    {
        wireTypeDataList.Clear();
        
        foreach (WireType type in System.Enum.GetValues(typeof(WireType)))
        {
            WireTypeData data = new WireTypeData
            {
                wireType = type,
                prefab = null,
                captureRotation = new Vector3(15, 45, 0),
                captureDistance = 5f
            };
            
            wireTypeDataList.Add(data);
        }
        
        Debug.Log($"WireRenderer: Initialized {wireTypeDataList.Count} wire type data entries");
    }
    
    void OnDestroy()
    {
        // Cleanup RenderTextures to prevent memory leaks
        if (wireIconDict != null)
        {
            foreach (var rt in wireIconDict.Values)
            {
                if (rt != null)
                {
                    rt.Release();
                    Destroy(rt);
                }
            }
        }
    }
}