using UnityEngine;
using UnityEditor;
using System.IO;

public class RobotPieceGenerator : EditorWindow
{
    private int numberOfPieces = 6;
    private string baseName = "Piece";
    private string saveFolder = "Assets/Resources/Prefabs/RobotPieces";

    private Vector3 pieceScale = new Vector3(1.2f, 1.2f, 1.2f);

    [MenuItem("Tools/Robot Piece Generator")]
    public static void ShowWindow()
    {
        GetWindow<RobotPieceGenerator>("Robot Piece Generator");
    }

    void OnGUI()
    {
        GUILayout.Label("Generate Robot Pieces", EditorStyles.boldLabel);

        numberOfPieces = EditorGUILayout.IntField("Number of Pieces:", numberOfPieces);
        baseName = EditorGUILayout.TextField("Base Name:", baseName);
        saveFolder = EditorGUILayout.TextField("Save Folder:", saveFolder);

        GUILayout.Space(10);

        pieceScale = EditorGUILayout.Vector3Field("Piece Scale:", pieceScale);

        GUILayout.Space(20);

        if (GUILayout.Button("Generate Prefabs"))
        {
            GeneratePrefabs();
        }
    }

    void GeneratePrefabs()
    {
        if (!Directory.Exists(saveFolder))
        {
            Directory.CreateDirectory(saveFolder);
        }

        // Materials folder
        string materialFolder = $"{saveFolder}/Materials";
        if (!Directory.Exists(materialFolder))
        {
            Directory.CreateDirectory(materialFolder);
        }

        for (int i = 1; i <= numberOfPieces; i++)
        {
            // =========================
            // ROOT OBJECT
            // =========================
            GameObject root = new GameObject($"{baseName}_{i}");
            root.transform.localScale = Vector3.one;

            // =========================
            // VISUAL CHILD
            // =========================
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Visual";
            visual.transform.SetParent(root.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = pieceScale;

            // Ensure collider exists for Raycasting
            if (visual.GetComponent<Collider>() == null)
            {
               visual.AddComponent<BoxCollider>();
            }

            // =========================
            // MATERIAL
            // =========================
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = Random.ColorHSV();

            string matPath = $"{materialFolder}/{root.name}_Mat.mat";
            AssetDatabase.CreateAsset(mat, matPath);

            Renderer renderer = visual.GetComponent<Renderer>();
            renderer.sharedMaterial = mat;

            // =========================
            // ADD ROBOT PIECE SCRIPT
            // =========================
            if (root.GetComponent<RobotPiece>() == null)
            {
                root.AddComponent<RobotPiece>();
            }

            if (root.GetComponent<ARItem>() == null)
            {
                root.AddComponent<ARItem>();
            }


            // =========================
            // SAVE PREFAB
            // =========================
            string prefabPath = $"{saveFolder}/{root.name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

            DestroyImmediate(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✅ Generated {numberOfPieces} robot pieces with Visual hierarchy in: {saveFolder}");
    }

}
