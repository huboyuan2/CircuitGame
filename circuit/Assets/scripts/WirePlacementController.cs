using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class WirePlacementController : MonoBehaviour
{
    public Camera mainCamera;
    public GameController gameController;
    public WireSystem wireSystem;
    public WireRenderer wireRenderer;
    public ToolKitView toolKitView; // Reference to ToolKit UI

    [Header("Build Settings")]
    public WireType currentWireType = WireType.Normal;

    [Header("Selection Settings")]
    [SerializeField] private float clickThreshold = 0.2f; // Click detection time threshold
    [SerializeField] private float dragThreshold = 10f;

    private GameObject selectedWireObject = null;
    private Vector3 dragOffset;
    private bool isDragging = false;
    private Vector3 originalPosition;
    private Vector3 mouseDownPosition;
    private float mouseDownTime;
    private int selectedGridX = -1;
    private int selectedGridY = -1;
    private bool isToolKitActive = false; // ToolKit activation state

    // Static flag to prevent input conflicts with UI drag operations
    public static bool isUIBeingDragged = false;

    private static WirePlacementController _instance;
    public static WirePlacementController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<WirePlacementController>();
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject("WirePlacementController");
                    _instance = singletonObject.AddComponent<WirePlacementController>();
                    DontDestroyOnLoad(singletonObject);
                }
            }
            return _instance;
        }
    }

    void Update()
    {
        HandleInput();
    }

    void HandleInput()
    {
        // Check if clicking on UI or UI is being dragged
        if (IsPointerOverUI() || isUIBeingDragged)
        {
            return; // If clicking on UI or UI drag in progress, skip game logic
        }

        // Unified handling of mouse and touch input
        if (Input.GetMouseButtonDown(0))
        {
            StartInteraction(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0) && selectedWireObject != null)
        {
            float dragDistance = Vector3.Distance(Input.mousePosition, mouseDownPosition);
            float holdTime = Time.time - mouseDownTime;

            // Start dragging when exceeding threshold
            if (!isDragging && dragDistance > dragThreshold && holdTime > clickThreshold)
            {
                isDragging = true;
                HideToolKit(); // Hide ToolKit when dragging
            }

            if (isDragging)
            {
                DragWire(Input.mousePosition);
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            if (isDragging)
            {
                EndDragWire(Input.mousePosition);
            }
            else if (selectedWireObject != null)
            {
                // Short click, treat as selection
                float clickDuration = Time.time - mouseDownTime;
                if (clickDuration < clickThreshold)
                {
                    HandleSelection(selectedGridX, selectedGridY);
                }
            }
            else
            {
                // Click on empty tile, try to build (only when ToolKit is not active)
                if (!isToolKitActive)
                {
                    TryPlaceWireAtMouse(Input.mousePosition);
                }
                else
                {
                    // Close ToolKit when clicking on empty space
                    HideToolKit();
                }
            }
            ResetInteraction();
        }

        // Hotkeys to switch wire type (optional to keep)
        if (Input.GetKeyDown(KeyCode.Equals))
        {
            CycleWireType(1);
        }
        if (Input.GetKeyDown(KeyCode.Minus))
        {
            CycleWireType(-1);
        }

        // ESC key to close ToolKit
        if (Input.GetKeyDown(KeyCode.Escape) && isToolKitActive)
        {
            HideToolKit();
        }
    }

    /// <summary>
    /// Check if mouse/touch is over UI
    /// </summary>
    private bool IsPointerOverUI()
    {
        // Mobile touch detection
        if (Input.touchCount > 0)
        {
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
        }
        // PC mouse detection
        else
        {
            return EventSystem.current.IsPointerOverGameObject();
        }
    }

    void StartInteraction(Vector3 screenPosition)
    {
        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        mouseDownPosition = screenPosition;
        mouseDownTime = Time.time;

        if (Physics.Raycast(ray, out hit))
        {
            Vector3 pos = hit.point;
            int x = Mathf.FloorToInt(pos.x / gameController.mapRenderer.tileSize) + 1;
            int y = Mathf.FloorToInt(pos.z / gameController.mapRenderer.tileSize) + 1;

            WireType wireType = wireSystem.GetWireType(x, y);
            if (wireType != WireType.None)
            {
                // Select existing wire
                selectedWireObject = hit.collider.gameObject;
                selectedGridX = x;
                selectedGridY = y;
                originalPosition = hit.collider.transform.position;
                dragOffset = hit.collider.transform.position - hit.point;
            }
        }
    }

    /// <summary>
    /// Handle selection logic, show ToolKit
    /// </summary>
    void HandleSelection(int x, int y)
    {
        if (wireSystem.GetWireType(x, y) == WireType.None)
            return;

        selectedGridX = x;
        selectedGridY = y;

        // Calculate world coordinates
        Vector3 worldPos = new Vector3(
            (x - 0.5f) * gameController.mapRenderer.tileSize,
            0.1f,
            (y - 0.5f) * gameController.mapRenderer.tileSize
        );

        // Show ToolKit and track position
        ShowToolKit(worldPos);

        Debug.Log($"Selected wire at ({x}, {y})");
    }

    void ShowToolKit(Vector3 worldPosition)
    {
        if (toolKitView == null) return;

        toolKitView.gameObject.SetActive(true);
        toolKitView.TrackLocation(worldPosition);
        isToolKitActive = true; // Mark ToolKit as activated
    }

    void HideToolKit()
    {
        if (toolKitView != null)
        {
            toolKitView.gameObject.SetActive(false);
        }
        isToolKitActive = false; // Mark ToolKit as closed
        //selectedGridX = -1;
        //selectedGridY = -1;
    }

    void TryPlaceWireAtMouse(Vector3 screenPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f))
        {
            Vector3 pos = hit.point;
            int x = Mathf.FloorToInt(pos.x / gameController.mapRenderer.tileSize) + 1;
            int y = Mathf.FloorToInt(pos.z / gameController.mapRenderer.tileSize) + 1;

            // Use the unified placement logic
            PlaceWireAtPosition(x, y, currentWireType, "click");
        }
    }

    /// <summary>
    /// Unified wire placement logic with WireLimit management
    /// </summary>
    /// <param name="x">Grid X coordinate</param>
    /// <param name="y">Grid Y coordinate</param>
    /// <param name="newWireType">Wire type to place</param>
    /// <param name="source">Source of placement (for logging)</param>
    /// <returns>True if placement succeeded</returns>
    public bool PlaceWireAtPosition(int x, int y, WireType newWireType, string source = "")
    {
        // Get existing wire type at this position
        WireType existingWireType = wireSystem.GetWireType(x, y);

        Debug.Log($"[{source}] Attempting to place {newWireType} at ({x}, {y}). Existing: {existingWireType}");

        // Build on empty tile or replace existing wire
        if (existingWireType == WireType.None)
        {
            // Placing on empty tile - just consume
            if (WireLimitManager.Instance != null && !WireLimitManager.Instance.TryConsumeWire(newWireType))
            {
                Debug.LogWarning($"Cannot place {newWireType}: limit reached!");
                return false;
            }

            // Try to build
            if (wireSystem.PlaceWire(x, y, newWireType, 0))
            {
                Debug.Log($"[{source}] Placed wire of type {newWireType} at ({x}, {y})");
                wireRenderer.RenderWire(x, y, newWireType, 0);
                gameController.CheckWinCondition();
                return true;
            }
            else
            {
                // Placement failed, return the wire
                if (WireLimitManager.Instance != null)
                {
                    WireLimitManager.Instance.ReturnWire(newWireType);
                }
                Debug.LogWarning($"[{source}] Failed to place {newWireType} at ({x}, {y})");
                return false;
            }
        }
        else
        {
            // Replacing existing wire
            // First check if we can place the new wire
            if (WireLimitManager.Instance != null && !WireLimitManager.Instance.CanPlaceWire(newWireType))
            {
                Debug.LogWarning($"Cannot place {newWireType}: limit reached!");
                return false;
            }

            // Return the old wire first
            if (WireLimitManager.Instance != null)
            {
                WireLimitManager.Instance.ReturnWire(existingWireType);
                Debug.Log($"[{source}] Returned {existingWireType}");
            }

            // Consume the new wire
            if (WireLimitManager.Instance != null && !WireLimitManager.Instance.TryConsumeWire(newWireType))
            {
                // This shouldn't happen since we checked CanPlaceWire, but handle it anyway
                Debug.LogError($"Failed to consume {newWireType} after returning {existingWireType}");
                // Re-consume the old wire to maintain consistency
                WireLimitManager.Instance.TryConsumeWire(existingWireType);
                return false;
            }

            // Place the new wire
            if (wireSystem.PlaceWire(x, y, newWireType, 0))
            {
                Debug.Log($"[{source}] Replaced {existingWireType} with {newWireType} at ({x}, {y})");
                wireRenderer.RenderWire(x, y, newWireType, 0);
                gameController.CheckWinCondition();
                return true;
            }
            else
            {
                // Placement failed, restore the old wire count
                if (WireLimitManager.Instance != null)
                {
                    WireLimitManager.Instance.ReturnWire(newWireType);
                    WireLimitManager.Instance.TryConsumeWire(existingWireType);
                }
                Debug.LogWarning($"[{source}] Failed to replace at ({x}, {y})");
                return false;
            }
        }
    }

    #region Drag related (keep original logic)

    void DragWire(Vector3 screenPosition)
    {
        if (selectedWireObject == null || mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        Plane dragPlane = new Plane(Vector3.up, originalPosition);
        float distance;

        if (dragPlane.Raycast(ray, out distance))
        {
            Vector3 worldPosition = ray.GetPoint(distance) + dragOffset;
            selectedWireObject.transform.position = worldPosition;
        }
    }

    void EndDragWire(Vector3 screenPosition)
    {
        if (selectedWireObject == null || mainCamera == null)
        {
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        WireType sourceWireType = wireSystem.GetWireType(selectedGridX, selectedGridY);
        int sourceRotation = wireSystem.GetRotation(selectedGridX, selectedGridY);

        if (Physics.Raycast(ray, out hit))
        {
            Vector3 pos = hit.point;
            int targetX = Mathf.FloorToInt(pos.x / gameController.mapRenderer.tileSize) + 1;
            int targetY = Mathf.FloorToInt(pos.z / gameController.mapRenderer.tileSize) + 1;

            if (targetX == selectedGridX && targetY == selectedGridY)
            {
                selectedWireObject.transform.position = originalPosition;
                return;
            }

            WireType targetWireType = wireSystem.GetWireType(targetX, targetY);

            if (targetWireType != WireType.None)
            {
                // Swap wires
                if (wireSystem.SwapWires(selectedGridX, selectedGridY, targetX, targetY))
                {
                    Debug.Log($"Swapped wires at ({selectedGridX},{selectedGridY}) with ({targetX},{targetY})");

                    GameObject targetWireObject = hit.collider.gameObject;
                    Vector3 sourceGridPos = new Vector3(
                        (selectedGridX - 0.5f) * gameController.mapRenderer.tileSize,
                        0.1f,
                        (selectedGridY - 0.5f) * gameController.mapRenderer.tileSize
                    );
                    Vector3 targetGridPos = new Vector3(
                        (targetX - 0.5f) * gameController.mapRenderer.tileSize,
                        0.1f,
                        (targetY - 0.5f) * gameController.mapRenderer.tileSize
                    );

                    selectedWireObject.transform.position = targetGridPos;
                    targetWireObject.transform.position = sourceGridPos;
                    wireRenderer.SwapWireObjects(selectedGridX, selectedGridY, targetX, targetY);
                    gameController.CheckWinCondition();
                }
                else
                {
                    selectedWireObject.transform.position = originalPosition;
                }
            }
            else
            {
                // Move to empty tile
                if (wireSystem.MoveWire(selectedGridX, selectedGridY, targetX, targetY))
                {
                    Debug.Log($"Moved wire from ({selectedGridX},{selectedGridY}) to ({targetX},{targetY})");

                    Vector3 targetGridPos = new Vector3(
                        (targetX - 0.5f) * gameController.mapRenderer.tileSize,
                        0.1f,
                        (targetY - 0.5f) * gameController.mapRenderer.tileSize
                    );

                    selectedWireObject.transform.position = targetGridPos;
                    wireRenderer.MoveWireObject(selectedGridX, selectedGridY, targetX, targetY);
                    gameController.CheckWinCondition();
                }
                else
                {
                    Debug.Log($"Cannot move wire to ({targetX},{targetY}) - not a valid floor tile");
                    selectedWireObject.transform.position = originalPosition;
                }
            }
        }
        else
        {
            selectedWireObject.transform.position = originalPosition;
        }
    }

    void ResetInteraction()
    {
        selectedWireObject = null;
        isDragging = false;
        dragOffset = Vector3.zero;
        mouseDownPosition = Vector3.zero;
        // Note: Do not reset selectedGridX/Y, keep selection state
    }

    #endregion

    #region ToolKit button callbacks

    public void TryRotateWireAtMouse(bool clockwise)
    {
        if (selectedGridX < 0 || selectedGridY < 0)
            return;

        WireType wireType = wireSystem.GetWireType(selectedGridX, selectedGridY);
        if (wireType == WireType.None)
            return;

        wireSystem.RotateWire(selectedGridX, selectedGridY, clockwise);
        int newRotation = wireSystem.GetRotation(selectedGridX, selectedGridY);
        wireRenderer.RefreshWire(selectedGridX, selectedGridY, wireType, newRotation);

        string direction = clockwise ? "clockwise" : "counter-clockwise";
        Debug.Log($"Rotated wire at ({selectedGridX},{selectedGridY}) {direction} to {newRotation * 90}бу");

        gameController.CheckWinCondition();
    }

    public void TryDeleteWireAtMouse()
    {
        if (selectedGridX < 0 || selectedGridY < 0)
            return;

        WireType wireType = wireSystem.GetWireType(selectedGridX, selectedGridY);

        if (wireSystem.PlaceWire(selectedGridX, selectedGridY, WireType.None, 0))
        {
            // Return the wire to available count
            if (WireLimitManager.Instance != null)
            {
                WireLimitManager.Instance.ReturnWire(wireType);
            }

            Debug.Log($"Deleted wire at ({selectedGridX}, {selectedGridY})");
            wireRenderer.RenderWire(selectedGridX, selectedGridY, WireType.None, 0);
            HideToolKit();
            gameController.CheckWinCondition();
        }
    }

    #endregion

    #region Helper methods

    void CycleWireType(int direction)
    {
        int nextType = (int)currentWireType + direction;
        int enumLength = System.Enum.GetValues(typeof(WireType)).Length;
        currentWireType = (WireType)((nextType + enumLength) % enumLength);
        Debug.Log($"Switched to wire type: {currentWireType}");
    }

    #endregion

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 300, 30), $"Current Wire: {currentWireType} (+/- to change)");
        if (selectedGridX >= 0 && selectedGridY >= 0)
        {
            GUI.Label(new Rect(10, 40, 300, 30), $"Selected: ({selectedGridX}, {selectedGridY})");
        }
        if (isToolKitActive)
        {
            GUI.Label(new Rect(10, 70, 300, 30), "ToolKit Active (ESC to close)");
        }
    }
}