using UnityEngine;

public class DebugPlayAreaVisualizer : MonoBehaviour
{
    [Header("Debug Toggle")]
    [SerializeField] private bool showDebug = true;

    [Header("Colors")]
    [SerializeField] private Color outerColor = Color.green;
    [SerializeField] private Color safeColor = Color.yellow;

    private void OnDrawGizmos()
    {
        if (!showDebug)
            return;

        if (GameRoomConfigManager.Instance == null)
            return;

        if (!GameRoomConfigManager.Instance.IsReady)
            return;

        float width = GameRoomConfigManager.Instance.playAreaWidth;
        float depth = GameRoomConfigManager.Instance.playAreaDepth;
        float safe = GameRoomConfigManager.Instance.safeMargin;

        Vector3 center = Vector3.zero;

        // 🟩 Outer play area
        Gizmos.color = outerColor;
        Gizmos.DrawWireCube(
            center,
            new Vector3(width, 0.01f, depth)
        );

        // 🟨 Safe inner area
        Gizmos.color = safeColor;
        Gizmos.DrawWireCube(
            center,
            new Vector3(
                width - safe * 2f,
                0.01f,
                depth - safe * 2f
            )
        );
    }
}
