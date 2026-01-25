using UnityEngine;

/// <summary>
/// Handles tap-to-collect interaction for AR items.
/// Uses screen raycast to detect taps on world-space items.
/// </summary>
public class ItemCollector : MonoBehaviour
{
    [Header("Collection Settings")]
    [SerializeField] private LayerMask itemLayerMask = -1; // All layers by default
    
    private Camera playerCamera;
    
    private void Start()
    {
        playerCamera = Camera.main;
        
        if (playerCamera == null)
        {
            Debug.LogError("ItemCollector: Main Camera not found!");
        }
    }
    
    private void Update()
    {
        // Prioritize Touch Input for mobile
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                HandleTap(touch.position);
            }
            return; // Don't check mouse if touching
        }

        // Fallback to Mouse for Editor
        if (Input.GetMouseButtonDown(0))
        {
            HandleTap(Input.mousePosition);
        }
    }
    
    /// <summary>
    /// Handles tap input by casting a ray from screen position to world space.
    /// </summary>
    private void HandleTap(Vector2 screenPosition)
    {
        if (playerCamera == null) return;
        
        // Cast ray from screen position into world space
        Ray ray = playerCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;
        
        // Check if ray hits an item collider
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, itemLayerMask))
        {
            // Check if hit object has GameItemIdentifier component
            // We look on the object or its parent/root
            GameItemIdentifier identifier = hit.collider.GetComponentInParent<GameItemIdentifier>();
            
            if (identifier != null)
            {
                // Delegate to Interaction Manager
                ItemInteractionManager.Instance.OnItemClicked(identifier.gameObject);
            }
        }
    }
}
