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

    // 🧠 SAFE SPAWN CONSTANTS
    private const float MIN_SAFE_DISTANCE = 1.5f; // Real-world meters - prevents spawning too close
    private const float VERTICAL_VARIANCE = 0.3f; // ±0.3 meters - adds natural height variation

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
        
        // 🧠 SAFE SPAWN: Calculate slice size for even distribution
        float sliceSize = 360f / items.Count;
        int itemIndex = 0;
        
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
            float currentMaxR = maxR; // Track max radius for expansion

            while (!placed && attempts < 200) 
            {
                attempts++;

                // 🧠 SAFE SPAWN FIX 1: Expansion instead of Shrinking
                if (attempts > 50) 
                {
                    currentSpacing *= 0.95f; // Relax spacing (safe)
                    currentMaxR *= 1.1f;     // EXPAND outward, never shrink inward
                }

                // 🧠 SAFE SPAWN FIX 2: Angular Slice Distribution
                // Divide 360° into equal slices, pick random angle within this item's slice
                float sliceStart = itemIndex * sliceSize;
                float sliceEnd = sliceStart + sliceSize;
                float angle = Random.Range(sliceStart, sliceEnd);
                
                // Rotate angle relative to forward direction
                Quaternion rot = Quaternion.AngleAxis(angle, Vector3.up);
                Vector3 dir = rot * forwardDir;

                // 🧠 SAFE SPAWN FIX 3: Minimum Distance with Outward Clamp
                // Pick random distance, then enforce minimum safe distance
                float rawDistance = Random.Range(minR, currentMaxR);
                rawDistance = Mathf.Max(rawDistance, MIN_SAFE_DISTANCE); // Never closer than 1.5m
                
                // Apply distance multiplier AFTER safety clamp
                float finalDistance = rawDistance * distanceMultiplier;

                Vector3 localCandidate = dir * finalDistance;

                // C. Position
                Vector3 futureWorldPos = originPos + localCandidate;
                
                // 🧠 SAFE SPAWN FIX 4: Vertical Randomization
                // Add natural height variation instead of flat line
                float verticalOffset = Random.Range(-VERTICAL_VARIANCE, VERTICAL_VARIANCE);
                futureWorldPos.y = Camera.main.transform.position.y + verticalOffset;
                
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

                // 🧠 SAFE SPAWN FIX 5: Double-Sided Rendering
                // Enable visibility from inside mesh (prevents invisibility when camera clips)
                Renderer[] renderers = itemFn.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    foreach (var mat in renderer.materials)
                    {
                        mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                    }
                }

                // Add Identifier
                var idComp = itemFn.AddComponent<GameItemIdentifier>();
                idComp.ItemId = data.id;

                spawnedItems.Add(itemFn);
                placed = true;

                Debug.Log($"🧩 Item {data.name} Spawned at {futureWorldPos} (Dist: {finalDistance:F1}m, Angle: {angle:F0}°, Slice: {itemIndex + 1}/{items.Count})");
            }

            if (!placed)
            {
                Debug.LogError($"❌ FAILED to place item {data.name} even after 200 attempts!");
            }
            
            itemIndex++; // Move to next slice
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

