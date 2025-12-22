using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine;

public class GameController : MonoBehaviour
{
    public MapSystem mapSystem;
    public MapRenderer mapRenderer;

    void Start()
    {
        // Generate or load the map
        mapSystem.GenerateEmptyMap();

        // Render the map visually
        mapRenderer.RenderMap();
    }
}

