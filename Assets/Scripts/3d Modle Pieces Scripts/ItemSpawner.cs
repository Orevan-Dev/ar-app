using UnityEngine;
using System.Collections.Generic;
using GameData;
using System.Linq;

/// <summary>
/// Spawns AR items in a 360° circle around the player in world space.
/// Items are fixed in world coordinates and do not follow the camera.
/// </summary>
public class ItemSpawner : MonoBehaviour
{
    public static ItemSpawner Instance;

    [Header("Spawn Settings")]
    [SerializeField] private string prefabFolderPath = "Prefabs/RobotPieces";
    
    [Tooltip("Global multiplier to increase spacing if items appear too close due to camera scale issues")]
    [SerializeField] private float globalScaleMultiplier = 20.0f; // Default to 20x to fight the shrinkage
    
    [Tooltip("Multiplier for the physical size of the item")]
    [SerializeField] private float itemScaleFactor = 6f; // User requested bigger than 1.2

    private GameObject[] availablePrefabs;



    private GameRoomConfigManager Config => GameRoomConfigManager.Instance;


    private List<GameObject> spawnedItems = new List<GameObject>();
    private bool hasSpawned = false;

    private float playAreaWidth;
    private float playAreaDepth;
    private float safeMargin;

    private int itemCount;
    private float minSpawnRadius; // NEW: Inner "donut" hole radius
    private float minDistanceBetweenItems;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[ItemSpawner] Instance set in Awake()");
        }
        else if (Instance != this)
        {
            Debug.LogWarning("[ItemSpawner] Multiple ItemSpawner instances found! Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        // Also set Instance when GameObject becomes active
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[ItemSpawner] Instance set in OnEnable()");
        }
    }

    private void Start()
    {

        // Load prefabs from Resources folder
        LoadPrefabsFromResources();

        if (availablePrefabs == null || availablePrefabs.Length == 0)
        {
            Debug.LogError($"ItemSpawner: Failed to load prefabs from Resources/{prefabFolderPath}!");
            return;
        }

        // var config = GameRoomConfigManager.Instance;
        // minDistanceBetweenItems = config.minItemSpacing;
        // playAreaWidth = config.playAreaWidth;
        // playAreaDepth = config.playAreaDepth;
        // safeMargin = config.safeMargin;

        // itemCount = config.itemCount;
        // minItemSpacing = config.minItemSpacing;
        // forwardBias = config.forwardBias;
        // Don't auto-spawn on Start - wait for explicit call from GameModeManager
    }
    public void ApplyConfig()
    {
        var c = GameRoomConfigManager.Instance;

        playAreaWidth = c.playAreaWidth;
        playAreaDepth = c.playAreaDepth;
        safeMargin = c.safeMargin;

        itemCount = c.itemCount;
        minSpawnRadius = c.minSpawnRadius;
        minDistanceBetweenItems = c.minItemSpacing;

        Debug.Log("📦 ItemSpawner config applied");
    }


    /// <summary>
    /// Loads all item prefabs from the Resources folder.
    /// Each spawn will randomly select from available prefabs.
    /// </summary>
    private void LoadPrefabsFromResources()
    {
        // Load all prefabs from the folder
        availablePrefabs = Resources.LoadAll<GameObject>(prefabFolderPath);

        if (availablePrefabs == null || availablePrefabs.Length == 0)
        {
            Debug.LogError($"ItemSpawner: No prefabs found in Resources/{prefabFolderPath}!");
            return;
        }

        Debug.Log($"ItemSpawner: Loaded {availablePrefabs.Length} prefab(s) from Resources/{prefabFolderPath}");
    }

    /// <summary>
    /// Gets a random prefab from the available prefabs.
    /// </summary>
    private GameObject GetRandomPrefab()
    {
        if (availablePrefabs == null || availablePrefabs.Length == 0)
            return null;

        return availablePrefabs[Random.Range(0, availablePrefabs.Length)];
    }






    public void SpawnItems(List<GameItemData> items)
    {
        if (hasSpawned || !GameModeManager.Instance.isGameRunning)
            return;

        hasSpawned = true;
        spawnedItems.Clear();

        Debug.Log($"🔥 START SPAWNING {items.Count} ITEMS");

        // Use player position logic as before
        Vector3 originPos = Camera.main.transform.position;
        float floorY = PlayAreaAnchor.Instance != null ? PlayAreaAnchor.Instance.transform.position.y : originPos.y - 1.4f;
        originPos.y = floorY;

        // Use raw Config values
        float halfW = playAreaWidth * 0.5f; 
        float halfD = playAreaDepth * 0.5f;
        float minR = Mathf.Max(minSpawnRadius, 1.0f);

        // Map prefabs by name for easy lookup
        if (availablePrefabs == null) LoadPrefabsFromResources();
        
        foreach (var data in items)
        {
            // Find prefab
            GameObject prefab = availablePrefabs.FirstOrDefault(p => p.name == data.prefabId);
            if (prefab == null)
            {
                Debug.LogWarning($"[ItemSpawner] Prefab '{data.prefabId}' not found. Using random.");
                prefab = GetRandomPrefab();
            }
            
            if (prefab == null) continue;

            int attempts = 0;
            bool placed = false;
            float currentSpacing = minDistanceBetweenItems;
            float currentMinR = minR;

            while (!placed && attempts < 200) 
            {
                attempts++;

                // 🧠 RELAXATION LOGIC: If we can't find a spot, slowly reduce requirements
                if (attempts > 50) 
                {
                    currentSpacing *= 0.95f; // Reduce spacing requirement
                    currentMinR *= 0.95f;   // Bring items closer to center
                }

                 // A. Cartesian Random
                float rawX = Random.Range(-halfW, halfW);
                float rawZ = Random.Range(-halfD, halfD);
                Vector3 localCandidate = new Vector3(rawX, 0f, rawZ);

                // B. Hole Check
                if (localCandidate.magnitude < currentMinR) continue; 

                // C. Position
                Vector3 futureWorldPos = originPos + localCandidate;
                
                // D. Spacing Check
                bool tooClose = false;
                foreach (var existingItem in spawnedItems)
                {
                    if (Vector3.Distance(futureWorldPos, existingItem.transform.position) < currentSpacing)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose) continue;

                // E. Spawn
                GameObject itemFn = Instantiate(prefab, futureWorldPos, Quaternion.identity);
                itemFn.transform.localScale = Vector3.one * itemScaleFactor; 
                itemFn.transform.SetParent(null); 
                
                // Add Identifier
                var idComp = itemFn.AddComponent<GameItemIdentifier>();
                idComp.ItemId = data.id;

                spawnedItems.Add(itemFn);
                placed = true;

                Debug.Log($"🧩 Item {data.name} Spawned at {futureWorldPos} after {attempts} attempts. (Spacing used: {currentSpacing:F2})");
            }

            if (!placed)
            {
                Debug.LogError($"❌ FAILED to place item {data.name} even after 200 attempts!");
            }
        }
    }

    /// <summary>
    /// Removes an item from the spawned items list when collected.
    /// </summary>
    public void OnItemCollected(GameObject item)
    {
        if (spawnedItems.Contains(item))
        {
            spawnedItems.Remove(item);
        }
    }

    /// <summary>
    /// Clears all spawned items (for testing/reset).
    /// </summary>
    public void ClearAllItems()
    {
        foreach (GameObject item in spawnedItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        spawnedItems.Clear();
    }

    public void ResetSpawner()
    {
        ClearAllItems();
        hasSpawned = false;
        spawnedItems.Clear(); // Redundant but safe
        Debug.Log("♻️ ItemSpawner RESET detected");
    }
}

