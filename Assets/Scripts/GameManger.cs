using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManger : MonoBehaviour {
    public GameObject Menu;
    public GameObject HelpPanel;


    private bool isMenuOpen = false;
	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();
    }

    public void OnMenu()
    {
        if (isMenuOpen)
        {
            isMenuOpen = false;
            Menu.GetComponent<Animator>().SetTrigger("close");
        }
        else
        {
            isMenuOpen = true;
            Menu.GetComponent<Animator>().SetTrigger("open");
        }
    }

    public void OnHelp()
    {

    }

    public void OnHowtoPlay()
    {

    }
}
