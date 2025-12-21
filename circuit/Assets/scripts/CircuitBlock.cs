using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class CircuitBlock : MonoBehaviour
{
    public List<bool> wasdBoundaryConnection = new List<bool>();
    public short rotateStatus = 0;
    Transform children;

    // Animation settings
    [SerializeField] private float rotationDuration = 0.3f; // Duration of rotation animation
    [SerializeField] private Ease rotationEase = Ease.OutQuad; // Easing function for smooth rotation
    
    private bool isRotating = false; // Prevent multiple rotations at once
    
    // Start is called before the first frame update
    void Start()
    {
        children = transform.Find("Cube");
    }

    // Update is called once per frame
    void Update()
    {

    }

    void ClockWiseRotete()
    {
        if (isRotating || children == null) return;

        isRotating = true;

        // Perform rotation animation with DOTween
        children.DORotate(children.eulerAngles + new Vector3(0, 90, 0), rotationDuration, RotateMode.FastBeyond360)
            .SetEase(rotationEase)
            .OnComplete(() =>
            {
                isRotating = false;
                ClockWiseRoteUpdateData();
            });
    }

    void AnticlockWiseRotete()
    {
        if (isRotating || children == null) return;

        isRotating = true;

        // Perform rotation animation with DOTween
        children.DORotate(children.eulerAngles + new Vector3(0, -90, 0), rotationDuration, RotateMode.FastBeyond360)
            .SetEase(rotationEase)
            .OnComplete(() =>
            {
                isRotating = false;
                AnticlockWiseRoteUpdateData();
            });
    }

    void ClockWiseRoteUpdateData()
    {
        rotateStatus = (short)((rotateStatus + 1) % 4);
        if (wasdBoundaryConnection.Count > 0)
        {
            LeftMoveListInPlace(wasdBoundaryConnection);
        }
    }

    void AnticlockWiseRoteUpdateData()
    {
        rotateStatus = (short)((rotateStatus + 3) % 4);
        if (wasdBoundaryConnection.Count > 0)
        {
            RightMoveListInPlace(wasdBoundaryConnection);
        }
    }

    void LeftMoveListInPlace(List<bool> list)
    {
        if (list.Count <= 1) return;
        bool first = list[0];
        for (int i = 0; i < list.Count - 1; i++)
        {
            list[i] = list[i + 1];
        }
        list[list.Count - 1] = first;
    }

    void RightMoveListInPlace(List<bool> list)
    {
        if (list.Count <= 1) return;
        bool last = list[list.Count - 1];

        for (int i = list.Count - 1; i > 0; i--)
        {
            list[i] = list[i - 1];
        }

        list[0] = last;
    }

    // Stop any ongoing rotation (optional utility method)
    void OnDestroy()
    {
        if (children != null)
        {
            children.DOKill();
        }
    }
}