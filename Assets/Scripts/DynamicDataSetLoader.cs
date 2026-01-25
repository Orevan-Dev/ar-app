using UnityEngine;
using System.Collections;

using Vuforia;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;
using SimpleJSON;
using UnityEngine.Video;
using TMPro;
using UnityEngine.UI;

public class DynamicDataSetLoader : MonoBehaviour
{
    public TextMeshProUGUI statusText;
    public GameObject TapText;
    public GameObject ClickToPlay;
    //public GoogleAnalyticsV4 googleAnalytics;
    public TextMeshProUGUI updateAppText;

    // specify these in Unity Inspector
    public GameObject augmentationObject = null;  // you can use teapot or other object
    public GameObject gameAugmentationObject = null;

    public GameObject ExtraLinkCanvas;
    public string dataSetName = "";  //  Assets/StreamingAssets/QCAR/DataSetName

    private string appVersion = "1.1";

    // Use this for initialization
    void Start()
    {
        // Vuforia 5.0 to 6.1
        //VuforiaBehaviour vb = GameObject.FindObjectOfType<VuforiaBehaviour>();
        //vb.RegisterVuforiaStartedCallback(LoadDataSet);

        // Vuforia 6.2+
        //VuforiaARController.Instance.RegisterVuforiaStartedCallback(LoadDataSet);
        StartCoroutine(GetDataSetVersion());

    }

    IEnumerator GetDataSetVersion()
    {
        UnityWebRequest webRequest = UnityWebRequest.Get("https://www.orevan.org/orevanar/orevan.json");
        yield return webRequest.SendWebRequest();

        if (webRequest.isNetworkError)
        {
            Debug.Log("Error: " + webRequest.error);
            LoadVuforia();
        }
        else
        {
            statusText.text = "Wait to Initialize....";

            JSONNode node = JSON.Parse(webRequest.downloadHandler.text);
            Debug.Log("Server Reply: " + webRequest.downloadHandler.text);

            string serverAppVersion = node["version"];
            if (appVersion != serverAppVersion)
            {
                updateAppText.gameObject.SetActive(true);
                yield return new WaitForSeconds(5f);
                updateAppText.gameObject.SetActive(false);
            }

            string storedVersion = PlayerPrefs.GetString("datasetVer", "");
            string serverVersion = node["datasetVer"];

            if (storedVersion == serverVersion)
            {
                LoadVuforia();
            }
            else
            {
                string dataseLink = node["dataseLink"];
                string datasetXML = node["datasetXML"];
                dataseLink = dataseLink.Replace("orevan.net", "orevan.org").Replace("www.orevan.net", "www.orevan.org");
                datasetXML = datasetXML.Replace("orevan.net", "orevan.org").Replace("www.orevan.net", "www.orevan.org");

                string savePath = string.Format("{0}/{1}.json", Application.persistentDataPath, dataSetName);
                System.IO.File.WriteAllText(savePath, webRequest.downloadHandler.text);

                StartCoroutine(DownloadDataSet(dataseLink, datasetXML, serverVersion));
            }
        }
    }

