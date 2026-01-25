using UnityEngine;

public class PlayAreaAnchor : MonoBehaviour
{
    public static PlayAreaAnchor Instance;

    void Awake()
    {
        Instance = this;
    }

    public void SetAnchor(Vector3 position)
    {
        transform.position = position;
        transform.rotation = Quaternion.identity;
    }
}
