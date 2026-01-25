using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TeamNameUI : MonoBehaviour
{
    public TMP_InputField input;

    public void OnSubmit()
    {
        string name = input.text;
        TeamData.TeamName = name;

        // Close the panel
        gameObject.SetActive(false);

        // Start spawning pieces (هنضيفه لاحقاً)
        Debug.Log("Team Name Entered → " + name);
    }
}
