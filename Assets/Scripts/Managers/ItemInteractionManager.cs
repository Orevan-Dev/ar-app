using UnityEngine;
using GameData;

public class ItemInteractionManager : MonoBehaviour
{
    public static ItemInteractionManager Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    public void OnItemClicked(GameObject itemObject)
    {
        // 1. Get Identifier
        var identifier = itemObject.GetComponent<GameItemIdentifier>();
        if (identifier == null)
        {
            Debug.LogWarning("Clicked object has no GameItemIdentifier!");
            return;
        }

        // 2. Get Data
        GameItemData data = GameSession.Instance.GetItemData(identifier.ItemId);
        if (data == null)
        {
            Debug.LogError($"No data found for item ID: {identifier.ItemId}");
            return;
        }

        // 3. Check status
        if (data.isCollected)
        {
            Debug.Log($"Item {data.name} is already collected.");
            return;
        }

        // 4. Start Question Cycle
        // Check if QuestionManager is free or already asking
        if (QuestionManager.Instance.IsSessionActive)
        {
            Debug.Log("Question session already active. Ignoring click.");
            return;
        }

        Debug.Log($"Starting interaction with {data.name}");
        QuestionManager.Instance.StartQuestionSession(data, itemObject);
    }
}
