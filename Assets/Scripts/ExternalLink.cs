using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExternalLink : MonoBehaviour
{
    public Button FullcreenBtn;
    public Button LinkButton;
    public TextMeshProUGUI LinkText;
    public GameObject Loading;

    public string link;


    // Start is called before the first frame update
    void Start()
    {
        FullcreenBtn.onClick.AddListener(GetComponentInParent<OrevanARMarker>().OnFullScreen);
        LinkButton.onClick.AddListener(() => GetComponentInParent<OrevanARMarker>().OnVideoInfo(link));
        GetComponentInParent<OrevanARMarker>().Loading = Loading;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
