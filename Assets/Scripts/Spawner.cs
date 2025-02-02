using UnityEngine;

public class TextureSpawner : MonoBehaviour
{
    public Transform KaiPrefab; 
    public Transform AlbertPrefab;
    // Prefab to spawn
  //  public Transform AlbertPrefab; // Prefab to spawn
    //public int spawnResolution = 10; // How many spawn points to check per axis

    private Material SpawnMap;
    private int SpawnCounter = 1;
    private Color color;
    private  Transform KaiParent;
    private Transform AlbertParent;
    public GameObject targetObject; // The GameObject whose color you want to check
    public Vector2 pixelCoordinate = new Vector2(0.5f, 0.5f); // Normalized coordinate (0-1) to sample the color

    private RenderTexture renderTexture;
    GameObject KaiParent_GameObject;
    GameObject AlbertParent_GameObject;

    
    private Camera renderCamera;
    void Start()
    {
        KaiParent_GameObject = GameObject.Find("Kai's Parent");
        AlbertParent_GameObject = GameObject.Find("Albert's Parent");
        targetObject = this.gameObject;
        KaiParent = KaiParent_GameObject.GetComponent<Transform>();
        AlbertParent = AlbertParent_GameObject.GetComponent<Transform>();
        renderTexture = new RenderTexture(256, 256, 24);
        renderTexture.Create();
        // Create a temporary camera to render the object
        renderCamera = new GameObject("RenderCamera").AddComponent<Camera>();
        renderCamera.enabled = false;
        renderCamera.targetTexture = renderTexture;

        color = GetObjectColor(targetObject);
        OnDestroy();
    }

    void LateUpdate()
    {
        if (KaiParent_GameObject.transform.childCount == 50) return;
        if (AlbertParent_GameObject.transform.childCount == 50) return;
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
    void CalculateSpawnPoints()
    {
       
        float spawnProbability = 0.001f*color.grayscale; // Grayscale value (0-1)

        // Randomly spawn based on the probability
        if (Random.value < spawnProbability)
        {
            if (SpawnCounter != 2)
            {
                Transform Kai = Instantiate(KaiPrefab, this.transform.position, Quaternion.identity);
                Transform Albert = Instantiate(AlbertPrefab, this.transform.position, Quaternion.identity);
                Kai.SetParent(KaiParent, true);
                Albert.SetParent(AlbertParent, true);
                SpawnCounter++;
            }
        }
    }
}