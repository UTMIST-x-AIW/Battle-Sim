using System.Collections;
using System.Linq;
using UnityEngine;

public class TextureSpawner : MonoBehaviour
{
    [SerializeField] Transform Prefab;
    private Color color;
    private Transform Parent_Transform = null;
    private Vector2 pixelCoordinate = new Vector2(0.5f, 0.5f); // Normalized coordinate (0-1) to sample the color
    private RenderTexture renderTexture;
    GameObject Parent_GameObject;
    [SerializeField, Range(0, 100)] private int MaxNumofSpawns;
    [SerializeField, Range(1, 10)] private int SpawnInterval;
    Material SpawnMaterial;
    

    private Camera renderCamera;
    void Start()
    {
        Parent_GameObject = GameObject.Find(Prefab.name+ " SpawnedObjectsContainer");
        if (Parent_GameObject == null)
        {
            Parent_GameObject = new GameObject(Prefab.name + " SpawnedObjectsContainer");
        }
        Parent_Transform = Parent_GameObject.transform;
        SpawnMaterial = gameObject.GetComponent<MeshRenderer>().material;
        renderTexture = new RenderTexture(256, 256, 24);
        renderTexture.Create();
        renderCamera = new GameObject("RenderCamera").AddComponent<Camera>();
        renderCamera.enabled = false;
        renderCamera.targetTexture = renderTexture;

        color = GetObjectColor(this.gameObject);
        OnDestroy();
    }

    void LateUpdate()
    {
        if (Parent_GameObject == null || Parent_Transform == null)
        {
            Debug.LogError("Parent GameObject or Transform is null!");
            return;
        }
        if (Parent_GameObject.transform.childCount == MaxNumofSpawns) return;
        StartCoroutine(CalculateSpawnPoints());
    }

    Color GetObjectColor(GameObject obj)
    {
        // Position the camera to fit the object
        Bounds bounds = obj.GetComponent<Renderer>().bounds;
        renderCamera.transform.position = bounds.center + Vector3.forward * bounds.size.magnitude;
        renderCamera.transform.LookAt(bounds.center);

        // Render the object to the RenderTexture
        renderCamera.Render();
        renderCamera.hideFlags = HideFlags.HideAndDontSave;

        // Read the pixel color from the RenderTexture
        RenderTexture.active = renderTexture;
        Texture2D texture2D = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
        texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture2D.Apply();

        // Sample the color at the specified normalized coordinate
        int pixelX = Mathf.FloorToInt(pixelCoordinate.x * renderTexture.width);
        int pixelY = Mathf.FloorToInt(pixelCoordinate.y * renderTexture.height);
        Color pixelColor = texture2D.GetPixel(pixelX, pixelY);

        // Clean up
        RenderTexture.active = null;
        Destroy(texture2D);

        return pixelColor;
    }

    void OnDestroy()
    {
        // Clean up the RenderTexture and temporary camera
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }

        if (renderCamera != null)
        {
            Destroy(renderCamera.gameObject);
        }
    }

    IEnumerator CalculateSpawnPoints()
    {

        float spawnProbability = 0.001f * color.grayscale; // Grayscale value (0-1)

        // Randomly spawn based on the probability
        if (Random.value < spawnProbability)
        {
            if (SpawnMaterial.shader.name == "Unlit/KaiShader")
            {
                if (SpawnMaterial.GetInt("_KaiSpawnMapEnabled") == 1)
                {
                    Transform Kai = Instantiate(Prefab, this.transform.position, Quaternion.identity);
                    Kai.SetParent(Parent_GameObject.transform, true);
                }

            }
            else if (SpawnMaterial.shader.name == "Unlit/AlbertShader")
            {
                if (SpawnMaterial.GetInt("_AlbertSpawnMapEnabled") == 1)
                {
                    Transform Albert = Instantiate(Prefab, this.transform.position, Quaternion.identity);
                    Albert.SetParent(Parent_Transform, true);
                }
            }

            yield return new WaitForSeconds(SpawnInterval);
        }

    }
    private void OnDisable()
    {
        while (Parent_Transform.childCount > 0)
        {
            DestroyImmediate(Parent_Transform.GetChild(0));
        }
        Destroy(Parent_GameObject);
    }
}