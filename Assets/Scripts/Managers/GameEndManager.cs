using UnityEngine;
using System;

namespace Managers
{
    /// <summary>
    /// Manages the global game-end logic for Phase 2.
    /// Listens for winner events from TeamManager and triggers UI/gameplay shutdown.
    /// </summary>
    public class GameEndManager : MonoBehaviour
    {
        public static GameEndManager Instance;

        [Header("Feature Toggle")]
        [Tooltip("If true, losing teams see a 'YOU LOST' panel. If false, only winner sees UI.")]
        public bool enableGlobalLose = true;

        // Global event fired when the game ends
        // Parameter: winnerTeamId (the team that won)
        public static event Action<string> OnGameFinished;

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
            // Subscribe to TeamManager's winner detection
            if (TeamManager.Instance != null)
            {
                // We'll add a callback to TeamManager's existing winner listener
                Debug.Log("[GameEndManager] Ready to listen for game end events.");
            }
        }

        /// <summary>
        /// Called by TeamManager when a winner is detected in Firestore.
        /// This is the central entry point for ending the game.
        /// </summary>
        public void TriggerGameEnd(string winnerTeamId, string winnerTeamName)
        {
            Debug.Log($"🏁 [GameEndManager] Game Finished! Winner: {winnerTeamName} (ID: {winnerTeamId})");

            // 1. Stop all gameplay immediately
            StopAllGameplay();

            // 2. Fire global event for other systems to react
            OnGameFinished?.Invoke(winnerTeamId);

            // 3. Determine if current team won or lost
            bool didWeWin = TeamManager.Instance.CurrentTeamId == winnerTeamId;

            // 4. Show appropriate UI based on toggle and result
            if (didWeWin)
            {
                ShowWinPanel();
            }
            else if (enableGlobalLose)
            {
                ShowLostPanel();
            }
            else
            {
                Debug.Log("[GameEndManager] Current team lost, but enableGlobalLose is false. No UI shown.");
            }
        }

        /// <summary>
        /// Immediately stops all active gameplay elements.
        /// </summary>
        private void StopAllGameplay()
        {
            Debug.Log("🛑 [GameEndManager] Stopping all gameplay...");

            // Cancel question UI if active
            var questionManager = FindObjectOfType<QuestionManager>();
            if (questionManager != null)
            {
                questionManager.CancelCurrentQuestion();
            }

            // Disable item interactions
            var itemCollector = FindObjectOfType<ItemCollector>();
            if (itemCollector != null)
            {
                itemCollector.enabled = false;
            }

            // Close any active popups/panels (except the end panel)
            // You can add more here based on your game's UI structure
        }

        private void ShowWinPanel()
        {
            Debug.Log("🎉 [GameEndManager] Showing WIN panel.");
            
            // Use existing WinUIManager instead of GameEndPanel
            var winUI = FindObjectOfType<WinUIManager>();
            if (winUI != null)
            {
                winUI.ShowPanel(true); // true = win
            }
            else
            {
                Debug.LogWarning("[GameEndManager] WinUIManager not found in scene!");
            }
        }

        private void ShowLostPanel()
        {
            Debug.Log("😢 [GameEndManager] Showing LOST panel.");
            
            // Use existing WinUIManager instead of GameEndPanel
            var winUI = FindObjectOfType<WinUIManager>();
            if (winUI != null)
            {
                winUI.ShowPanel(false); // false = lost
            }
            else
            {
                Debug.LogWarning("[GameEndManager] WinUIManager not found in scene!");
            }
        }

        /// <summary>
        /// Called when the user clicks EXIT on the end panel.
        /// Resets the game to the initial state.
        /// </summary>
        public void OnExitButtonPressed()
        {
            Debug.Log("[GameEndManager] EXIT pressed. Returning to scan state.");

            // Use existing GameModeManager reset logic
            if (GameModeManager.Instance != null)
            {
                GameModeManager.Instance.EndGameMode();
            }

            // Hide the win UI
            var winUI = FindObjectOfType<WinUIManager>();
            if (winUI != null)
            {
                winUI.HidePanel();
            }
        }
    }
}
