using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using GameData;

namespace Managers
{
    public class TeamManager : MonoBehaviour
    {
        public static TeamManager Instance;

        private FirebaseFirestore db;
        private CollectionReference teamsRef;
        private ListenerRegistration winnerListener;
        private ListenerRegistration leaderboardListener;

        public string CurrentTeamId { get; private set; }
        public bool IsGameEnded { get; private set; } = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
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
            Debug.Log("[TeamManager] Initializing Firestore...");
            db = FirebaseFirestore.DefaultInstance;
            teamsRef = db.Collection("teams");
            
            // Listen for any team becoming a winner to stop game globally
            ListenForWinner();
            
            FirebaseInitializer.OnFirebaseInitialized -= InitializeFirestore;
        }

        private void ListenForWinner()
        {
            Query winnerQuery = teamsRef.WhereEqualTo("isWinner", true).Limit(1);
            winnerListener = winnerQuery.Listen(snapshot =>
            {
                if (snapshot.Count > 0)
                {
                    IsGameEnded = true;
                    
                    // Extract winner data safely
                    string winnerTeamId = "Unknown";
                    string winnerName = "Unknown";
                    
                    try 
                    {
                        DocumentSnapshot winnerDoc = snapshot.Documents.First();
                        winnerTeamId = winnerDoc.Id;
                        winnerName = winnerDoc.GetValue<string>("teamName");
                    } 
                    catch (Exception e)
                    {
                        Debug.LogError($"[TeamManager] Error extracting winner data: {e.Message}");
                    }
                    
                    Debug.Log($"🏆 GAME OVER! Winner found: {winnerName} (ID: {winnerTeamId})");
                    
                    // 🎯 PHASE 2: Trigger game end via GameEndManager
                    if (GameEndManager.Instance != null)
                    {
                        GameEndManager.Instance.TriggerGameEnd(winnerTeamId, winnerName);
                    }
                    else
                    {
                        Debug.LogWarning("[TeamManager] GameEndManager not found! Game end UI will not show.");
                    }
                }
                else
                {
                    if (IsGameEnded)
                    {
                        IsGameEnded = false;
                        Debug.Log("🔄 Game Reset! No winners found in database. You can now create teams.");
                    }
                }
            });
        }

        [ContextMenu("Reset Game Data (Delete All Teams)")]
        public void ResetGameData()
        {
            Debug.Log("🧹 Resetting Game Data in Firestore...");
            teamsRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"❌ Failed to fetch teams for reset: {task.Exception}");
                    return;
                }

                var batch = db.StartBatch();
                foreach (DocumentSnapshot doc in task.Result.Documents)
                {
                    batch.Delete(doc.Reference);
                }

                batch.CommitAsync().ContinueWithOnMainThread(batchTask =>
                {
                    if (batchTask.IsFaulted)
                    {
                        Debug.LogError($"❌ Failed to delete teams: {batchTask.Exception}");
                    }
                    else
                    {
                        Debug.Log("✅ Game Data Reset Complete. All teams deleted.");
                        // IsGameEnded will automatically update via the Listener.
                    }
                });
            });
        }

        public void CreateTeam(string teamName, Action<string> onSuccess, Action<string> onFailure)
        {
            if (IsGameEnded)
            {
                onFailure?.Invoke("Game is already over. Use 'TeamManager.Instance.ResetGameData()' to clear the old winner.");
                return;
            }

            DocumentReference newTeamRef = teamsRef.Document();
            TeamModel newTeam = new TeamModel
            {
                TeamName = teamName,
                CollectedItemsCount = 0,
                CreatedAt = Timestamp.GetCurrentTimestamp(),
                IsWinner = false
            };

            newTeamRef.SetAsync(newTeam).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"❌ Failed to create team: {task.Exception}");
                    onFailure?.Invoke(task.Exception?.Message ?? "Unknown Error");
                }
                else
                {
                    CurrentTeamId = newTeamRef.Id;
                    Debug.Log($"✅ Team Created: {teamName} (ID: {CurrentTeamId})");
                    onSuccess?.Invoke(CurrentTeamId);
                }
            });
        }

        public void IncrementCollectedItemCount()
        {
            if (string.IsNullOrEmpty(CurrentTeamId))
            {
                Debug.LogError("❌ Cannot increment count: No Team ID set.");
                return;
            }

            if (IsGameEnded)
            {
                Debug.LogWarning("⚠️ Game ended, ignoring collection.");
                return;
            }

            DocumentReference docRef = teamsRef.Document(CurrentTeamId);

            db.RunTransactionAsync(async transaction =>
            {
                // 1. Read the document
                DocumentSnapshot snapshot = await transaction.GetSnapshotAsync(docRef);
                
                if (!snapshot.Exists) {
                    Debug.LogError("❌ Team document does not exist!");
                    return; 
                }

                TeamModel team = snapshot.ConvertTo<TeamModel>();

                // 2. Logic Check
                // Double check if game ended (optimistic)
                if (team.IsWinner) return; // Already won

                int newCount = team.CollectedItemsCount + 1;
                bool isWinner = newCount >= 6; // Target count from requirements

                // 3. Write updates
                Dictionary<string, object> updates = new Dictionary<string, object>
                {
                    { "collectedItemsCount", newCount }
                };

                if (isWinner)
                {
                    updates.Add("isWinner", true);
                }

                transaction.Update(docRef, updates);

                // If won, we can set local state immediately or wait for listener
            }).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"❌ Transaction failed: {task.Exception}");
                }
                else
                {
                    Debug.Log($"✅ Item Count Updated.");
                }
            });
        }

        public void GetRankedTeams(Action<List<TeamModel>> callbacks, Action<string> onFailure)
        {
            Query query = teamsRef
                .OrderByDescending("collectedItemsCount")
                .OrderBy("createdAt");

            query.GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"❌ Failed to fetch rankings: {task.Exception}");
                    onFailure?.Invoke(task.Exception?.Message);
                }
                else
                {
                    List<TeamModel> rankedList = new List<TeamModel>();
                    foreach (DocumentSnapshot doc in task.Result.Documents)
                    {
                        TeamModel team = doc.ConvertTo<TeamModel>();
                        team.Id = doc.Id;
                        rankedList.Add(team);
                    }
                    callbacks?.Invoke(rankedList);
                }
            });
        }

        /// <summary>
        /// Starts a real-time listener for the leaderboard.
        /// The callback will fire immediately, and then every time any team's score changes.
        /// </summary>
        public void ListenToRankedTeams(Action<List<TeamModel>> onUpdate)
        {
            // Stop previous listener if it exists
            if (leaderboardListener != null) leaderboardListener.Stop();

            Query query = teamsRef
                .OrderByDescending("collectedItemsCount")
                .OrderBy("createdAt");

            leaderboardListener = query.Listen(snapshot =>
            {
                List<TeamModel> rankedList = new List<TeamModel>();
                foreach (DocumentSnapshot doc in snapshot.Documents)
                {
                    TeamModel team = doc.ConvertTo<TeamModel>();
                    team.Id = doc.Id;
                    rankedList.Add(team);
                }
                
                Debug.Log($"[Leaderboard] Received real-time update. Team count: {rankedList.Count}");
                onUpdate?.Invoke(rankedList);
            });
        }

        private void OnDestroy()
        {
            if (winnerListener != null) winnerListener.Stop();
            if (leaderboardListener != null) leaderboardListener.Stop();
        }
    }
}
