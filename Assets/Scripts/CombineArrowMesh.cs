using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CombineArrowMesh : MonoBehaviour
{
    [ContextMenu("Combine Arrow Mesh")]
    public void CombineMeshes()
    {
        var childMeshFilters = GetComponentsInChildren<MeshFilter>(true);
        if (childMeshFilters == null || childMeshFilters.Length == 0)
        {
            Debug.LogError("No child MeshFilters found.");
            return;
        }

        var combineList = new System.Collections.Generic.List<CombineInstance>();
        Material firstMaterial = null;

        foreach (var mf in childMeshFilters)
        {
            if (mf.transform == transform)
                continue;

            if (mf.sharedMesh == null)
                continue;

            var mr = mf.GetComponent<MeshRenderer>();
            if (firstMaterial == null && mr != null)
                firstMaterial = mr.sharedMaterial;

            combineList.Add(new CombineInstance
            {
                mesh = mf.sharedMesh,
                transform = transform.worldToLocalMatrix * mf.transform.localToWorldMatrix
            });
        }

        if (combineList.Count == 0)
        {
            Debug.LogError("No valid child meshes found to combine.");
            return;
        }

        Mesh combinedMesh = new Mesh();
        combinedMesh.name = gameObject.name + "_Combined";
        combinedMesh.CombineMeshes(combineList.ToArray(), true, true);

        var parentMF = GetComponent<MeshFilter>();
        var parentMR = GetComponent<MeshRenderer>();

        parentMF.sharedMesh = combinedMesh;

        if (firstMaterial != null)
            parentMR.sharedMaterial = firstMaterial;

        foreach (Transform child in transform)
            child.gameObject.SetActive(false);

        Debug.Log("Combined mesh created: " + combinedMesh.name);
    }
}