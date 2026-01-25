using UnityEngine;
using TMPro;

public class CollectionUI : MonoBehaviour
{
    public static CollectionUI Instance;

    [Header("UI Components")]
    [SerializeField] private GameObject displayPanel; // The bar or container
    [SerializeField] private TextMeshProUGUI countText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        
        // Hide the UI by default until game starts
        Hide();
    }

    public void Show()
    {
        if (displayPanel != null) displayPanel.SetActive(true);
        
        // Initialize text immediately when showing (e.g. "Collected 0 / 6")
        if (GameSession.Instance != null)
        {
            UpdateText(GameSession.Instance.CollectedCount, GameSession.Instance.TotalItems);
        }
    }

    public void Hide()
    {
        if (displayPanel != null) displayPanel.SetActive(false);
    }

    private void OnEnable()
    {
        GameSession.OnItemCollected += HandleItemCollected;
    }

    private void OnDisable()
    {
        GameSession.OnItemCollected -= HandleItemCollected;
    }

    private void HandleItemCollected(int current, int total)
    {
        UpdateText(current, total);
    }

    private void UpdateText(int current, int total)
    {
        if (countText != null)
        {
            countText.text = $"Collected {current} / {total}";
        }
    }

    // Public method for manual/initial updates
    public void UpdateCount(int collected, int total)
    {
        UpdateText(collected, total);
    }
}
