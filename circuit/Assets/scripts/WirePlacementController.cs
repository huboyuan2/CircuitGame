using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WirePlacementController : MonoBehaviour
{
    public Camera mainCamera;
    public GameController gameController;
    public WireSystem wireSystem;
    public WireRenderer wireRenderer;

    public WireType currentWireType = WireType.Normal;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryPlaceWireAtMouse();
        }
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
    }

    void TryPlaceWireAtMouse()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f))
        {
            Debug.Log($"Raycast HIT: {hit.collider.gameObject.name} at {hit.point}");
            // Assuming your tiles are at y = 0 and spaced evenly
            Vector3 pos = hit.point;

            int x = Mathf.FloorToInt(pos.x / gameController.mapRenderer.tileSize)+1;
            int y = Mathf.FloorToInt(pos.z / gameController.mapRenderer.tileSize)+1;

            if (wireSystem.PlaceWire(x, y, currentWireType))
            {
                Debug.Log($"Placed wire of type {currentWireType} at ({x}, {y})");
                // Optional: update visuals here if you have a WireRenderer
                wireRenderer.RenderWire(x, y, currentWireType);
                // Then check win condition
                gameController.CheckWinCondition();
            }
        }
    }
}

