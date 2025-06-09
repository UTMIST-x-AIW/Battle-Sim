using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class MeshMaker : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField]
    MeshFilter meshFilter;
    void Start()
    {
        Mesh mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        Vector3[] vertices = new Vector3[3];
        Vector2[] uvs = new Vector2[3];
        int[] triangles = new int[3];
        
        vertices[0] = new Vector3(0, 0);
        vertices[1] = new Vector3(1,0);
        vertices[2] = new Vector3(0,1);
        
        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 2;
        mesh.vertices = vertices;
        mesh.triangles = triangles;
    }

    
    public void SaveMesh(string assetPath)
    {
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogError("No mesh found to save!");
            return;
        }

        Mesh meshToSave = meshFilter.sharedMesh;

#if UNITY_EDITOR
        // Save the mesh as an asset
        AssetDatabase.CreateAsset(meshToSave, assetPath);
        AssetDatabase.SaveAssets();
        Debug.Log($"Mesh saved to {assetPath}");
#else
        Debug.LogWarning("Mesh saving is only available in the Unity Editor.");
#endif
    }

    // Example usage
    [ContextMenu("Save Mesh")]
    private void SaveMeshExample()
    {
        SaveMesh("Assets/SavedMesh.asset");
    }
}
