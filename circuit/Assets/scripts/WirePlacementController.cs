using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EditorMode
{
    Build,  // Build mode: place wires
    Edit    // Edit mode: rotate and swap wires
}

public class WirePlacementController : MonoBehaviour
{
    public Camera mainCamera;
    public GameController gameController;
    public WireSystem wireSystem;
    public WireRenderer wireRenderer;

    [Header("Editor Mode")]
    public EditorMode currentMode = EditorMode.Build;

    [Header("Build Mode Settings")]
    public WireType currentWireType = WireType.Normal;
    public int currentRotation = 0; // Current rotation for placing wires

    [Header("Edit Mode Settings")]
    [SerializeField] private float dragThreshold = 10f;

    // Drag and drop related variables (for Edit mode)
    private GameObject selectedWireObject = null;
    private Vector3 dragOffset;
    private bool isDragging = false;
    private Vector3 originalPosition;
    private Vector3 mouseDownPosition;
    private int selectedGridX = -1;
    private int selectedGridY = -1;

    void Update()
    {
        // Mode switching with Tab key
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleMode();
        }

        // Handle input based on current mode
        if (currentMode == EditorMode.Build)
        {
            HandleBuildMode();
        }
        else
        {
            HandleEditMode();
        }
    }

    void ToggleMode()
    {
        currentMode = (currentMode == EditorMode.Build) ? EditorMode.Edit : EditorMode.Build;
        Debug.Log($"Switched to {currentMode} mode");

        // Reset edit mode state when switching
        if (currentMode == EditorMode.Build)
        {
            ResetDrag();
        }
    }

    #region Build Mode

    void HandleBuildMode()
    {
        // Left click to place wire
        if (Input.GetMouseButtonDown(0))
        {
            TryPlaceWireAtMouse();
        }

        // Switch wire type with +/- keys
        if (Input.GetKeyDown(KeyCode.Equals))
        {
            int nextType = (int)currentWireType + 1;
            int enumLength = System.Enum.GetValues(typeof(WireType)).Length;
            currentWireType = (WireType)(nextType % enumLength);
            Debug.Log($"Switched to wire type: {currentWireType}");
        }
        if (Input.GetKeyDown(KeyCode.Minus))
        {
            int prevType = (int)currentWireType - 1;
            int enumLength = System.Enum.GetValues(typeof(WireType)).Length;
            currentWireType = (WireType)((prevType + enumLength) % enumLength);
            Debug.Log($"Switched to wire type: {currentWireType}");
        }

        // Rotate preview with R key
        if (Input.GetKeyDown(KeyCode.R))
        {
            TryRotateWireAtMouse(clockwise: true);
            //currentRotation = (currentRotation + 1) % 4;
            //Debug.Log($"Current build rotation: {currentRotation * 90}бу");
        }
    }

    void TryPlaceWireAtMouse()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f))
        {
            Vector3 pos = hit.point;

            int x = Mathf.FloorToInt(pos.x / gameController.mapRenderer.tileSize) + 1;
            int y = Mathf.FloorToInt(pos.z / gameController.mapRenderer.tileSize) + 1;

            // Place wire with current rotation
            if (wireSystem.PlaceWire(x, y, currentWireType, currentRotation))
            {
                Debug.Log($"Placed wire of type {currentWireType} at ({x}, {y}) with rotation {currentRotation * 90}бу");

                // Render with rotation
                wireRenderer.RenderWire(x, y, currentWireType, currentRotation);

                gameController.CheckWinCondition();
            }
        }
    }

    #endregion

    #region Edit Mode

    void HandleEditMode()
    {
        // Start detection
        if (Input.GetMouseButtonDown(0))
        {
            StartDragWire(Input.mousePosition);
        }
        // Hold down, check if threshold exceeded
        else if (Input.GetMouseButton(0) && selectedWireObject != null)
        {
            float dragDistance = Vector3.Distance(Input.mousePosition, mouseDownPosition);

            // Only start dragging if movement exceeds threshold
            if (!isDragging && dragDistance > dragThreshold)
            {
                isDragging = true;
                // Optional: add visual feedback (e.g., transparency)
            }

            if (isDragging)
            {
                DragWire(Input.mousePosition);
            }
        }
        // Release mouse
        else if (Input.GetMouseButtonUp(0))
        {
            if (isDragging)
            {
                EndDragWire(Input.mousePosition);
            }
            else if (selectedWireObject != null)
            {
                // No drag, treat as click, perform clockwise rotation
                PerformRotation(selectedGridX, selectedGridY, clockwise: true);
            }
            ResetDrag();
        }
        // Right-click for counter-clockwise rotation
        else if (Input.GetMouseButtonDown(1))
        {
            TryRotateWireAtMouse(clockwise: false);
        }
    }

    void StartDragWire(Vector3 screenPosition)
    {
        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            // Check if hit a wire object
            GameObject hitObject = hit.collider.gameObject;

            // Convert hit position to grid coordinates
            Vector3 pos = hit.point;
            int x = Mathf.FloorToInt(pos.x / gameController.mapRenderer.tileSize) + 1;
            int y = Mathf.FloorToInt(pos.z / gameController.mapRenderer.tileSize) + 1;

            // Check if there's a wire at this position
            WireType wireType = wireSystem.GetWireType(x, y);
            if (wireType != WireType.None)
            {
                selectedWireObject = hitObject;
                selectedGridX = x;
                selectedGridY = y;
                mouseDownPosition = screenPosition;
                originalPosition = hitObject.transform.position;

                // Calculate offset between mouse click position and object center
                dragOffset = hitObject.transform.position - hit.point;
            }
        }
    }

    void DragWire(Vector3 screenPosition)
    {
        if (selectedWireObject == null || mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);

        // Drag on the same Y plane as the original object
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

        // Get source wire info
        WireType sourceWireType = wireSystem.GetWireType(selectedGridX, selectedGridY);
        int sourceRotation = wireSystem.GetRotation(selectedGridX, selectedGridY);

        // Detect release position
        if (Physics.Raycast(ray, out hit))
        {
            Vector3 pos = hit.point;
            int targetX = Mathf.FloorToInt(pos.x / gameController.mapRenderer.tileSize) + 1;
            int targetY = Mathf.FloorToInt(pos.z / gameController.mapRenderer.tileSize) + 1;

            // Check if target is the same as source
            if (targetX == selectedGridX && targetY == selectedGridY)
            {
                // Same position, just restore
                selectedWireObject.transform.position = originalPosition;
                return;
            }

            // Check if target position has a wire
            WireType targetWireType = wireSystem.GetWireType(targetX, targetY);

            if (targetWireType != WireType.None)
            {
                // Target has a wire - perform swap
                if (wireSystem.SwapWires(selectedGridX, selectedGridY, targetX, targetY))
                {
                    Debug.Log($"Swapped wires at ({selectedGridX},{selectedGridY}) with ({targetX},{targetY})");

                    // Update visual positions
                    GameObject targetWireObject = hit.collider.gameObject;

                    // Calculate grid center positions
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

                    // Update renderer's internal reference array
                    wireRenderer.SwapWireObjects(selectedGridX, selectedGridY, targetX, targetY);

                    // Check win condition after swap
                    gameController.CheckWinCondition();
                }
                else
                {
                    // Swap failed, restore original position
                    selectedWireObject.transform.position = originalPosition;
                }
            }
            else
            {
                // Target is empty - try to move wire to empty floor tile
                if (wireSystem.MoveWire(selectedGridX, selectedGridY, targetX, targetY))
                {
                    Debug.Log($"Moved wire from ({selectedGridX},{selectedGridY}) to ({targetX},{targetY})");

                    // Calculate new grid position
                    Vector3 targetGridPos = new Vector3(
                        (targetX - 0.5f) * gameController.mapRenderer.tileSize,
                        0.1f,
                        (targetY - 0.5f) * gameController.mapRenderer.tileSize
                    );

                    // Update visual position
                    selectedWireObject.transform.position = targetGridPos;

                    // Update renderer's internal reference array
                    wireRenderer.MoveWireObject(selectedGridX, selectedGridY, targetX, targetY);

                    // Check win condition after move
                    gameController.CheckWinCondition();
                }
                else
                {
                    // Move failed (not a valid floor tile), restore original position
                    Debug.Log($"Cannot move wire to ({targetX},{targetY}) - not a valid floor tile");
                    selectedWireObject.transform.position = originalPosition;
                }
            }
        }
        else
        {
            // Released to empty area (no raycast hit), restore original position
            selectedWireObject.transform.position = originalPosition;
        }
    }

    void ResetDrag()
    {
        selectedWireObject = null;
        isDragging = false;
        dragOffset = Vector3.zero;
        mouseDownPosition = Vector3.zero;
        selectedGridX = -1;
        selectedGridY = -1;
    }

    void TryRotateWireAtMouse(bool clockwise)
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f))
        {
            Vector3 pos = hit.point;
            int x = Mathf.FloorToInt(pos.x / gameController.mapRenderer.tileSize) + 1;
            int y = Mathf.FloorToInt(pos.z / gameController.mapRenderer.tileSize) + 1;

            PerformRotation(x, y, clockwise);
        }
    }

    void PerformRotation(int x, int y, bool clockwise)
    {
        WireType wireType = wireSystem.GetWireType(x, y);
        if (wireType == WireType.None)
            return;

        // Update rotation in WireSystem (this updates the int array)
        wireSystem.RotateWire(x, y, clockwise);

        // Get new rotation state
        int newRotation = wireSystem.GetRotation(x, y);

        // Re-render wire with new rotation
        wireRenderer.RefreshWire(x, y, wireType, newRotation);

        string direction = clockwise ? "clockwise" : "counter-clockwise";
        Debug.Log($"Rotated wire at ({x},{y}) {direction} to {newRotation * 90}бу");

        // Check win condition after rotation
        gameController.CheckWinCondition();
    }

    #endregion

    void OnGUI()
    {
        // Display current mode in top-left corner
        GUI.Label(new Rect(10, 10, 300, 30), $"Mode: {currentMode} (Press Tab to switch)");

        if (currentMode == EditorMode.Build)
        {
            GUI.Label(new Rect(10, 40, 300, 30), $"Current Wire: {currentWireType} (+/- to change)");
            GUI.Label(new Rect(10, 70, 300, 30), $"Rotation: (Press R to rotate)");
        }
        else
        {
            GUI.Label(new Rect(10, 40, 400, 30), "Left Click: Rotate | Drag: Swap/Move | Right Click: Counter-rotate");
        }
    }
}