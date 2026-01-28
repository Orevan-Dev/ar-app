using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using TMPro;
using System.IO;
using UnityEngine.Networking;
using Vuforia; // Essential for TrackableBehaviour

public class OrevanARMarker : DefaultTrackableEventHandler
{
    public string CampName;

    public VideoPlayer videoPlayer;
    public GameObject TapText;
    public GameObject Loading;
    public GameObject ClickToPlay;
    public GameObject gameModelPrefab;


    public string url_web;
    public string videoName;

    //private string localFilePath;

    //public GoogleAnalyticsV4 googleAnalytics;

    private bool isTracked;
    public bool IsCurrentlyTracked => isTracked;

    // 🧠 FIX: Check real Vuforia status because isTracked gets reset manually
    public bool IsVuforiaTracked
    {
        get
        {
            var tb = GetComponent<TrackableBehaviour>();
            if (tb == null) return false;

            // 1. If Vuforia says "TRACKED", it is definitely visible.
            if (tb.CurrentStatus == TrackableBehaviour.Status.TRACKED) return true;

            // 2. If "EXTENDED_TRACKED", it means Vuforia knows the position but maybe not the image.
            // On iOS, this happens often. We MUST trust it, BUT...
            if (tb.CurrentStatus == TrackableBehaviour.Status.EXTENDED_TRACKED)
            {
                // ...only if the target is actually ON SCREEN. 
                // This prevents the "Click to Play" from popping up when looking away.
                return IsInMainCameraView(transform.position);
            }

            return false;
        }
    }

    private bool IsInMainCameraView(Vector3 worldPos)
    {
        Camera cam = Camera.main;
        if (cam == null) return false;

        Vector3 vp = cam.WorldToViewportPoint(worldPos);
        // 🧠 RELAXED CHECK: Use a small margin (-0.1 to 1.1) to catch targets 
        // that are partially on screen.
        return vp.z > 0 && 
               vp.x > -0.1f && vp.x < 1.1f && 
               vp.y > -0.1f && vp.y < 1.1f;
    }
    
    // 🧠 FIX: Allow external reset to force "Lost" state logic
    public void ForceReset()
    {
        Debug.Log($"🔄 forcing reset on {name}");
        isTracked = false;
        ScanCount = 0;
        
        if (TapText) TapText.SetActive(false);
        if (ClickToPlay) ClickToPlay.SetActive(false);
        if (Loading) Loading.SetActive(false);
        
        StopAllCoroutines();
    }

    public void SimulateTrackingFound()
    {
        Debug.Log($"🔄 Simulating Scan for {name} (iOS Fix)");
        OnTrackingFound();
    }

    private int ScanCount = 0;

    bool one_click = false;
    bool timer_running;
    float timer_for_double_click;

    //this is how long in seconds to allow for a double click
    float delay;

    protected override void OnTrackingFound()
    {
        // 🧠 STRICT CONTROL: If the game is already in session, this marker handler 
        // MUST NOT interfere or show UI.
        if (GameModeManager.Instance != null &&
           (GameModeManager.Instance.CurrentState == GameState.Playing ||
            GameModeManager.Instance.CurrentState == GameState.ReadyToPlay))
        {
            Debug.Log($"🛑 OrevanARMarker ignored OnTrackingFound because GameState is {GameModeManager.Instance.CurrentState}");
            return;
        }

        //googleAnalytics.LogScreen(CampName);
        if (videoName != null && videoName.Contains("VerfyTU01"))
        {
            // For Game Target, we WANT to show the Click To Play UI.
            // The previous logic hid it. We are removing that suppression.
            // TapText.SetActive(false);
            // ClickToPlay.SetActive(false);
            // return;
        }
        PointCameraGuide.instance.OnMarkerFound();
        isTracked = true;
        TapText.GetComponent<TextMeshProUGUI>().text = "Content Found, Tap to Play!";
        ScanCount = 0;
        TapText.gameObject.SetActive(true);
        ClickToPlay.SetActive(true);

    }

