using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using static UnityEngine.Mesh;
using System.IO;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TilemapMeshGenerator : MonoBehaviour
{
    public Tilemap tilemap; // Assign in Inspector
    private Mesh mesh;
    [SerializeField] Vector2 offset;
    public HeatMapContainer mapContainer;

    void Start()
    {
        GenerateMesh();
        Debug.Log(Application.dataPath);
    }
    private void LateUpdate()
    {
        GenerateMesh();
    }


    void GenerateMesh()
    {
        if (LoadMesh("HeatMap1") != null)
        {
            return;
        }
        mesh = new Mesh();

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        Dictionary<Vector3, int> vertexIndexMap = new Dictionary<Vector3, int>();

        // Loop through all tiles in the Tilemap
        BoundsInt bounds = tilemap.cellBounds;
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int cellPosition = new Vector3Int(x, y, 0);
                if (tilemap.HasTile(cellPosition))
                {
                    // Get world position of the tile
                    Vector3 worldPos = tilemap.CellToWorld(cellPosition);
                    Vector3 isoPosition = new Vector3((worldPos.x - worldPos.y),(worldPos.x + worldPos.y) * 0.5f,0);

                    // Define the 4 corners of the tile
                    Vector3[] tileCorners = new Vector3[]
                    {
                        new Vector3(worldPos.x, worldPos.y+0.5f, 0),       // Bottom-left
                        new Vector3(worldPos.x + 1, worldPos.y, 0),   // Bottom-right
                        new Vector3(worldPos.x - 1, worldPos.y, 0),   // Top-left
                        new Vector3(worldPos.x, worldPos.y  -  0.5f, 0) // Top-right
                    };

                    int[] cornerIndices = new int[4];

                    // Add unique vertices to the list
                    for (int i = 0; i < 4; i++)
                    {
                        if (!vertexIndexMap.ContainsKey(tileCorners[i]))
                        {
                            vertexIndexMap[tileCorners[i]] = vertices.Count;
                            vertices.Add(tileCorners[i]);
                        }
                        cornerIndices[i] = vertexIndexMap[tileCorners[i]];
                    }

                    // Create two triangles for this tile
                    triangles.Add(cornerIndices[0]); // Bottom-left
                    triangles.Add(cornerIndices[1]); // Bottom-right
                    triangles.Add(cornerIndices[2]); // Top-left

                    triangles.Add(cornerIndices[1]); // Bottom-right
                    triangles.Add(cornerIndices[3]); // Top-right
                    triangles.Add(cornerIndices[2]); // Top-left
                }
            }
        }

        // Assign the data to the mesh
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        // Attach mesh to MeshFilter
        GetComponent<MeshFilter>().mesh = mesh;
        SaveMesh(mesh, "HeatMap1");
    }

    public static void SaveMesh(Mesh mesh, string fileName)
    {
        string path = Application.dataPath + "/" + fileName + ".json";
        MeshData data = new MeshData(mesh);
        string json = JsonUtility.ToJson(data);
        File.WriteAllText(path, json);
        Debug.Log("Mesh saved to: " + path);
    }
    class MeshData
    {
        public Vector3[] vertices;
        public int[] triangles;

        public MeshData(Mesh mesh)
        {
            vertices = mesh.vertices;
            triangles = mesh.triangles;
        }

        public Mesh ToMesh()
        {
            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            return mesh;
        }
    }

    public static Mesh LoadMesh(string fileName)
    {
        string path = Application.dataPath + "/" + fileName + ".json";
        if (!File.Exists(path))
        {
            Debug.LogError("File not found: " + path);
            return null;
        }

        string json = File.ReadAllText(path);
        MeshData data = JsonUtility.FromJson<MeshData>(json);
        return data.ToMesh();
    }
}
