using UnityEngine;
using System.Collections.Generic;
using GameData;

public class QuestionManager : MonoBehaviour
{
    public static QuestionManager Instance;

    [Header("Settings")]
    public float maxInteractionDistance = 100.0f; // Increased for testing

    private GameItemData currentItemData;
    private GameObject currentItemObject;
    public bool IsSessionActive { get; private set; }

    // Logic state
    private List<QuestionData> sessionQuestions;
    private int currentQuestionIndex;
    
    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Update()
    {
        if (IsSessionActive && currentItemObject != null)
        {
            // Check distance
            float dist = Vector3.Distance(Camera.main.transform.position, currentItemObject.transform.position);
            if (dist > maxInteractionDistance)
            {
                Debug.Log($"⚠️ Auto-Cancel (Distance): Current {dist:F2} > Max {maxInteractionDistance:F2}");
                CancelSession();
            }
        }
    }

    public void StartQuestionSession(GameItemData itemData, GameObject itemObj)
    {
        currentItemData = itemData;
        currentItemObject = itemObj;
        IsSessionActive = true;

        // Prepare questions (Shuffle logic ideally, or just load them)
        // User said: "one random question ... wrong -> another question ... all 3 wrong -> cycle resets"
        // So we can shuffle the list and iterate.
        ResetQuestionCycle();

        NextQuestion();
    }

    private void ResetQuestionCycle()
    {
        // Copy list
        sessionQuestions = new List<QuestionData>(currentItemData.questions);
        // Shuffle
        for (int i = 0; i < sessionQuestions.Count; i++)
        {
            QuestionData temp = sessionQuestions[i];
            int randomIndex = Random.Range(i, sessionQuestions.Count);
            sessionQuestions[i] = sessionQuestions[randomIndex];
            sessionQuestions[randomIndex] = temp;
        }
        currentQuestionIndex = 0;
    }

    private void NextQuestion()
    {
        if (currentQuestionIndex >= sessionQuestions.Count)
        {
            // Cycle ended (all wrong probably), reset
            Debug.Log("Cycle finished with all wrong. Resetting.");
            ResetQuestionCycle();
        }

        QuestionData q = sessionQuestions[currentQuestionIndex];
        // Show UI
        if (QuestionUI.Instance != null)
        {
            // Position UI above item
            Vector3 worldPos = currentItemObject.transform.position + Vector3.up * 0.5f; 
            QuestionUI.Instance.ShowQuestion(q.text, worldPos);
        }
    }

    public void SubmitAnswer(bool answer)
    {
        if (!IsSessionActive) return;

        QuestionData currentQ = sessionQuestions[currentQuestionIndex];
        
        if (answer == currentQ.correctAnswer)
        {
            // Correct!
            OnCorrectAnswer();
        }
        else
        {
            // Wrong
            Debug.Log("Wrong answer.");
            currentQuestionIndex++;
            NextQuestion();
        }
    }

    private void OnCorrectAnswer()
    {
        Debug.Log("Correct Answer! Collecting item.");
        
        // 1. Mark Collected
        GameSession.Instance.MarkItemCollected(currentItemData);
        
        // 2. Hide UI
        if (QuestionUI.Instance != null) QuestionUI.Instance.Hide();
        
        // 3. Remove Item
        // Ideally play effect
        if (ItemSpawner.Instance != null) ItemSpawner.Instance.OnItemCollected(currentItemObject);
        Destroy(currentItemObject);

        EndSession();
    }

    public void CancelSession()
    {
        if (!IsSessionActive) return;
        
        Debug.Log("Session cancelled (walked away or other).");
        if (QuestionUI.Instance != null) QuestionUI.Instance.Hide();
        EndSession();
    }

    /// <summary>
    /// Phase 2: Immediately cancels the current question without any feedback.
    /// Called when the game ends globally.
    /// </summary>
    public void CancelCurrentQuestion()
    {
        if (IsSessionActive)
        {
            Debug.Log("[QuestionManager] Force-cancelling question due to game end.");
            if (QuestionUI.Instance != null) QuestionUI.Instance.Hide();
            EndSession();
        }
    }

    private void EndSession()
    {
        currentItemData = null;
        currentItemObject = null;
        IsSessionActive = false;
        sessionQuestions = null;
    }
}
