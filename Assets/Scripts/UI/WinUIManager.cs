using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Managers;

/// <summary>
/// Manages the Win/Lost Overlay UI, including fading and the exit button.
/// 
/// Phase 1: Listens to GameSession.OnAllItemsCollected (local win)
/// Phase 2: Listens to GameEndManager.OnGameFinished (global win/loss)
/// </summary>
public class WinUIManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private CanvasGroup winPanelGroup; // Used for smooth fade
    [SerializeField] private Button exitButton;
    
    [Header("UI Text (Phase 2)")]
    [SerializeField] private TextMeshProUGUI resultText; // Optional: For "YOU WIN" / "YOU LOST"

    [Header("UI Images (Phase 2)")]
    [SerializeField] private Image displayImage; // Image component that will show the robot
    [SerializeField] private Sprite winSprite;   // Image to show on win
    [SerializeField] private Sprite lostSprite;  // Image to show on loss
    
    [Header("Settings")]
    [SerializeField] private float fadeDuration = 1.0f;
    
    [Header("Text Content (Phase 2)")]
    [SerializeField] private string winText = "YOU WIN";
    [SerializeField] private string lostText = "YOU LOST";

    private void Awake()
    {
        // Initial state: Hidden and non-interactable
        if (winPanelGroup != null)
        {
            winPanelGroup.alpha = 0f;
            winPanelGroup.blocksRaycasts = false;
            winPanelGroup.interactable = false;
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(OnExitPressed);
        }
    }

    private void OnEnable()
    {
        // Phase 1: Subscribe to local win event
        GameSession.OnAllItemsCollected += HandleLocalWin;
        
        // Phase 2: Subscribe to global game-end event
        GameEndManager.OnGameFinished += HandleGlobalGameEnd;
    }

    private void OnDisable()
    {
        // Unsubscribe to avoid memory leaks
        GameSession.OnAllItemsCollected -= HandleLocalWin;
        GameEndManager.OnGameFinished -= HandleGlobalGameEnd;
    }

    /// <summary>
    /// Phase 1: Called when the current team collects all items locally.
    /// This is the original behavior.
    /// </summary>
    private void HandleLocalWin()
    {
        Debug.Log("[WinUI] Local win event received! Starting fade in.");
        ShowPanel(true); // Always a win for local completion
    }

    /// <summary>
    /// Phase 2: Called when ANY team wins globally via Firestore.
    /// Determines if current team won or lost.
    /// </summary>
    private void HandleGlobalGameEnd(string winnerTeamId)
    {
        Debug.Log($"[WinUI] Global game end received. Winner: {winnerTeamId}");
        
        // Check if we won
        bool didWeWin = TeamManager.Instance != null && 
                        TeamManager.Instance.CurrentTeamId == winnerTeamId;
        
        // Check feature toggle
        bool showLostPanel = GameEndManager.Instance != null && 
                            GameEndManager.Instance.enableGlobalLose;
        
        if (didWeWin)
        {
            ShowPanel(true); // Show WIN
        }
        else if (showLostPanel)
        {
            ShowPanel(false); // Show LOST
        }
        else
        {
            Debug.Log("[WinUI] Current team lost, but enableGlobalLose is false. No UI shown.");
        }
    }

    /// <summary>
    /// Shows the panel with appropriate text and image.
    /// </summary>
    /// <param name="didWin">True for WIN, false for LOST</param>
    public void ShowPanel(bool didWin)
    {
        // Update text if available (Phase 2)
        if (resultText != null)
        {
            resultText.text = didWin ? winText : lostText;
        }

        // Update image if available (Phase 2)
        if (displayImage != null)
        {
            displayImage.sprite = didWin ? winSprite : lostSprite;
        }
        
        StopAllCoroutines();
        StartCoroutine(FadeInSequence());
    }

    private IEnumerator FadeInSequence()
    {
        if (winPanelGroup == null) yield break;

        // Ensure visible but not yet interactable while fading
        winPanelGroup.blocksRaycasts = true;
        
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            winPanelGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        winPanelGroup.alpha = 1f;
        winPanelGroup.interactable = true;
    }

    private void OnExitPressed()
    {
        Debug.Log("[WinUI] Exit button pressed. Returning to scan state.");
        
        // Hide UI immediately
        if (winPanelGroup != null)
        {
            winPanelGroup.alpha = 0f;
            winPanelGroup.blocksRaycasts = false;
            winPanelGroup.interactable = false;
        }

        // Trigger the global game reset
        if (GameModeManager.Instance != null)
        {
            // Transition back to None state via GameModeManager
            // This clears items, resets GPS, and shows the scanner
            GameModeManager.Instance.EndGameMode();
        }
    }
    
    /// <summary>
    /// Public method to hide the panel (called externally if needed).
    /// </summary>
    public void HidePanel()
    {
        if (winPanelGroup != null)
        {
            winPanelGroup.alpha = 0f;
            winPanelGroup.blocksRaycasts = false;
            winPanelGroup.interactable = false;
        }
    }
}

