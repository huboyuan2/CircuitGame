using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildPanelView : MonoBehaviour
{
    public GameObject ButtonObj;
    public Transform ButtonParent;
    //List<Button> buildButtons = new List<Button>();    
    private void Awake()
    {
        if (ButtonParent == null)
            ButtonParent = this.transform.Find("Image"); 
        WireRenderer.OnIconsReady += HandleIcons;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void HandleIcons()
    {
        foreach (var icon in WireRenderer.Instance.wireIconDict)
        {
            var button = Instantiate(ButtonObj, ButtonParent);
            button.GetComponentInChildren<TextMeshProUGUI>().text = icon.Key.ToString();

            var rawImage = button.GetComponent<RawImage>();
            if (rawImage != null && icon.Value is RenderTexture)
            {
                rawImage.texture = icon.Value as RenderTexture;
            }
            //else
            //{
            //    button.GetComponent<Image>().sprite = icon.Value;
            //}
            button.GetComponent<Button>()?.onClick.AddListener(() => {
                WirePlacementController.Instance.currentWireType = icon.Key;
            });
            //buildButtons.Add(button);
        }
    }
    private void OnDestroy()
    {
        WireRenderer.OnIconsReady -= HandleIcons;
    }
}
