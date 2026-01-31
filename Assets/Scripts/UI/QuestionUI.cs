using UnityEngine;
using UnityEngine.UI;
using TMPro; // Assuming TextMeshPro is used, if not I'll use Text

public class QuestionUI : MonoBehaviour
{
    public static QuestionUI Instance;

    [Header("UI References")]
    public GameObject resultPanel; // The whole container
    public TextMeshProUGUI questionText; 
    public Button yesButton;
    public Button noButton;

    [Header("Behavior")]
    public bool lookAtCamera = true;
    public float heightOffset = 1.0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        
        // Setup listeners
        if (yesButton) yesButton.onClick.AddListener(() => OnAnswer(true));
        if (noButton) noButton.onClick.AddListener(() => OnAnswer(false));
        
        Hide();
    }

    private void Update()
    {
        // Only billboard if using World Space Canvas
        if (resultPanel.activeSelf && lookAtCamera)
        {
             Canvas canvas = GetComponentInParent<Canvas>();
             if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
             {
                resultPanel.transform.LookAt(Camera.main.transform);
                resultPanel.transform.Rotate(0, 180, 0); 
             }
        }
    }

    public void ShowQuestion(string text, Vector3 worldPosition)
    {
        if (resultPanel)
        {
            resultPanel.SetActive(true);
            
            // Only move if World Space
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
            {
                 resultPanel.transform.position = worldPosition + Vector3.up * heightOffset;
            }
        }
        
        if (questionText) questionText.text = text;
    }

    public void Hide()
    {
        if (resultPanel) resultPanel.SetActive(false);
    }

    private void OnAnswer(bool isYes)
    {
        QuestionManager.Instance.SubmitAnswer(isYes);
    }
}
