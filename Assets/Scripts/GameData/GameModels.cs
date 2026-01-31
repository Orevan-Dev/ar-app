using System;
using System.Collections.Generic;

namespace GameData
{
    [Serializable]
    public class GameItemData
    {
        public string id; // Firestore Document ID
        public string name; // Display name
        public string prefabId; // ID to find the prefab (e.g., "RobotArm", "Gear")
        public List<QuestionData> questions;
        public bool isCollected;

        public GameItemData()
        {
            questions = new List<QuestionData>();
            isCollected = false;
        }
    }

    [Serializable]
    public class QuestionData
    {
        public string text;
        public bool correctAnswer; // true for Yes, false for No

        public QuestionData(string text, bool correctAnswer)
        {
            this.text = text;
            this.correctAnswer = correctAnswer;
        }
    }
}
