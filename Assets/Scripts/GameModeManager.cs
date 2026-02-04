using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Vuforia;
using Managers;

public enum GameState
{
    None,
    Scanned,      // Image found, "Click To Play" visible, Config loading
    ReadyToPlay,  // User clicked "Click To Play", Team UI visible
    Playing,      // Team submitted, Game running
    Ended
}

public class GameModeManager : MonoBehaviour
{
    public static GameModeManager Instance;

    public Canvas scanningUI;
    public Canvas gameUI;
    public Camera arCamera;
    public GameObject teamNamePanel;
    public GameObject confirmEndGamePanel;
    public TMP_InputField input;
    public GameObject teamNameErrorObject;
    public GameObject teamCreationLoader; // Reference to loading spinner/overlay
    private Coroutine errorToastCoroutine; // Track the running coroutine
    public bool isGameRunning = false; // Legacy flag, kept for compatibility
    public GameObject endGameButton;
    public Transform worldAnchor;
    public bool isTrackingStable;

    public GameState CurrentState { get; private set; } = GameState.None;
    private bool userClickedPlay = false;

    private void Awake()
    {
        Instance = this;
        if (arCamera == null)
        {
            arCamera = Camera.main;
            Debug.Log(arCamera != null ? "🎥 Auto-assigned AR Camera" : "⚠️ Camera.main not found!");
        }

        // Ensure FirebaseInitializer exists
        if (FindObjectOfType<FirebaseInitializer>() == null)
        {
            GameObject fi = new GameObject("FirebaseInitializer");
            fi.AddComponent<FirebaseInitializer>();
            Debug.Log("Created FirebaseInitializer automatically.");
        }

        // Ensure TeamManager exists
        if (FindObjectOfType<TeamManager>() == null)
        {
            GameObject tm = new GameObject("TeamManager");
            tm.AddComponent<TeamManager>();
            Debug.Log("Created TeamManager automatically.");
        }

        // Ensure GameEndManager exists (Phase 2)
        if (FindObjectOfType<Managers.GameEndManager>() == null)
        {
            GameObject gem = new GameObject("GameEndManager");
            gem.AddComponent<Managers.GameEndManager>();
            Debug.Log("Created GameEndManager automatically.");
        }
    }

    public void OnTargetScanned()
    {
        // 🧠 STRICT RULE: Only accept scan if we are in None state (fresh start)
        // or already Scanned (updates).
        // If we are ReadyToPlay, Playing, or Ended, IGNORE.
        if (CurrentState != GameState.None && CurrentState != GameState.Scanned)
        {
             Debug.Log($"👁 IGNORING SCAN - GameState is {CurrentState}");
             return;
        }

        if (CurrentState == GameState.Scanned) return; // Already there

        SetState(GameState.Scanned);
        Debug.Log("👁 TARGET SCANNED — State: SCANNED. Waiting for user input.");
    }

    public void OnUserClickedPlay()
    {
        Debug.Log("👆 USER CLICKED PLAY");
        userClickedPlay = true;

        if (GameRoomConfigManager.Instance.IsReady)
        {
            SetState(GameState.ReadyToPlay);
        }
        else
        {
            Debug.Log("⏳ Config not ready yet, showing loading...");
            // Optionally show a loading spinner here if "Click To Play" hides immediately
            // For now, we assume "Click To Play" stays or we show a "Loading..." text
            // If OrevanARMarker hides "Click To Play", we might need a Loading UI
        }
    }

    public void OnConfigReady()
    {
        Debug.Log($"🧩 CONFIG READY. UserClickedPlay: {userClickedPlay}");

        if (userClickedPlay && CurrentState == GameState.Scanned)
        {
            SetState(GameState.ReadyToPlay);
        }
    }

