using UnityEngine;
using Managers;

/// <summary>
/// Example script showing how to subscribe to Phase 2 game-end events.
/// This is NOT required for Phase 2 to work - it's just a reference example.
/// </summary>
public class GameEndEventExample : MonoBehaviour
{
    private void OnEnable()
    {
        // Subscribe to the global game-end event
        GameEndManager.OnGameFinished += HandleGameFinished;
    }

    private void OnDisable()
    {
        // Always unsubscribe to prevent memory leaks
        GameEndManager.OnGameFinished -= HandleGameFinished;
    }

    /// <summary>
    /// Called when any team wins the game.
    /// </summary>
    /// <param name="winnerTeamId">The ID of the winning team</param>
    private void HandleGameFinished(string winnerTeamId)
    {
        Debug.Log($"[GameEndEventExample] Game ended! Winner ID: {winnerTeamId}");

        // Example: Check if we won
        bool didWeWin = TeamManager.Instance.CurrentTeamId == winnerTeamId;
        
        if (didWeWin)
        {
            Debug.Log("🎉 We won! Custom celebration logic here.");
            // Play victory sound
            // Trigger confetti effect
            // Send analytics event
        }
        else
        {
            Debug.Log("😢 We lost. Custom loss logic here.");
            // Play sad sound
            // Show encouragement message
            // Send analytics event
        }

        // Example: Stop background music
        // AudioManager.Instance.StopMusic();

        // Example: Save game stats
        // PlayerPrefs.SetInt("GamesPlayed", PlayerPrefs.GetInt("GamesPlayed") + 1);
        // if (didWeWin) PlayerPrefs.SetInt("GamesWon", PlayerPrefs.GetInt("GamesWon") + 1);
    }
}
