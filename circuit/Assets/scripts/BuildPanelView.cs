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
        
        // Subscribe to wire limit changes for UI updates
        WireLimitManager.OnWireCountChanged += OnWireCountChanged;
    }

    void HandleIcons()
    {
        foreach (var icon in WireRenderer.Instance.wireIconDict)
        {
            var button = Instantiate(ButtonObj, ButtonParent);
            
            // Set button text to show wire type and count
            var textMesh = button.GetComponentInChildren<TextMeshProUGUI>();
            if (textMesh != null)
            {
                UpdateButtonText(textMesh, icon.Key);
            }

            var rawImage = button.GetComponent<RawImage>();
            if (rawImage != null && icon.Value is RenderTexture)
            {
                rawImage.texture = icon.Value as RenderTexture;
            }

            // Add drag component
            var dragHandler = button.AddComponent<WireButtonDragHandler>();
            dragHandler.wireType = icon.Key;
            dragHandler.buttonTextMesh = textMesh; // Pass reference for updates

            // Click to directly select wire type
            button.GetComponent<Button>()?.onClick.AddListener(() => {
                WirePlacementController.Instance.currentWireType = icon.Key;
                Debug.Log($"Selected wire type: {icon.Key}");
            });
        }
    }

    private void OnWireCountChanged(WireType wireType, int current, int max)
    {
        // Update UI when wire count changes
        UpdateAllButtonTexts();
    }

    private void UpdateAllButtonTexts()
    {
        // Update all button texts to reflect current counts
        var dragHandlers = GetComponentsInChildren<WireButtonDragHandler>();
        foreach (var handler in dragHandlers)
        {
            if (handler.buttonTextMesh != null)
            {
                UpdateButtonText(handler.buttonTextMesh, handler.wireType);
            }
        }
    }

    private void UpdateButtonText(TextMeshProUGUI textMesh, WireType wireType)
    {
        if (WireLimitManager.Instance != null && WireLimitManager.Instance.HasLimit(wireType))
        {
            int remaining = WireLimitManager.Instance.GetRemainingCount(wireType);
            int max = WireLimitManager.Instance.GetMaxCount(wireType);
            textMesh.text = $"{wireType}\n{remaining}/{max}";
            
            // Optional: Change color if limit reached
            textMesh.color = remaining > 0 ? Color.white : Color.red;
        }
        else
        {
            textMesh.text = wireType.ToString();
        }
    }

    private void OnDestroy()
    {
        WireRenderer.OnIconsReady -= HandleIcons;
        WireLimitManager.OnWireCountChanged -= OnWireCountChanged;
    }
}

/// <summary>
/// Handle drag logic for BuildPanel buttons
/// </summary>
public class WireButtonDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public WireType wireType;
    public TextMeshProUGUI buttonTextMesh; // Reference for updating display

    private GameObject dragPreview;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Set flag to prevent WirePlacementController from processing input
        WirePlacementController.isUIBeingDragged = true;

        // Check if wire limit allows placement
        if (WireLimitManager.Instance != null && !WireLimitManager.Instance.CanPlaceWire(wireType))
        {
            Debug.LogWarning($"Cannot drag {wireType}: limit reached!");
            return;
        }

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
        // Only try to place if preview was created (limit check passed)
        if (dragPreview != null)
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

                // Use the unified placement logic from WirePlacementController
                Debug.Log($"OnEndDrag: Attempting to place {wireType} at ({x}, {y})");
                controller.PlaceWireAtPosition(x, y, wireType, "drag&drop");
            }

            // Cleanup preview
            Destroy(dragPreview);
        }

        // Clear flag to allow WirePlacementController to process input again
        // Use a small delay to ensure the MouseButtonUp event is not processed this frame
        StartCoroutine(ClearUIBeingDraggedFlag());
    }

    private IEnumerator ClearUIBeingDraggedFlag()
    {
        // Wait for end of frame to ensure all input events this frame are processed
        yield return new WaitForEndOfFrame();
        WirePlacementController.isUIBeingDragged = false;
        Debug.Log("Cleared isUIBeingDragged flag");
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
                mat.SetFloat("_ditheralpha", 1.5f);
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