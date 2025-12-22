using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayCastTest : MonoBehaviour
{
    private Camera mainCamera;
    
    // Drag and drop related variables
    private CircuitBlock selectedBlock = null;
    private Vector3 dragOffset;
    private bool isDragging = false;
    private Vector3 originalPosition;
    private Vector3 mouseDownPosition; // Record screen position when pressed

    // Drag threshold (in pixels)
    [SerializeField] private float dragThreshold = 10f;

    // Reference to SudoMap
    public SudoMap sudoMap;

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found!");
        }

        // Auto-find SudoMap if not manually assigned
        if (sudoMap == null)
        {
            sudoMap = FindObjectOfType<SudoMap>();
        }
    }

    void Update()
    {
#if UNITY_ANDROID || UNITY_IOS
        HandleTouchInput();
#else
        HandleMouseInput();
#endif
    }

    void HandleMouseInput()
    {
        // Start detection
        if (Input.GetMouseButtonDown(0))
        {
            StartDrag(Input.mousePosition);
        }
        // Hold down, check if threshold exceeded
        else if (Input.GetMouseButton(0) && selectedBlock != null)
        {
            float dragDistance = Vector3.Distance(Input.mousePosition, mouseDownPosition);
            
            // Only start dragging if movement exceeds threshold
            if (!isDragging && dragDistance > dragThreshold)
            {
                isDragging = true;

                //it works, but I dont know 1.5 make it transparent
                selectedBlock?.SetDitherTransparency(1.5f);
            }

            if (isDragging)
            {
                DragBlock(Input.mousePosition);
            }
        }
        // Release mouse
        else if (Input.GetMouseButtonUp(0))
        {
            if (isDragging)
            {
                EndDrag(Input.mousePosition);
            }
            else if (selectedBlock != null)
            {
                // No drag, treat as click, perform rotation
                PerformClick(selectedBlock);
            }
            ResetDrag();
        }
        // Right-click rotation
        else if (Input.GetMouseButtonDown(1))
        {
            PerformRaycast(Input.mousePosition, false);
        }
    }

    void HandleTouchInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            if (touch.phase == TouchPhase.Began)
            {
                StartDrag(touch.position);
            }
            else if (touch.phase == TouchPhase.Moved && selectedBlock != null)
            {
                float dragDistance = Vector3.Distance(touch.position, mouseDownPosition);
                
                if (!isDragging && dragDistance > dragThreshold)
                {
                    isDragging = true;
                }

                if (isDragging)
                {
                    DragBlock(touch.position);
                }
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                if (isDragging)
                {
                    EndDrag(touch.position);
                }
                else if (selectedBlock != null)
                {
                    PerformClick(selectedBlock);
                }
                ResetDrag();
            }
        }
    }

    void StartDrag(Vector3 screenPosition)
    {
        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            CircuitBlock block = hit.collider.GetComponent<CircuitBlock>();
            if (block != null)
            {
                selectedBlock = block;
                
                mouseDownPosition = screenPosition; // Record pressed position
                originalPosition = block.transform.position;
                
                // Calculate offset between mouse click position and object center
                dragOffset = block.transform.position - hit.point;
            }
        }
    }

    void DragBlock(Vector3 screenPosition)
    {
        if (selectedBlock == null || mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        
        // Drag on the same Y plane as the original object
        Plane dragPlane = new Plane(Vector3.up, originalPosition);
        float distance;
        
        if (dragPlane.Raycast(ray, out distance))
        {
            Vector3 worldPosition = ray.GetPoint(distance) + dragOffset;
            selectedBlock.transform.position = worldPosition;
        }
    }

    void EndDrag(Vector3 screenPosition)
    {
        if (selectedBlock == null || mainCamera == null)
        {
            return;
        }
        selectedBlock.SetDitherTransparency(1.0f);
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        // Detect if there's another CircuitBlock at release position
        if (Physics.Raycast(ray, out hit))
        {
            CircuitBlock targetBlock = hit.collider.GetComponent<CircuitBlock>();
            
            if (targetBlock != null && targetBlock != selectedBlock)
            {
                // Perform swap, pass in originalPosition
                if (sudoMap != null && sudoMap.TrySwapBlocks(selectedBlock, targetBlock, originalPosition))
                {
                    Debug.Log($"Swapped {selectedBlock.name} with {targetBlock.name}");
                }
                else
                {
                    // Swap failed, restore original position
                    selectedBlock.transform.position = originalPosition;
                }
            }
            else
            {
                // Didn't hit another block, restore original position
                selectedBlock.transform.position = originalPosition;
            }
        }
        else
        {
            // Released to empty area, restore original position
            selectedBlock.transform.position = originalPosition;
        }
    }

    void ResetDrag()
    {
        selectedBlock = null;
        isDragging = false;
        dragOffset = Vector3.zero;
        mouseDownPosition = Vector3.zero;
    }

    // Handle click event (rotation)
    void PerformClick(CircuitBlock block)
    {
        if (block != null)
        {
            block.SendMessage("ClockWiseRotete");
            Debug.Log($"Clicked {block.name} - Rotating clockwise");
        }
    }

    void PerformRaycast(Vector3 screenPosition, bool isLeftClickOrTouch)
    {
        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            CircuitBlock circuitBlock = hit.collider.GetComponent<CircuitBlock>();
            
            if (circuitBlock != null)
            {
                if (isLeftClickOrTouch)
                {
                    circuitBlock.SendMessage("ClockWiseRotete");
                }
                else
                {
                    circuitBlock.SendMessage("AnticlockWiseRotete");
                }
            }
        }
    }
}
