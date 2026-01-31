using UnityEngine;

public class RobotPiece : MonoBehaviour
{
    private Transform visual;
    private Vector3 visualStartLocalPos;

    // public float floatAmplitude = 0.1f;
    // public float floatSpeed = 2f;

    void Awake()
    {
        visual = transform.Find("Visual");

        if (visual == null)
        {
            Debug.LogError($"[RobotPiece] Visual child not found on {name}");
            enabled = false;
            return;
        }

        visualStartLocalPos = visual.localPosition;
    }

    void Update()
    {
        // float yOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        // float yOffset = Mathf.Sin(Time.time);
        visual.localPosition = visualStartLocalPos + Vector3.up ;
    }
}