    private void SetState(GameState newState)
    {
        CurrentState = newState;
        Debug.Log($"🔄 STATE CHANGE: {newState}");

        switch (newState)
        {
            case GameState.None:
                // Full Reset Look
                if (scanningUI) scanningUI.gameObject.SetActive(true);
                if (gameUI) gameUI.gameObject.SetActive(false);
                if (teamNamePanel) teamNamePanel.SetActive(false);
                if (input != null) input.gameObject.SetActive(false); // 🧠 iOS UI FIX: Prevent constraint errors
                if (CollectionUI.Instance != null) CollectionUI.Instance.Hide(); // Hide tracker
                HideMarkerUIs(); // Hide any floating "Tap text" until found again
                break;

            case GameState.Scanned:
                scanningUI.gameObject.SetActive(true); // Keep scanning UI/overlay active if needed
                gameUI.gameObject.SetActive(false);
                teamNamePanel.SetActive(false);
                if (input != null) input.gameObject.SetActive(false); // 🧠 iOS UI FIX
                break;

            case GameState.ReadyToPlay:
                scanningUI.gameObject.SetActive(false);
                // Hide legacy marker UIs
                HideMarkerUIs();
                
                // 🧠 FIX: gameUI contains TeamNamePanel, so it MUST be active
                gameUI.gameObject.SetActive(true);
                teamNamePanel.SetActive(true);
                if (input != null) input.gameObject.SetActive(true); // 🧠 iOS UI FIX: Enable only when needed
                
                // Ensure other game HUD elements are hidden
                if (endGameButton != null) endGameButton.SetActive(false);
                if (confirmEndGamePanel != null) confirmEndGamePanel.SetActive(false);
                break;

            case GameState.Playing:
                if (CollectionUI.Instance != null) CollectionUI.Instance.Show(); // Show tracker
                StartGameMode(); // execute actual game start logic
                break;
                
             case GameState.Ended:
                if (CollectionUI.Instance != null) CollectionUI.Instance.Hide(); // Hide tracker
                EndGameMode();
                break;
        }
    }
    
    private void HideMarkerUIs()
    {
         var markers = FindObjectsOfType<OrevanARMarker>();
         foreach (var m in markers)
         {
             if (m.ClickToPlay) m.ClickToPlay.SetActive(false);
             if (m.TapText) m.TapText.SetActive(false);
             if (m.Loading) m.Loading.SetActive(false);
         }
    }


