using UnityEngine;
using System.Collections;
using Vuforia;



public class GameTargetEventHandler : DefaultTrackableEventHandler
{
    protected override void OnTrackingFound()
    {

        base.OnTrackingFound();

        if (GameModeManager.Instance.isTrackingStable)
            return;

        Debug.Log("🟢 TRACKING FOUND — Initiating Background Load");
        
        // Notify GameModeManager that we have scanned the target
        // This sets state to Scanned, but does NOT start game or show Team UI
        GameModeManager.Instance.OnTargetScanned();

        StartCoroutine(ConfirmStableTracking());
    }


    IEnumerator ConfirmStableTracking()
    {
        yield return new WaitForSeconds(0.5f);

        GameModeManager.Instance.isTrackingStable = true;
        Debug.Log("✅ TRACKING IS NOW STABLE");

        // 🧠 CRITICAL FIX: Do NOT move the anchor if the game is already running!
        // The game uses a custom world anchor (player position) once started.
        // We only want to set anchor to the target during the SETUP phase.
        if (!GameModeManager.Instance.isGameRunning && GameModeManager.Instance.CurrentState != GameState.Playing)
        {
            PlayAreaAnchor.Instance.SetAnchor(transform.position); 
        }


        var trackable = GetComponent<TrackableBehaviour>()
                     ?? GetComponentInParent<TrackableBehaviour>();

        if (trackable == null)
        {
            Debug.LogError("❌ No TrackableBehaviour found");
            yield break;
        }

        string roomId = trackable.TrackableName;
        Debug.Log("🏷 ROOM ID: " + roomId);

        if (GameRoomConfigManager.Instance == null)
        {
            Debug.LogError("❌ GameRoomConfigManager missing");
            yield break;
        }

        // TRIGGER BACKGROUND LOAD ONLY
        GameRoomConfigManager.Instance.LoadRoomConfig(roomId);

    }


    protected override void OnTrackingLost()
    {
        base.OnTrackingLost();

        Debug.Log("🔴 TRACKING LOST");

        GameModeManager.Instance.isTrackingStable = false;

        // ItemSpawner.Instance?.HideAllItems();
    }

}
