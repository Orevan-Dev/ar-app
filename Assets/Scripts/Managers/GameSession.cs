using UnityEngine;
using System.Collections.Generic;
using GameData;
using Managers;

public class GameSession : MonoBehaviour
{
    public static GameSession Instance;

    public List<GameItemData> AllItems { get; private set; } = new List<GameItemData>();
    
    // Track collected count dynamically
    public int CollectedCount 
    {
        get 
        {
            int count = 0;
            if (AllItems != null)
            {
                foreach (var item in AllItems)
                {
                    if (item.isCollected) count++;
                }
            }
            return count;
        }
    }
    
    public int TotalItems => AllItems != null ? AllItems.Count : 0;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // Removed LoadData() here.
        // It is now triggered by GameRoomConfigManager.cs -> LoadRoomConfig()
        // so it happens in the startup flow alongside room config logic.
    }

    public void LoadData()
    {
        // 🧠 PROTECTION: If we already have items and the game is running,
        // do NOT reload. This prevents a mid-game marker re-scan from 
        // overwriting player progress (the "0/6 reset" bug).
        if (AllItems != null && AllItems.Count > 0)
        {
            if (GameModeManager.Instance != null && GameModeManager.Instance.isGameRunning)
            {
                Debug.Log("[GameSession] Game is running. Ignoring data reload request to protect progress.");
                return;
            }
        }

        Debug.Log("[GameSession] Requesting fresh data from Firebase...");
        FirebaseManager.Instance.LoadGameItems(OnDataLoaded, OnDataLoadFailed);
    }

    private void OnDataLoaded(List<GameItemData> items)
    {
        AllItems = items;
        Debug.Log($"[GameSession] Data cached. Total items: {AllItems.Count}");
        
        // Notify Spawner to spawn these specific items
        // 🧠 CHANGED: Don't spawn immediately. Wait for "StartGameMode"
        // If the game IS already running (rare edge case of reload), we could spawn.
        if (GameModeManager.Instance != null && GameModeManager.Instance.isGameRunning)
        {
             ItemSpawner.Instance.SpawnItems(AllItems);
        }
    }

    public void SpawnCachedItems()
    {
        if (AllItems != null && AllItems.Count > 0)
        {
             ItemSpawner.Instance.SpawnItems(AllItems);
        }
    }

    private void OnDataLoadFailed(string error)
    {
        Debug.LogError($"[GameSession] Data load failed: {error}");
        // Handle retry or error UI
    }

    public GameItemData GetItemData(string id)
    {
        return AllItems.Find(x => x.id == id);
    }
    
    // Event fired whenever an item is successfully collected
    // Parameters: (int currentCount, int totalCount)
    public static event System.Action<int, int> OnItemCollected;

    // Event fired when all items for the session are collected
    public static event System.Action OnAllItemsCollected;

    public void MarkItemCollected(GameItemData item)
    {
        if (item != null)
        {
            item.isCollected = true;
            
            Debug.Log($"[GameSession] Item {item.name} collected. Total: {CollectedCount}/{TotalItems}");

            // Notify listeners (like CollectionUI) via event
            OnItemCollected?.Invoke(CollectedCount, TotalItems);

            // Update Firestore Count
            if (TeamManager.Instance != null)
            {
                TeamManager.Instance.IncrementCollectedItemCount();
            }

            // Check Win Condition
            if (CollectedCount >= TotalItems && TotalItems > 0)
            {
                Debug.Log("🏆 ALL ITEMS COLLECTED! Firing Win Event.");
                OnAllItemsCollected?.Invoke();
            }
        }
    }

    public void ResetSession()
    {
        Debug.Log("[GameSession] Resetting session data.");
        if (AllItems != null)
        {
            foreach (var item in AllItems)
            {
                item.isCollected = false;
            }
        }
        // If you want to reload from Firebase on next scan, you could clear the list entirely
        // AllItems.Clear(); 
    }
}
