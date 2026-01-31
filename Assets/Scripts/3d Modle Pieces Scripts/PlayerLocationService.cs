using UnityEngine;
using System.Collections;

public class PlayerLocationService : MonoBehaviour
{
    public static PlayerLocationService Instance;

    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public bool IsReady { get; private set; }

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    IEnumerator Start()
    {
        if (!Input.location.isEnabledByUser)
        {
            Debug.LogError("[GPS] Location service not enabled by user");
            yield break;
        }

        Input.location.Start(1f, 0.5f);

        while (Input.location.status == LocationServiceStatus.Initializing)
            yield return null;

        if (Input.location.status != LocationServiceStatus.Running)
        {
            Debug.LogError("[GPS] Location service failed");
            yield break;
        }

        IsReady = true;
        Debug.Log("📍 GPS READY");

        while (true)
        {
            Latitude = Input.location.lastData.latitude;
            Longitude = Input.location.lastData.longitude;

            yield return new WaitForSeconds(1f);
        }
    }
}
