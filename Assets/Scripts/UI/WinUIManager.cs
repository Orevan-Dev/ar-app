using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Manages the Win Overlay UI, including fading and the exit button.
/// Listens to GameSession.OnAllItemsCollected event.
/// </summary>
public class WinUIManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private CanvasGroup winPanelGroup; // Used for smooth fade
    [SerializeField] private Button exitButton;

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 1.0f;

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
        // Subscribe to the win event
        GameSession.OnAllItemsCollected += HandleWin;
    }

    private void OnDisable()
    {
        // Unsubscribe to avoid memory leaks
        GameSession.OnAllItemsCollected -= HandleWin;
    }

    private void HandleWin()
    {
        Debug.Log("[WinUI] Win event received! Starting fade in.");
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
}