    protected override void OnTrackingLost()
    {
        if (videoName != null && videoName.Contains("VerfyTU01"))
            return;
        base.OnTrackingLost();
        StopAllCoroutines();
        PointCameraGuide.instance.OnMarkerLost();
        isTracked = false;

        TapText.gameObject.SetActive(false);
        ClickToPlay.SetActive(false);
        if (Loading)
            Loading.SetActive(false);

        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
        }
    }

    public void Update()
    {
        // 🧠 iOS RECOVERY FIX: 
        // If Vuforia thinks we are tracking but our internal 'isTracked' is false 
        // (meaning we reset for a new game), we force the UI to show IF it's in view.
        if (!isTracked && IsVuforiaTracked)
        {
            if (GameModeManager.Instance != null && 
               (GameModeManager.Instance.CurrentState == GameState.None || 
                GameModeManager.Instance.CurrentState == GameState.Scanned))
            {
                Debug.Log($"📱 [Update Recovery] Marker {name} found in view. Re-triggering UI.");
                OnTrackingFound();
            }
        }

        if (isTracked)
        {
            // 🧠 FIX: Do not process clicks if game is running
            if (GameModeManager.Instance != null &&
               (GameModeManager.Instance.CurrentState == GameState.Playing ||
                GameModeManager.Instance.CurrentState == GameState.ReadyToPlay))
            {
                // Silence tracking while playing
                return;
            }

            // 🧠 FIX: Do not listen to clicks if clicking on UI (like End Game Button)
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            // if (videoName == "VerfyTU01") return; // REMOVED to allow click detection
            if (Input.GetMouseButtonDown(0))
            {
                if (videoName != null && videoName.Contains("VerfyTU01"))
                {
                    // 🧠 GAME FLOW FIX: Intercept click for Game Target
                    Debug.Log("🛑 MARKER CLICKED - GAME TARGET");
                    GameModeManager.Instance.OnUserClickedPlay();
                    return; // Stop here. Do NOT run video logic.
                }

                isTracked = false;
                //TapText.gameObject.SetActive(false);
                ClickToPlay.SetActive(false);

                /*
                localFilePath = Path.Combine(Application.persistentDataPath, videoName);

                // Check if the video file exists locally
                if (File.Exists(localFilePath))
                {
                    // Play the video from local storage
                    //PlayVideo(localFilePath);
                    StartCoroutine("StartScan");
                }
                else
                {
                    // Download and play the video
                    StartCoroutine(DownloadAndPlayVideo());
                }
                */
                SendPostRequest();

                StartCoroutine("StartScan");
            }
        }
    }
    /*
    IEnumerator DownloadAndPlayVideo()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url_web + videoName))
        {
            // Start the download
            TapText.GetComponent<TextMeshProUGUI>().text = "Downlaoding video....";
            yield return request.SendWebRequest();

            // Check for download errors
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to download video: " + request.error);
                TapText.GetComponent<TextMeshProUGUI>().text = "Failed to download video....";
            }
            else
            {
                // Save the downloaded video to local storage
                File.WriteAllBytes(localFilePath, request.downloadHandler.data);

                // Play the video
                //PlayVideo(localFilePath);
                StartCoroutine("StartScan");
            }
        }
    }


    */
    //IEnumerator StartScan()
    //{
    //    TapText.GetComponent<TextMeshProUGUI>().text = "Preparing Video....";
    //    //videoPlayer.url = "file://" + localFilePath;
    //    //videoPlayer.url = localFilePath;
    //    //yield return new WaitUntil(() => File.Exists(localFilePath));

    //    if (videoPlayer != null)
    //    {
    //        videoPlayer.url = url_web + videoName;
    //        videoPlayer.Prepare();
    //        yield return new WaitForSeconds(2f);

    //        base.OnTrackingFound();
    //        TapText.gameObject.SetActive(false);
    //        videoPlayer.Play();
    //    }
    //    else
    //    {
    //        // No video player found — this is likely a 3D model target
    //        Debug.Log("No VideoPlayer attached — skipping video playback.");
    //        base.OnTrackingFound();
    //    }

    //    //videoPlayer.url = url_web + videoName;

    //    //videoPlayer.Prepare();
    //    //yield return new WaitForSeconds(2f);
    //    ////if (videoPlayer.isPrepared)
    //    ////{

    //    //    base.OnTrackingFound();
    //    //    TapText.gameObject.SetActive(false);
    //    //    videoPlayer.Play();
    //    //}
    //    //else
    //    //{
    //    //    Debug.LogError(ScanCount);
    //    //    ScanCount++;
    //    //    Debug.LogError(ScanCount);

    //    //    if (ScanCount > 7)
    //    //    {
    //    //        TapText.SetActive(true);
    //    //        TapText.GetComponent<TextMeshProUGUI>().text = "Error in buffering, please try again!";
    //    //    }
    //    //    else
    //    //    {
    //    //        StartCoroutine("StartScan");
    //    //    }
    //    //}

    //}

    IEnumerator StartScan()
    {
        TapText.GetComponent<TextMeshProUGUI>().text = "Preparing Content....";

        // 🧠 Case 1: this is your 3D model marker
        // if (CampName == "VerfyTU01") // 👈 change this to your actual target name
        {
            Debug.Log("3D Model Target detected — showing model only.");

            // Stop video completely if it was set up
            if (videoPlayer != null)
            {
                if (videoPlayer.isPlaying)
                    videoPlayer.Stop();
                videoPlayer.gameObject.SetActive(false); // hide the video plane
            }

            // Hide UI
            TapText.gameObject.SetActive(false);
            if (Loading) Loading.SetActive(false);
            if (ClickToPlay) ClickToPlay.SetActive(false);

            // ✅ Spawn the 3D model and play its animation
            if (gameModelPrefab != null)
            {
                var modelInstance = Instantiate(gameModelPrefab, transform);
                modelInstance.transform.localPosition = Vector3.zero;
                modelInstance.transform.localRotation = Quaternion.identity;
                modelInstance.transform.localScale = Vector3.one;

                // ▶️ Play animation automatically
                //Animator animator = modelInstance.GetComponent<Animator>();
                var animator = modelInstance.GetComponentInChildren<Animator>();

                if (animator != null)
                {
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    animator.speed = 1f;
                    animator.Play(0);
                    Debug.Log("Animator found & playing.");
                }
                else
                {
                    Debug.LogWarning("No Animator found on spawned model.");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ No 3D model prefab assigned!");
            }

            // End here — skip video logic
            yield break;
        }

        // 🧠 Case 2: all other markers → play video
        if (videoPlayer != null)
        {
            videoPlayer.url = url_web + videoName;
            videoPlayer.Prepare();
            yield return new WaitForSeconds(2f);

            base.OnTrackingFound();
            TapText.gameObject.SetActive(false);
            videoPlayer.Play();
        }
        else
        {
            Debug.Log("No VideoPlayer attached — skipping video playback.");
            base.OnTrackingFound();
        }
    }

    public void OnFullScreen()
    {
        videoPlayer.Pause();
        StartCoroutine(PlayVideoCoroutine(videoPlayer.url));
    }

    private IEnumerator PlayVideoCoroutine(string videoPath)
    {
        //var spinnerPanel = SpinnerPanel.Instance();
        //spinnerPanel.Show();
        Debug.Log("Play Full Movie");
        if (Loading)
            Loading.SetActive(true);
        yield return new WaitForSeconds(0.1f);

        Handheld.PlayFullScreenMovie(
        videoPath,
        Color.black,
        FullScreenMovieControlMode.Minimal,
        FullScreenMovieScalingMode.AspectFit);

        //yield return new WaitForSeconds(0.1f);
        //Loading.SetActive(false);
    }

    public void OnVideoInfo(string url)
    {
        Application.OpenURL(url);
    }

    public void OnApplicationQuit()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
        }
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
        }
    }


    public void SendPostRequest()
    {
        string url = "https://www.orevan.org/orevanar/location";
        // Parameters
        string file = videoName;
        string lat = GPSLocation.Lat;
        string lng = GPSLocation.Long;

        // Construct the full URL with query parameters
        string fullUrl = $"{url}?file={file}&lat={lat}&lng={lng}";

        Debug.Log("Locatoion: " + lat + ", " + lng);

        // Start the POST request coroutine
        if (lat.Length > 0)
            StartCoroutine(PostRequestCoroutine(fullUrl));
    }

    private IEnumerator PostRequestCoroutine(string fullUrl)
    {
        using (UnityWebRequest request = UnityWebRequest.PostWwwForm(fullUrl, ""))
        {
            // Set headers if needed
            request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

            // Send the request
            yield return request.SendWebRequest();

            // Handle the response
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Request successful: " + request.downloadHandler.text);
            }
            else
            {
                Debug.LogError("Request failed: " + request.error);
            }
        }
    }


}
