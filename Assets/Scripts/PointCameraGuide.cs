using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointCameraGuide : MonoBehaviour {
    public static PointCameraGuide instance;
    public GameObject CamraGuide;

    private bool isVisible;

    void Awake()
    {
        instance = this;
        isVisible = true;
    }

	public void OnMarkerFound()
    {
        if (isVisible)
        {
            isVisible = false;
            CamraGuide.SetActive(false);
        }
    }

    public void OnMarkerLost()
    {
        if (!isVisible)
        {
            isVisible = true;
            CamraGuide.SetActive(true);
        }
    }

    public void ForceShow()
    {
        isVisible = true;
        if (CamraGuide != null) CamraGuide.SetActive(true);
    }
}
