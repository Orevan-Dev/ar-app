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
    private float distanceMultiplier = 1.0f;

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
        distanceMultiplier = c.distanceMultiplier;

        Debug.Log($"📦 ItemSpawner config applied. DistMult: {distanceMultiplier}");
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
        if (items == null || items.Count == 0)
        {
            Debug.LogWarning("[ItemSpawner] SpawnItems called with empty or null list. Ignoring.");
            return;
        }

        if (hasSpawned || !GameModeManager.Instance.isGameRunning)
        {
            Debug.Log($"[ItemSpawner] Spawn ignored. hasSpawned: {hasSpawned}, isGameRunning: {GameModeManager.Instance.isGameRunning}");
            return;
        }

        hasSpawned = true;
        spawnedItems.Clear();

        Debug.Log($"🔥 START SPAWNING {items.Count} ITEMS");
        float startTime = Time.time;

        // 🧠 HYBRID FIX 1: Origin is the ANCHOR, not the CAMERA
        // This stops items from orbiting the player if they walk away.
        Vector3 originPos;
        Vector3 forwardDir;
        
        if (PlayAreaAnchor.Instance != null)
        {
             originPos = PlayAreaAnchor.Instance.transform.position;
             forwardDir = PlayAreaAnchor.Instance.transform.forward;
             // If anchor has no rotation (identity), use camera forward at start or just Z forward
             if (forwardDir == Vector3.zero) forwardDir = Vector3.forward;
        }
        else
        {
             originPos = Camera.main.transform.position;
             forwardDir = Camera.main.transform.forward;
        }

        float floorY = originPos.y; // Keep same floor level
        originPos.y = floorY;

        // Use raw Config values
        float halfW = playAreaWidth * 0.5f; 
        float halfD = playAreaDepth * 0.5f;
        float minR = Mathf.Max(minSpawnRadius, 1.0f);
        float maxR = Mathf.Min(halfW, halfD); // Use radial limit for the "Room" feel

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

                // 🧠 RELAXATION LOGIC
                if (attempts > 50) 
                {
                    currentSpacing *= 0.95f; 
                    currentMinR *= 0.95f;   
                }

                // 🧠 HYBRID FIX 2 & 3: Forward Bias + Distance Multiplier
                // We use Polar Coordinates to easily control Angle and Distance
                
                // A. Angle with Bias
                float bias = Config.forwardBias; // 0.0 to 1.0
                float randomVal = Random.value;
                float angle;

                // "Bias" % chance to be in the front 180 degrees (-90 to +90)
                if (randomVal < bias)
                {
                    angle = Random.Range(-90f, 90f);
                }
                else
                {
                    // Remaining % chance to be in the back 180 degrees
                    angle = Random.Range(90f, 270f);
                }
                
                // Rotate angle relative to forward direction
                float baseAngle = Mathf.Atan2(forwardDir.z, forwardDir.x) * Mathf.Rad2Deg;
                float finalAngleRad = (baseAngle - angle) * Mathf.Deg2Rad; // Unity rotation is clockwise? Check math. 
                // Standard trig: x = cos, z = sin. 0 deg is +X. 
                // Let's use Quaternion for safety to rotate the vector.
                Quaternion rot = Quaternion.AngleAxis(angle, Vector3.up);
                Vector3 dir = rot * forwardDir;

                // B. Distance with Multiplier
                // Pick a distance between hole and max room radius
                float rawDistance = Random.Range(currentMinR, maxR);
                // float roll = Random.value;
                // float rawDistance;

                // if (roll < 0.3f)
                //     rawDistance = Random.Range(currentMinR, maxR * 0.45f);   // near
                // else if (roll < 0.7f)
                //     rawDistance = Random.Range(maxR * 0.45f, maxR * 0.75f);  // mid
                // else
                //     rawDistance = Random.Range(maxR * 0.75f, maxR);          // far
                // 🧠 CONTROL KNOB: Multiply the distance visually
                // If multiplier is 2.0, items spawn 2x further than the "room logic" suggests
                float finalDistance = rawDistance * distanceMultiplier;

                Vector3 localCandidate = dir * finalDistance;

                // C. Position
                Vector3 futureWorldPos = originPos + localCandidate;
                
                // D. Spacing Check (Standard)
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
                
                // Rotate item to face the origin (optional, looks nice)
                itemFn.transform.LookAt(new Vector3(originPos.x, itemFn.transform.position.y, originPos.z));

                // Add Identifier
                var idComp = itemFn.AddComponent<GameItemIdentifier>();
                idComp.ItemId = data.id;

                spawnedItems.Add(itemFn);
                placed = true;

                Debug.Log($"🧩 Item {data.name} Spawned at {futureWorldPos} (Dist: {finalDistance:F1}m). Bias used: {randomVal < bias}");
            }

            if (!placed)
            {
                Debug.LogError($"❌ FAILED to place item {data.name} even after 200 attempts!");
            }
        }

        Debug.Log($"✅ FINISHED SPAWNING {spawnedItems.Count} items in {Time.time - startTime:F2}s");
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

