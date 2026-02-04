using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections.Generic;
using System;
using GameData;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance;
    private FirebaseFirestore db;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Wait for Firebase initialization
        if (FirebaseInitializer.Instance != null && FirebaseInitializer.Instance.IsInitialized)
        {
            InitializeFirestore();
        }
        else
        {
            FirebaseInitializer.OnFirebaseInitialized += InitializeFirestore;
        }
    }

    private void InitializeFirestore()
    {
        Debug.Log("[FirebaseManager] Initializing Firestore...");
        db = FirebaseFirestore.DefaultInstance;
        FirebaseInitializer.OnFirebaseInitialized -= InitializeFirestore;
    }

    /// <summary>
    /// Loads all game items from the 'game_items' collection.
    /// This should be called once at the start of the session.
    /// </summary>
    public void LoadGameItems(Action<List<GameItemData>> onSuccess, Action<string> onFailure)
    {
        Debug.Log("[FirebaseManager] Loading game items...");
        
        db.Collection("game_items").GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                string error = task.Exception != null ? task.Exception.ToString() : "Unknown Error";
                Debug.LogError($"[FirebaseManager] Failed to load items: {error}");
                onFailure?.Invoke(error);
                return;
            }

            QuerySnapshot snapshot = task.Result;
            List<GameItemData> loadedItems = new List<GameItemData>();

            foreach (DocumentSnapshot document in snapshot.Documents)
            {
                Dictionary<string, object> data = document.ToDictionary();
                
                GameItemData newItem = new GameItemData();
                newItem.id = document.Id;
                newItem.name = data.ContainsKey("name") ? data["name"].ToString() : "Unknown";
                newItem.prefabId = data.ContainsKey("prefabId") ? data["prefabId"].ToString() : "";

                if (data.ContainsKey("questions"))
                {
                    List<object> questionsList = data["questions"] as List<object>;
                    if (questionsList != null)
                    {
                        foreach (var qObj in questionsList)
                        {
                            if (qObj is Dictionary<string, object> qDict)
                            {
                                string text = qDict.ContainsKey("text") ? qDict["text"].ToString() : "";
                                // Assume correctAnswer is stored as bool or string "Yes"/"No"
                                bool correct = false;
                                if (qDict.ContainsKey("correctAnswer"))
                                {
                                    object ans = qDict["correctAnswer"];
                                    if (ans is bool b) correct = b;
                                    else if (ans is string s) correct = s.Equals("Yes", StringComparison.OrdinalIgnoreCase) || s.Equals("true", StringComparison.OrdinalIgnoreCase);
                                }
                                
                                newItem.questions.Add(new QuestionData(text, correct));
                            }
                        }
                    }
                }
                
                // IMPORTANT: Only add if valid
                if (newItem.questions.Count == 3)
                {
                    loadedItems.Add(newItem);
                }
                else
                {
                    Debug.LogWarning($"[FirebaseManager] Item {newItem.name} has {newItem.questions.Count} questions instead of 3. Skipping.");
                }
            }

            Debug.Log($"[FirebaseManager] Loaded {loadedItems.Count} items.");
            onSuccess?.Invoke(loadedItems);
        });
    }
}