    public void StartGameMode()
    {
        Debug.Log($"🚀 StartGameMode CALLED. CurrentState: {CurrentState}");
        // Debug.Log(System.Environment.StackTrace); // Trace who called me

        // 🧠 STRICT CHECK: Only allow game start if we are effectively in Playing state
        // (or transitioning to it).
        if (CurrentState != GameState.Playing)
        {
            Debug.LogError($"❌ StartGameMode attempted but State is {CurrentState}. Aborting auto-start.");
            return;
        }

        if (isGameRunning) return;
        
        // 🧠 STEP 5 — wait for Firebase config
        if (!GameRoomConfigManager.Instance.IsReady)
        {
            Debug.Log("⏳ Waiting for room config... (This should not happen if flow is correct)");
            return;
        }

        // 🧠 NEW: Ensure GameSession has items. If not, we wait for OnDataLoaded to trigger the spawn.
        if (GameSession.Instance.TotalItems == 0)
        {
            Debug.LogWarning("⏳ Items not loaded yet. Game will start, but pieces will spawn once data arrives.");
        }

        isGameRunning = true;

        // 🧠 FIX: Re-Anchor FIRST, so items spawn around the player's new position.
        if (arCamera != null && PlayAreaAnchor.Instance != null)
        {
            Vector3 cameraPos = arCamera.transform.position;
            Vector3 currentAnchorPos = PlayAreaAnchor.Instance.transform.position;
            Vector3 newAnchorPos = new Vector3(cameraPos.x, currentAnchorPos.y, cameraPos.z);
            
            // 🧠 DEBUG PARENTING
            Debug.Log($"⚓ Anchor Parent BEFORE: {PlayAreaAnchor.Instance.transform.parent?.name ?? "null"} | Scale: {PlayAreaAnchor.Instance.transform.localScale}");

            // 🧠 DETACH: Ensure it's not a child of the Image Target anymore
            PlayAreaAnchor.Instance.transform.SetParent(null); 
            
            // 🧠 FIX SCALE: Force scale to 1,1,1 to ensure meters = meters.
            PlayAreaAnchor.Instance.transform.localScale = Vector3.one;
            
            Debug.Log($"⚓ Anchor Parent AFTER: {PlayAreaAnchor.Instance.transform.parent?.name ?? "null"} | Scale: {PlayAreaAnchor.Instance.transform.localScale}");

            PlayAreaAnchor.Instance.SetAnchor(newAnchorPos);
            Debug.Log($"📍 Re-Anchored Play Area to Player: {newAnchorPos} (Camera was: {cameraPos})");
        }
        else
        {
            Debug.LogError($"❌ Re-Anchoring FAILED. ARCamera null? {arCamera == null} Anchor null? {PlayAreaAnchor.Instance == null}");
        }

        MapWorldAnchor.Instance.SetOrigin(
            PlayerLocationService.Instance.Latitude,
            PlayerLocationService.Instance.Longitude
        );

        // Interactables depend on the Anchor position, so spawn triggers after re-anchoring
        if (GameSession.Instance != null)
        {
            GameSession.Instance.SpawnCachedItems();
        }
        else
        {
            Debug.LogError("❌ GameSession instance not found! Cannot spawn items.");
        }
        
        scanningUI.gameObject.SetActive(false);
        // teamNamePanel.SetActive(false); // Managed by SetState now
        gameUI.gameObject.SetActive(true);

        // 🧠 DISABLE TARGET HANDLER: Prevent any re-scams or anchor shifts from the image target
        var targetHandler = FindObjectOfType<GameTargetEventHandler>();
        if (targetHandler != null)
        {
            targetHandler.enabled = false;
        }

        // 🧠 RE-ENABLE ITEM COLLECTOR: In case it was disabled by GameEndManager
        var collector = FindObjectOfType<ItemCollector>();
        if (collector != null)
        {
            collector.enabled = true;
            Debug.Log("[GAME MODE] ItemCollector re-enabled for new session.");
        }

        // 🧠 RESET QUESTION MANAGER: Clear any stuck states
        if (QuestionManager.Instance != null)
        {
            QuestionManager.Instance.ResetManager();
        }

        Debug.Log("[GAME MODE] Started with GPS world origin");
    }

    public void EndGameMode()
    {
        Debug.Log("🛑 GAME MODE: ENDING... Resetting System.");
        
        // 1. Reset Flags
        isGameRunning = false;
        userClickedPlay = false;
        isTrackingStable = false; // Important: Allow new tracking event
        
        // 2. Reset Data
        if (input != null) input.text = "";
        TeamData.TeamName = ""; 
        
        // 3. Reset Spawner
        if (ItemSpawner.Instance != null)
        {
            ItemSpawner.Instance.ResetSpawner();
        }

        // 3b. Reset Session Data (Count 0/6)
        if (GameSession.Instance != null)
        {
            GameSession.Instance.ResetSession();
        }

        // 4. Reset UI via State
        // effectively going back to "None" or "Scanned" but without the target being necessarily there.
        // We set to None, so the user has to find the target again.
        SetState(GameState.None); 
        
        // 4. Reset UI
        // We do this explicitly here to ensure visual confirmation
        if (gameUI != null) gameUI.gameObject.SetActive(false);
        if (scanningUI != null) scanningUI.gameObject.SetActive(true);
        if (teamNamePanel != null) teamNamePanel.SetActive(false);
        
        // Hide marker UIs that might have been left over
        // HideMarkerUIs(); 
        // 🧠 RESET MARKERS: Force them to forget they are tracking.
        var markers = FindObjectsOfType<OrevanARMarker>();
        foreach (var m in markers)
        {
            m.ForceReset();
        }

        // 🧠 RE-ENABLE TARGET HANDLER
        var targetHandler = FindObjectOfType<GameTargetEventHandler>();
        if (targetHandler != null)
        {
            targetHandler.enabled = true;
        }

        // 🧠 SHOW SCAN LINE / CAMERA GUIDE
        if (PointCameraGuide.instance != null)
        {
            PointCameraGuide.instance.ForceShow();
        }

        Debug.Log("🔄 System Reset Complete. Returning to None state.");
        SetState(GameState.None); 

        // 🧠 iOS FIX: If we are still looking at a marker, manually trigger the Scanned state UI
        // since Vuforia won't fire a new OnTrackingFound event if tracking never broke.
        StartCoroutine(CheckForActiveMarkersAfterReset());
    }

