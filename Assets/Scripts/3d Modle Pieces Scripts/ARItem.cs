
using UnityEngine;

public class ARItem : MonoBehaviour
{
    private Renderer[] renderers;

    private float discoveryRadius;
    private float fadeSpeed;
    private float interactionRadius;

    private float currentAlpha = 0f;
    private bool isCollected = false;

    private Transform cameraTransform;
    private bool isConfigured = false;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();

        foreach (var r in renderers)
        {
            foreach (var mat in r.materials)
            {
                MakeMaterialTransparent(mat);
            }
        }

        // foreach (var r in renderers)
        // {
        //     foreach (var mat in r.materials)
        //     {
        //         mat.SetInt("_ZWrite", 0);
        //         mat.renderQueue = 3000;
        //     }
        // }

        cameraTransform = Camera.main.transform;
        SetAlpha(0f);
    }

    void Start()
    {
        // var config = GameRoomConfigManager.Instance;

        // discoveryRadius = config.discoveryRadius;
        // fadeSpeed = config.fadeSpeed;
        // interactionRadius = config.interactionRadius;
    }

    void Update()
    {

        if (!isConfigured)
        {
            if (!GameRoomConfigManager.Instance.IsReady)
                return;

            ApplyConfig();
        }
        if (isCollected) return;

        float distance = Vector3.Distance(transform.position, cameraTransform.position);
        float targetAlpha = distance <= discoveryRadius ? 1f : 0f;

        if (!Mathf.Approximately(currentAlpha, targetAlpha))
        {
            currentAlpha = Mathf.MoveTowards(
                currentAlpha,
                targetAlpha,
                Time.deltaTime * fadeSpeed
            );

            SetAlpha(currentAlpha);
        }
    }
    void ApplyConfig()
    {
        var config = GameRoomConfigManager.Instance;

        discoveryRadius = config.discoveryRadius;
        fadeSpeed = config.fadeSpeed;
        interactionRadius = config.interactionRadius;

        isConfigured = true;

        Debug.Log(
            $"👁 ARItem READY: {name}\n" +
            $"📍 World Pos: {transform.position}\n" +
            $"📏 Discovery Radius: {discoveryRadius}"
        );
    }
    // OnMouseDown removed. Interaction is now handled by ItemCollector + ItemInteractionManager (Centralized)
    void MakeMaterialTransparent(Material mat)
    {
        mat.SetFloat("_Mode", 2); // Fade
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
    }

    private void SetAlpha(float alpha)
    {
        foreach (var r in renderers)
        {
            foreach (var mat in r.materials)
            {
                Color c = mat.color;
                c.a = alpha;
                mat.color = c;
            }
        }
    }

    // Collect removed. Managed by QuestionManager.
}


