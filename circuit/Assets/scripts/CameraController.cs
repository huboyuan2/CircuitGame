using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine;

public class CameraController : MonoBehaviour
{
    public MapSystem mapSystem;

    public float height = 20f;      // how high the camera is
    public float tiltAngle = 45f;   // downward tilt
    public float rotateY = 45f;     // rotation around Y axis

    void Start()
    {
        PositionCamera();
    }

    void PositionCamera()
    {
        float centerX = mapSystem.width / 2f;
        float centerY = mapSystem.height / 2f;

        Vector3 center = new Vector3(centerX, 0, centerY);

        // Position the camera above the center
        transform.position = center + new Vector3(0, height, -height);

        // Rotate camera
        transform.rotation = Quaternion.Euler(tiltAngle, rotateY, 0);

        // Make sure camera looks at the center
        transform.LookAt(center);
    }
}

