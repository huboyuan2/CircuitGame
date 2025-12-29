using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class BuildPanelView : MonoBehaviour
{
    public GameObject ButtonObj;
    public Transform ButtonParent;

    private void Awake()
    {
        if (ButtonParent == null)
            ButtonParent = this.transform.Find("Image");
        WireRenderer.OnIconsReady += HandleIcons;
    }

    void HandleIcons()
    {
        foreach (var icon in WireRenderer.Instance.wireIconDict)
        {
            var button = Instantiate(ButtonObj, ButtonParent);
            button.GetComponentInChildren<TextMeshProUGUI>().text = icon.Key.ToString();

            var rawImage = button.GetComponent<RawImage>();
            if (rawImage != null && icon.Value is RenderTexture)
            {
                rawImage.texture = icon.Value as RenderTexture;
            }

            // Add drag component
            var dragHandler = button.AddComponent<WireButtonDragHandler>();
            dragHandler.wireType = icon.Key;

            // Click to directly select wire type
            button.GetComponent<Button>()?.onClick.AddListener(() => {
                WirePlacementController.Instance.currentWireType = icon.Key;
                Debug.Log($"Selected wire type: {icon.Key}");
            });
        }
    }

    private void OnDestroy()
    {
        WireRenderer.OnIconsReady -= HandleIcons;
    }
}

/// <summary>
/// Handle drag logic for BuildPanel buttons
/// </summary>
public class WireButtonDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public WireType wireType;

    private GameObject dragPreview;
    private Camera mainCamera;
    private Material previewMaterial;
    private Color originalColor;

    void Start()
    {
        mainCamera = Camera.main;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Get the actual wire prefab from WireRenderer based on wireType
        GameObject wirePrefab = WireRenderer.Instance.GetWirePrefab(wireType);

        if (wirePrefab != null && mainCamera != null)
        {
            // Calculate world position at ground level (y=0)
            Vector3 worldPos = GetGroundPosition(eventData.position);

            // Create drag preview in world space (not as UI child)
            dragPreview = Instantiate(wirePrefab, worldPos, Quaternion.identity);
            dragPreview.name = $"DragPreview_{wireType}";

            // Make preview semi-transparent
            MakeTransparent(dragPreview, 0.6f);

            // Disable colliders to prevent raycast interference
            DisableColliders(dragPreview);
        }

        Debug.Log($"Start dragging wire type: {wireType}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragPreview != null && mainCamera != null)
        {
            // Update position to follow mouse/touch at ground level
            Vector3 worldPos = GetGroundPosition(eventData.position);
            dragPreview.transform.position = worldPos;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Detect release position
        Ray ray = mainCamera.ScreenPointToRay(eventData.position);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            var controller = WirePlacementController.Instance;
            Vector3 pos = hit.point;
            int x = Mathf.FloorToInt(pos.x / controller.gameController.mapRenderer.tileSize) + 1;
            int y = Mathf.FloorToInt(pos.z / controller.gameController.mapRenderer.tileSize) + 1;

            // Try to build
            if (controller.wireSystem.PlaceWire(x, y, wireType, 0))
            {
                Debug.Log($"Placed {wireType} at ({x}, {y}) via drag&drop");
                controller.wireRenderer.RenderWire(x, y, wireType, 0);
                controller.gameController.CheckWinCondition();
            }
        }

        // Cleanup preview
        if (dragPreview != null)
        {
            Destroy(dragPreview);
        }
    }

    /// <summary>
    /// Calculate world position where ray intersects ground plane (y=0)
    /// </summary>
    private Vector3 GetGroundPosition(Vector3 screenPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);

        // Create a plane at y=0 (ground level)
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        float distance;

        if (groundPlane.Raycast(ray, out distance))
        {
            Vector3 worldPoint = ray.GetPoint(distance);
            // Set y to 0.1f to match WireRenderer's placement height
            return new Vector3(worldPoint.x, 0.1f, worldPoint.z);
        }

        // Fallback position if raycast fails
        return new Vector3(0, 0.1f, 0);
    }

    /// <summary>
    /// Make preview object semi-transparent
    /// </summary>
    private void MakeTransparent(GameObject obj, float alpha)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            foreach (Material mat in renderer.materials)
            {
                // Enable transparency
                if (mat.HasProperty("_Color"))
                {
                    Color color = mat.color;
                    color.a = alpha;
                    mat.color = color;
                }

                // Set rendering mode to transparent if using standard shader
                mat.SetFloat("_Mode", 3); // Transparent mode
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
            }
        }
    }

    /// <summary>
    /// Disable all colliders to prevent raycast interference
    /// </summary>
    private void DisableColliders(GameObject obj)
    {
        Collider[] colliders = obj.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
    }
}