using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayCastTest : MonoBehaviour
{
    private Camera mainCamera;

    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found!");
        }
    }

    // Update is called once per frame
    void Update()
    {
#if UNITY_ANDROID || UNITY_IOS
        // Touch input logic
        HandleTouchInput();
#else
        // Mouse input logic
        HandleMouseInput();
#endif
    }

    void HandleMouseInput()
    {
        // Detect left mouse button click
        if (Input.GetMouseButtonDown(0))
        {
            PerformRaycast(Input.mousePosition, true);
        }
        // Detect right mouse button click
        else if (Input.GetMouseButtonDown(1))
        {
            PerformRaycast(Input.mousePosition, false);
        }
    }

    void HandleTouchInput()
    {
        // Detect touch input
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                PerformRaycast(touch.position, true);
            }
        }
    }

    void PerformRaycast(Vector3 screenPosition, bool isLeftClickOrTouch)
    {
        if (mainCamera == null) return;

        // Cast a ray from camera to screen click position
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        // Perform raycast detection
        if (Physics.Raycast(ray, out hit))
        {
            // Check if the hit object has a CircuitBlock component
            CircuitBlock circuitBlock = hit.collider.GetComponent<CircuitBlock>();
            
            if (circuitBlock != null)
            {
                if (isLeftClickOrTouch)
                {
                    // Left click or touch: clockwise rotation
                    circuitBlock.SendMessage("ClockWiseRotete");
                }
                else
                {
                    // Right click: counterclockwise rotation
                    circuitBlock.SendMessage("AnticlockWiseRotete");
                }
            }
        }
    }
}
