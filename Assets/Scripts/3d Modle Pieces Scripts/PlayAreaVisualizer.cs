using UnityEngine;

public class PlayAreaVisualizer : MonoBehaviour
{
    [SerializeField] private Color areaColor = new Color(0f, 0.6f, 1f, 0.15f);
    [SerializeField] private float height = 0.01f;

    private GameObject areaObject;

    public void Build(float width, float depth)
    {
        if (areaObject != null)
            Destroy(areaObject);

        areaObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        areaObject.name = "PlayArea_Debug";

        // Remove collider (debug only)
        Destroy(areaObject.GetComponent<Collider>());

        areaObject.transform.SetParent(transform);
        areaObject.transform.localPosition = Vector3.zero;
        areaObject.transform.localRotation = Quaternion.identity;
        areaObject.transform.localScale = new Vector3(width, height, depth);

        ApplyMaterial();
    }

    private void ApplyMaterial()
    {
        var renderer = areaObject.GetComponent<Renderer>();
        var mat = new Material(Shader.Find("Standard"));

        mat.color = areaColor;
        mat.SetFloat("_Mode", 3); // Transparent
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;

        renderer.material = mat;
    }
}
