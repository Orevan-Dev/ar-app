using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


public class GPSLocation : MonoBehaviour
{
    //public RTLTextMeshPro text;
    //public GameObject CheckAgain;
    //public GameObject closeBtn;
    private static string _lat = "";
    private static string _long = "";

    public static string Lat
    {
        get 
        {
            if (_lat.Length > 0)
                return _lat;
            else
            {
                _lat = PlayerPrefs.GetString("lat", "");
                return _lat;
            }
        }
    }

    public static string Long
    {
        get {
            if (_long.Length > 0)
                return _long;
            else
            {
                _long = PlayerPrefs.GetString("long", "");
                return _long;
            }
        } 
    }

    private void Start()
    {
        GetLocation();
    }

    public  void GetLocation()
    {
        StartCoroutine(GetLocationByGPS());
        //StartCoroutine(GetLocationWithIP());
    }

    public bool Initialize()
    {
        _lat = GetLat();
        _long = GetLong();

        if (_lat == "null" || _long == "null")
            return false;
        else
            return true;
    }

    public string GetLat()
    {
        if(_lat == null)
        {
            _lat = PlayerPrefs.GetString("lat", "null");
        }

        return _lat;
    }

    public string GetLong()
    {
        if(_long == null)
        {
            _long = PlayerPrefs.GetString("long", "null");
        }

        return _long;
    }

    IEnumerator GetLocationByGPS()
    {
        // First, check if user has location service enabled
        if (!Input.location.isEnabledByUser)
        {
            //StartCoroutine(GetLocationWithIP());
            yield break;
        }



        // Start service before querying location
        Input.location.Start(500);

        // Wait until service initializes
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1f);
            maxWait--;
        }

        // Service didn't initialize in 20 seconds

        if (maxWait < 1)
        {
            //StartCoroutine(GetLocationWithIP());
            yield break;
        }
        // Connection has failed
        if (Input.location.status == LocationServiceStatus.Failed)
        {
           // StartCoroutine(GetLocationWithIP());
            yield break;
        }

        maxWait = 20;
        while (Input.location.status != LocationServiceStatus.Running && maxWait > 0)
        {
            yield return new WaitForSeconds(1f);
            maxWait--;
        }

        // Service didn't initialize in 20 seconds

        if (maxWait < 1)
        {
            //StartCoroutine(GetLocationWithIP());
            yield break;
        }

        else if (Input.location.status == LocationServiceStatus.Running)
        {
            double aa = Input.location.lastData.latitude;
            double oo = Input.location.lastData.longitude;
            Debug.Log("Double Latitude=" + aa + "  Double Longitude: " + oo);

            _lat = (Input.location.lastData.latitude.ToString("R"));
            _long = (Input.location.lastData.longitude.ToString("R"));

            Debug.Log("Lat: " + _lat + "  _long: " + _long);

            SaveLocation();

            //altitudes = double.Parse(Input.location.lastData.altitude.ToString("R"));
            //deviceinfo = SystemInfo.deviceModel;
            // Access granted and location value could be retrieved
            //Locationinformation.text = "Location: " + Input.location.lastData.latitude + " " + Input.location.lastData.longitude + " " + Input.location.lastData.altitude + " " + Input.location.lastData.horizontalAccuracy + " " + Input.location.lastData.timestamp;
            //print("Location: " + Input.location.lastData.latitude + " " + Input.location.lastData.longitude + " " + Input.location.lastData.altitude + " " + Input.location.lastData.horizontalAccuracy + " " + Input.location.lastData.timestamp);
        }
        else
        {
            //StartCoroutine(GetLocationWithIP());
            yield break;
        }


        // Stop service if there is no need to query location updates continuously
        Input.location.Stop();
    }


    IEnumerator GetLocationWithIP()
    {
        string apiUrl = "http://ip-api.com/json/";

        using (UnityWebRequest webRequest = UnityWebRequest.Get(apiUrl))
        {
            // Send the request and wait for a response
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string jsonResult = webRequest.downloadHandler.text;
                //ParseLocationData(jsonResult);
                // Parse JSON data using JSON.NET or a similar library
                JObject json = JObject.Parse(jsonResult);

                string country = json["country"].ToString();
                string city = json["city"].ToString();
                float lat = (float)json["lat"];
                float lon = (float)json["lon"];

                Debug.Log("Country: " + country);
                Debug.Log("City: " + city);
                Debug.Log("Latitude: " + lat);
                Debug.Log("Longitude: " + lon);


                _lat = (lat.ToString("R"));
                _long = (lon.ToString("R"));
                SaveLocation(lat, lon);

                SaveLocation();

            }
            else
            {
                Debug.LogError("Error fetching location data: " + webRequest.error);

                yield break;
            }
        }
    }

    private void SaveLocation()
    {
        PlayerPrefs.SetString("lat", _lat);
        PlayerPrefs.SetString("long", _long);
    }

    private void SaveLocation(float lat, float lon)
    {
        PlayerPrefs.SetString("lat", lat.ToString("R"));
        PlayerPrefs.SetString("long", lon.ToString("R"));
    }
}
