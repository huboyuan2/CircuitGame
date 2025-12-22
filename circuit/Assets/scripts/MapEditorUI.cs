using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MapEditorUI : MonoBehaviour
{
    public MapSystem mapSystem;
    public MapRenderer mapRenderer;

    public TMP_InputField fileNameInput;
    public Button saveButton;
    public Button loadButton;

    private string folderPath;

    void Start()
    {
        folderPath = Application.dataPath + "/Maps/";

        // Ensure folder exists
        if (!System.IO.Directory.Exists(folderPath))
            System.IO.Directory.CreateDirectory(folderPath);

        saveButton.onClick.AddListener(SaveMap);
        loadButton.onClick.AddListener(LoadMap);
    }

    void SaveMap()
    {
        string name = fileNameInput.text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogError("File name is empty");
            return;
        }

        string path = folderPath + name + ".json";
        mapSystem.SaveMap(path);
        Debug.Log("Saved map to: " + path);
    }

    void LoadMap()
    {
        string name = fileNameInput.text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogError("File name is empty");
            return;
        }

        string path = folderPath + name + ".json";
        mapSystem.LoadMap(path);

        // Re-render after loading
        mapRenderer.RenderMap();

        Debug.Log("Loaded map from: " + path);
    }
}

