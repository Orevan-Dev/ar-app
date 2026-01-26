using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// UI Controller for the Game End Panel (Phase 2).
/// Shows WIN or LOST result with smooth fade-in animation.
/// </summary>
public class GameEndPanel : MonoBehaviour
{
    public static GameEndPanel Instance;

    [Header("UI References")]
    public GameObject panelRoot;
    public TextMeshProUGUI resultText;
    public Image robotImage;
    public Button exitButton;
    public CanvasGroup canvasGroup;

    [Header("Animation Settings")]
    public float fadeInDuration = 1f;

    [Header("Text Content")]
    public string winText = "YOU WIN";
    public string lostText = "YOU LOST";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Ensure panel is hidden at start
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        // Setup exit button
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(OnExitClicked);
        }

        // Ensure we have a CanvasGroup for fading
        if (canvasGroup == null)
        {
            canvasGroup = panelRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = panelRoot.AddComponent<CanvasGroup>();
            }
        }
    }

    /// <summary>
    /// Shows the panel with the appropriate result.
    /// </summary>
    /// <param name="didWin">True if current team won, false if lost</param>
    public void ShowPanel(bool didWin)
    {
        if (panelRoot == null)
        {
            Debug.LogError("[GameEndPanel] Panel root is not assigned!");
            return;
        }

        // Set the result text
        if (resultText != null)
        {
            resultText.text = didWin ? winText : lostText;
        }

        // Activate panel
        panelRoot.SetActive(true);

        // Start fade-in animation
        StartCoroutine(FadeIn());
    }

    /// <summary>
    /// Hides the panel (called when returning to scan state).
    /// </summary>
    public void HidePanel()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private IEnumerator FadeIn()
    {
        if (canvasGroup == null) yield break;

        canvasGroup.alpha = 0f;
        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    private void OnExitClicked()
    {
        Debug.Log("[GameEndPanel] Exit button clicked.");
        
        // Notify GameEndManager to handle the exit logic
        if (Managers.GameEndManager.Instance != null)
        {
            Managers.GameEndManager.Instance.OnExitButtonPressed();
        }
    }
}
