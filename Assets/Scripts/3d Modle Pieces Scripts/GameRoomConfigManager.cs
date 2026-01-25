using UnityEngine;
using Firebase.Firestore;
using Firebase.Extensions;

public class GameRoomConfigManager : MonoBehaviour
{
    public static GameRoomConfigManager Instance;

    public bool IsReady { get; private set; }

    // PlayArea
    public float playAreaWidth;
    public float playAreaDepth;
    public float safeMargin;

    // Spawn
    public int itemCount;
    public float minItemSpacing;
    public float forwardBias;
    public float minSpawnRadius; // NEW: Inner "donut" hole radius

    // Discovery
    public float discoveryRadius;
    public float fadeSpeed;

    // Interaction
    public float interactionRadius;

    private FirebaseFirestore db;
    // [SerializeField] private PlayAreaVisualizer playAreaVisualizer;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        db = FirebaseFirestore.DefaultInstance;
    }

    public void LoadRoomConfig(string roomId)
    {
        Debug.Log($"📡 Loading config for room: {roomId}");

        IsReady = false;

        db.Collection("game_rooms")
          .Document(roomId)
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task =>
          {
              if (!task.IsCompleted || task.Result == null || !task.Result.Exists)
              {
                  Debug.LogError("❌ Room config not found. Using safe defaults.");
                  ApplySafeDefaults();
                  IsReady = true;
                  return;
              }

              var data = task.Result;

              playAreaWidth = data.GetValue<float>("playAreaWidth");
              playAreaDepth = data.GetValue<float>("playAreaDepth");
              safeMargin = data.GetValue<float>("safeMargin");

              itemCount = data.GetValue<int>("itemCount");
              minItemSpacing = data.GetValue<float>("minItemSpacing");
              // Try get value, default to 0.5f if missing to support old configs
              if (data.ContainsField("minSpawnRadius"))
                  minSpawnRadius = data.GetValue<float>("minSpawnRadius");
              else
                  minSpawnRadius = 1.5f; // Default to Search Radius if missing

              forwardBias = Mathf.Clamp01(data.GetValue<float>("forwardBias"));

              discoveryRadius = data.GetValue<float>("discoveryRadius");
              fadeSpeed = data.GetValue<float>("fadeSpeed");

              interactionRadius = data.GetValue<float>("interactionRadius");

              Validate();
              IsReady = true;

              Debug.Log("✅ Room config loaded & locked");
              
              if (GameModeManager.Instance != null)
              {
                  GameModeManager.Instance.OnConfigReady();
              }
              
              ItemSpawner.Instance?.ApplyConfig();

              // 🧠 NEW: Kick off Item Data loading immediately after Config rules
              if (GameSession.Instance != null)
              {
                  GameSession.Instance.LoadData();
              }
              else
              {
                  Debug.LogWarning("⚠️ GameSession missing - Items will not auto-load!");
              }
          });
    }

    private void Validate()
    {
        playAreaWidth = Mathf.Max(1f, playAreaWidth);
        playAreaDepth = Mathf.Max(1f, playAreaDepth);
        safeMargin = Mathf.Clamp(safeMargin, 0f, Mathf.Min(playAreaWidth, playAreaDepth) * 0.4f);

        itemCount = Mathf.Max(1, itemCount);
        minItemSpacing = Mathf.Max(0.2f, minItemSpacing);
        minSpawnRadius = Mathf.Clamp(minSpawnRadius, 0f, Mathf.Min(playAreaWidth, playAreaDepth) * 0.45f); // Prevent hole bigger than room

        discoveryRadius = Mathf.Max(interactionRadius + 0.1f, discoveryRadius);
        interactionRadius = Mathf.Max(0.2f, interactionRadius);
    }

    private void ApplySafeDefaults()
    {
        // 🧠 RESET TO ROOM SCALE DEFAULTS
        playAreaWidth = 6f; // Was 3f
        playAreaDepth = 6f; // Was 3f
        safeMargin = 0.5f;

        itemCount = 6;
        minItemSpacing = 1.0f;
        minSpawnRadius = 1.5f; // Was 0.8f. Forces items 1.5m away.
        forwardBias = 0.5f;

        discoveryRadius = 1.2f;
        fadeSpeed = 2f;
        interactionRadius = 0.8f;
    }
}
