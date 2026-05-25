#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class SaveCombinedMeshAsset
{
    [MenuItem("Tools/Save Combined Mesh Asset")]
    static void SaveMesh()
    {
        var go = Selection.activeGameObject;
        if (go == null)
        {
            Debug.LogError("Select the combined Arrow object.");
            return;
        }

        var mf = go.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
        {
            Debug.LogError("Selected object has no mesh.");
            return;
        }

        string path = EditorUtility.SaveFilePanelInProject(
            "Save Combined Mesh",
            mf.sharedMesh.name + ".asset",
            "asset",
            "Choose where to save the mesh asset"
        );

        if (string.IsNullOrEmpty(path))
            return;

        Mesh meshCopy = Object.Instantiate(mf.sharedMesh);
        AssetDatabase.CreateAsset(meshCopy, path);
        AssetDatabase.SaveAssets();

        Debug.Log("Saved mesh asset to: " + path);
    }
}
#endif