    private IEnumerator CheckForActiveMarkersAfterReset()
    {
        // Wait a frame for states to settle
        yield return null; 

        var markers = FindObjectsOfType<OrevanARMarker>();
        foreach (var m in markers)
        {
            if (m.IsVuforiaTracked)
            {
                Debug.Log($"📱 iOS/Stable Tracking: Marker {m.name} is still tracked after reset. Re-triggering UI.");
                OnTargetScanned(); // Set global state
                m.SimulateTrackingFound(); // Manually show marker UI
                break;
            }
        }
    }

    public void OnSubmitTeam()
    {
        Debug.Log("SUBMIT TEAM CLICKED");

        if (input != null)
        {
            string teamNameStr = input.text;

            if (string.IsNullOrWhiteSpace(teamNameStr))
            {
                ShowErrorMessage("");
                return;
            }

            // Show Loader and Disable Input
            if (teamCreationLoader != null) teamCreationLoader.SetActive(true);
            input.interactable = false;

            // 🧠 TIMEOUT LOGIC: If Firestore doesn't respond in 10s, fail gracefully
            Coroutine timeoutCoroutine = StartCoroutine(TeamCreationTimeout(10f));

            // Call TeamManager to create team in Firestore
            TeamManager.Instance.CreateTeam(teamNameStr, (teamId) => {
                if (timeoutCoroutine != null) StopCoroutine(timeoutCoroutine);
                
                // Success: Hide loader, enable input (though we hide panel anyway)
                if (teamCreationLoader != null) teamCreationLoader.SetActive(false);
                input.interactable = true;

                TeamData.TeamName = teamNameStr; // Keep local Reference
                
                if (teamNameErrorObject != null) teamNameErrorObject.SetActive(false);
                if (teamNamePanel != null) teamNamePanel.SetActive(false);
                if (endGameButton != null) endGameButton.SetActive(true);
                
                SetState(GameState.Playing);

            }, (errorMsg) => {
                if (timeoutCoroutine != null) StopCoroutine(timeoutCoroutine);
                HandleTeamCreationFailure(errorMsg);
            });
        }
    }

    private void ShowErrorMessage(string msg)
    {
        if (teamNameErrorObject != null)
        {
            var textComp = teamNameErrorObject.GetComponentInChildren<TextMeshProUGUI>();
            if (textComp != null) textComp.text = msg;

            if (errorToastCoroutine != null) StopCoroutine(errorToastCoroutine);
            teamNameErrorObject.SetActive(true);
            errorToastCoroutine = StartCoroutine(HideErrorToastAfterDelay(3f));
        }
        Debug.LogWarning($"⚠️ {msg}");
    }

    private void HandleTeamCreationFailure(string errorMsg)
    {
        Debug.LogError("Failed to create team: " + errorMsg);
        
        if (teamCreationLoader != null) teamCreationLoader.SetActive(false);
        input.interactable = true;
        
        // ShowErrorMessage("Connection Error. Please try again.");
    }

    private IEnumerator TeamCreationTimeout(float delay)
    {
        yield return new WaitForSeconds(delay);
        Debug.LogWarning("⏱ Team creation timed out.");
        HandleTeamCreationFailure("Timeout");
    }

    private IEnumerator HideErrorToastAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (teamNameErrorObject != null)
        {
            teamNameErrorObject.SetActive(false);
        }
        errorToastCoroutine = null;
    }

    public void OnEndGamePressed()
    {
        confirmEndGamePanel.SetActive(true);
    }

    public void OnCancelEndGame()
    {
        confirmEndGamePanel.SetActive(false);
    }

    public void OnConfirmEndGame()
    {
        confirmEndGamePanel.SetActive(false);
        SetState(GameState.Ended);
    }

}
