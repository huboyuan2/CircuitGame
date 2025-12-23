using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public MapSystem mapSystem;
    public MapRenderer mapRenderer;
    public WireSystem wireSystem;

    void Start()
    {
        // 1. Generate or load the map
        //mapSystem.GenerateEmptyMap();
        mapSystem.LoadMap(Application.dataPath + "/Maps/1.json");

        // 2. Render the map visually
        mapRenderer.RenderMap();

        

        // 3. Initialize wire system based on map
        wireSystem.mapSystem = mapSystem; // if not wired via Inspector
        // wireSystem.Awake() will already have run, so make sure mapSystem width/height
        // are already set before play, or move initialization to Start in WireSystem.
    }

    public void CheckWinCondition()
    {
        bool win = wireSystem.CheckWin();
        if (win)
        {
            Debug.Log("YOU WIN!");
            // Later: show UI, go to next level, etc.
        }
    }
}


