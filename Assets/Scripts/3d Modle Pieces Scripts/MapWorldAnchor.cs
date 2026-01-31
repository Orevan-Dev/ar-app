using UnityEngine;

public class MapWorldAnchor : MonoBehaviour
{
    public static MapWorldAnchor Instance;

    public Vector2 originLatLng;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void SetOrigin(double lat, double lng)
    {
        originLatLng = new Vector2((float)lat, (float)lng);
        transform.position = Vector3.zero;

        Debug.Log($"🌍 Map Origin Set: {lat}, {lng}");
    }

    public Vector3 LatLngToWorld(double lat, double lng)
    {
        const float metersPerDegree = 111320f;

        float x = (float)(lng - originLatLng.y) * metersPerDegree;
        float z = (float)(lat - originLatLng.x) * metersPerDegree;

        return new Vector3(x, 0f, z);
    }
}