    IEnumerator DownloadDataSet(string url, string xmlURL, string version)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                LoadVuforia();
            }
            else
            {
                string savePath = string.Format("{0}/{1}.dat", Application.persistentDataPath, dataSetName);
                System.IO.File.WriteAllBytes(savePath, www.downloadHandler.data);

                StartCoroutine(DownloadDataSetXML(xmlURL, version));
            }
        }
    }

    IEnumerator DownloadDataSetXML(string url, string version)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                LoadVuforia();
            }
            else
            {
                statusText.text = "Successfully Downloaded....";
                yield return new WaitForSeconds(1f);
                string savePath = string.Format("{0}/{1}.xml", Application.persistentDataPath, dataSetName);
                System.IO.File.WriteAllBytes(savePath, www.downloadHandler.data);
                LoadVuforia();

                Debug.Log("Succeded in downloading the files from Internet");

                PlayerPrefs.SetString("datasetVer", version);
            }
        }
    }

    private void LoadVuforia()
    {
        statusText.text = "Scanning....";
        VuforiaARController.Instance.RegisterVuforiaStartedCallback(LoadDataSet);
    }

    void LoadDataSet()
    {
        ObjectTracker objectTracker = TrackerManager.Instance.GetTracker<ObjectTracker>();

        DataSet dataSet = objectTracker.CreateDataSet();


        Debug.Log("Load Data Set from:    " + Application.persistentDataPath + dataSetName + ".xml");
        //if (dataSet.Load(dataSetName))
        //if (dataSet.Load(Application.dataPath + "/Resources/" + dataSetName, VuforiaUnity.StorageType.STORAGE_ABSOLUTE))
        if (dataSet.Load(Application.persistentDataPath + "/" + dataSetName + ".xml", VuforiaUnity.StorageType.STORAGE_ABSOLUTE))
        {
            objectTracker.Stop();  // stop tracker so that we can add new dataset

            if (!objectTracker.ActivateDataSet(dataSet))
            {
                // Note: ImageTracker cannot have more than 100 total targets activated
                Debug.Log("<color=yellow>Failed to Activate DataSet: " + dataSetName + "</color>");
            }

            if (!objectTracker.Start())
            {
                Debug.Log("<color=yellow>Tracker Failed to Start.</color>");
            }

            int counter = 0;

            string savePath = string.Format("{0}/{1}.json", Application.persistentDataPath, dataSetName);
            string loadedJsonDataString = File.ReadAllText(savePath);

            JSONNode node = JSON.Parse(loadedJsonDataString);
            //Debug.Log("Targets count: " + node["Targets"].Count);

            IEnumerable<TrackableBehaviour> tbs = TrackerManager.Instance.GetStateManager().GetTrackableBehaviours();
            foreach (TrackableBehaviour tb in tbs)
            {
                Debug.Log("testing" + tb.TrackableName);

                Debug.Log("Working on: " + tb.TrackableName + "for getting the 3d model and test it ");
                if (tb.name == "New Game Object")
                {
                    //tb.Trackable.
                    // change generic name to include trackable name
                    //tb.gameObject.name = ++counter + ":DynamicImageTarget-" + tb.TrackableName;
                    tb.gameObject.name = ++counter + ":DynamicImageTarget-" + tb.TrackableName;



                    // add additional script components for trackable
                    //tb.gameObject.AddComponent<DefaultTrackableEventHandler>();
                    OrevanARMarker orevanMarker = tb.gameObject.AddComponent<OrevanARMarker>();
                    tb.gameObject.AddComponent<TurnOffBehaviour>();

                    if (augmentationObject != null && tb.TrackableName != "VerfyTU01")
                    {
                        // create video plane for normal targets only
                        GameObject augmentation = Instantiate(augmentationObject);
                        augmentation.transform.parent = tb.gameObject.transform;
                        augmentation.transform.localPosition = new Vector3(0f, 0.008f, 0f);
                        augmentation.transform.localEulerAngles = new Vector3(90, 0, 0);
                        augmentation.transform.localScale = new Vector3(40, 32, 0.005f);
                        augmentation.SetActive(true);
                    }
                    else
                    {
                        Debug.Log("Skipping video plane for 3D model target: " + tb.TrackableName);
                    }

                    if (gameAugmentationObject != null && tb.TrackableName == "VerfyTU01")
                    {
                        tb.gameObject.AddComponent<GameTargetEventHandler>();
                        Debug.Log("GAME SYSTEM: GameTargetEventHandler added to VerfyTU01");

                    }
                    else
                    {
                        Debug.Log("<color=yellow>Warning: No augmentation object specified for: " + tb.TrackableName + "</color>");
                    }

                    //Initialize Variables
                    orevanMarker.TapText = TapText;
                    orevanMarker.ClickToPlay = ClickToPlay;
                    //orevanMarker.googleAnalytics = googleAnalytics;
                    orevanMarker.videoPlayer = orevanMarker.gameObject.GetComponentInChildren<VideoPlayer>();
                    //orevanMarker.videoPlayer.url = "https://www.orevan.org/orevanar/?file=" + tb.TrackableName + ".mp4";
                    orevanMarker.url_web = "https://www.orevan.org/orevanar/?file=";
                    orevanMarker.videoName = tb.TrackableName + ".mp4";


                    for (int i = 0; i < node["Targets"].Count; i++)
                    {
                        Debug.Log("Check Link: " + tb.TrackableName + " == " + node["Targets"][i]["@name"]);
                        if (tb.TrackableName.CompareTo(node["Targets"][i]["@name"]) == 0)
                        {
                            Debug.Log("Truuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuu");
                            if (node["Targets"][i]["@url"].ToString().Length > 1)
                            {
                                GameObject ExtraLinkObject = (GameObject)GameObject.Instantiate(ExtraLinkCanvas);
                                ExtraLinkObject.transform.parent = tb.gameObject.transform;
                                ExtraLinkObject.GetComponent<ExternalLink>().link = node["Targets"][i]["@url"];
                                ExtraLinkObject.GetComponent<ExternalLink>().LinkText.text = node["Targets"][i]["@txt"];

                            }
                            break;
                        }
                    }
                    string storedVersion = PlayerPrefs.GetString("datasetVer", "");
                    string serverVersion = node["datasetVer"];
                }
            }
            Debug.Log("Everything Loaded successfully");
            foreach (DataSet d in objectTracker.GetActiveDataSets())
            {
                Debug.Log("Active dataset: " + d.Path);
            }

        }
        else
        {
            Debug.LogError("<color=yellow>Failed to load dataset: '" + Application.persistentDataPath + "'</color>");
            Debug.LogError("<color=yellow>Failed to load dataset: '" + dataSetName + "'</color>");
        }
    }
}
