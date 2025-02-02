using System.Collections;
using UnityEngine;

public class TextureSpawner : MonoBehaviour
{
    public Transform KaiPrefab;
    public Transform AlbertPrefab;
    private Color color;
    private Transform KaiParent;
    private Transform AlbertParent;
    private Vector2 pixelCoordinate = new Vector2(0.5f, 0.5f); // Normalized coordinate (0-1) to sample the color
    private RenderTexture renderTexture;
    GameObject KaiParent_GameObject;
    GameObject AlbertParent_GameObject;
    [SerializeField, Range(0, 100)] private int MaxNumofSpawns;
    [SerializeField, Range(1, 10)] private int SpawnInterval;
    Material SpawnMaterial;

    private Camera renderCamera;

    void Start()
    {
        KaiParent_GameObject = GameObject.Find("Kai's Parent");
        AlbertParent_GameObject = GameObject.Find("Albert's Parent");
        SpawnMaterial = gameObject.GetComponent<MeshRenderer>().material;
        KaiParent = KaiParent_GameObject.GetComponent<Transform>();
        AlbertParent = AlbertParent_GameObject.GetComponent<Transform>();
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
        if (KaiParent_GameObject.transform.childCount == MaxNumofSpawns) return;
        if (AlbertParent_GameObject.transform.childCount == MaxNumofSpawns) return;
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
                    Transform Kai = Instantiate(KaiPrefab, this.transform.position, Quaternion.identity);
                    Kai.SetParent(KaiParent, true);
                }

            }
            else if (SpawnMaterial.shader.name == "Unlit/AlbertShader")
            {
                if (SpawnMaterial.GetInt("_AlbertSpawnMapEnabled") == 1)
                {
                    Transform albert = Instantiate(AlbertPrefab, this.transform.position, Quaternion.identity);
                    albert.SetParent(AlbertParent, true);
                }
            }

            yield return new WaitForSeconds(SpawnInterval);
        }

    }
}