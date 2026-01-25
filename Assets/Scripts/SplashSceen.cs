using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SplashSceen : MonoBehaviour
{
    public GameObject LoadingScene;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnLoadAppMainScene()
    {
        LoadingScene.SetActive(true);
        SceneManager.LoadScene("ARScene-Cloud");
    }
}
