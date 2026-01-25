using Firebase.Firestore;

namespace GameData
{
    [FirestoreData]
    public class TeamModel
    {
        [FirestoreDocumentId]
        public string Id { get; set; }

        [FirestoreProperty("teamName")]
        public string TeamName { get; set; }

        [FirestoreProperty("collectedItemsCount")]
        public int CollectedItemsCount { get; set; }

        [FirestoreProperty("createdAt")]
        public Timestamp CreatedAt { get; set; }

        [FirestoreProperty("isWinner")]
        public bool IsWinner { get; set; }
    }
}
