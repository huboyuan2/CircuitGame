using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class ToolKitView : MonoBehaviour
{
    public Button CWRotateBtn;
    public Button CCWRotateBtn;
    public Button DeleteBtn;
    public Button CloseBtn;
    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private Camera mainCamera;

    private void Awake()
    {
        CWRotateBtn.onClick.AddListener(HandleCWRotate);
        CCWRotateBtn.onClick.AddListener(HandleCCWRotate);
        DeleteBtn.onClick.AddListener(HandleDelete);
        CloseBtn.onClick.AddListener(HandleClose);
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
        mainCamera = Camera.main;
    }
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    /// <summary>
    /// Convert world coordinates to screen coordinates and update UI position
    /// </summary>
    /// <param name="worldPosition">World coordinates</param>
    public void TrackLocation(Vector3 worldPosition)
    {
        if (rectTransform == null || mainCamera == null)
            return;

        // Convert world coordinates to screen coordinates
        Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);

        // Handle differently based on Canvas render mode
        if (parentCanvas != null)
        {
            if (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                // Overlay mode: Use screen coordinates directly
                rectTransform.position = screenPosition;
            }
            else if (parentCanvas.renderMode == RenderMode.ScreenSpaceCamera ||
                     parentCanvas.renderMode == RenderMode.WorldSpace)
            {
                // Camera mode: Need to convert to Canvas local coordinates
                Vector2 localPosition;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentCanvas.transform as RectTransform,
                    screenPosition,
                    parentCanvas.worldCamera,
                    out localPosition);

                rectTransform.localPosition = localPosition;
            }
        }
        else
        {
            // If no Canvas, use screen coordinates directly
            rectTransform.position = screenPosition;
        }
    }

    void HandleCWRotate()
    {
        WirePlacementController.Instance.TryRotateWireAtMouse(true);
    }
    void HandleCCWRotate()
    {
        WirePlacementController.Instance.TryRotateWireAtMouse(false);
    }
    void HandleDelete()
    {
        WirePlacementController.Instance.TryDeleteWireAtMouse();
    }
    void HandleClose()
    {
        //WirePlacementController.Instance.CloseToolKit();
        this.gameObject.SetActive(false);
    }
    private void OnDestroy()
    {
        
    }
}