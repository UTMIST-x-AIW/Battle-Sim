using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TextureSpawner : MonoBehaviour
{
    [SerializeField] GameObject Prefab;
    private Color color;
    private Transform Parent_Transform = null;
    private Vector2 pixelCoordinate = new Vector2(0.5f, 0.5f); // Normalized coordinate (0-1) to sample the color
    private RenderTexture renderTexture;
    GameObject Parent_GameObject;
    [SerializeField, Range(0, 100)] private int MaxNumofSpawns;
    [SerializeField, Range(1, 10)] private int SpawnInterval;
    Material SpawnMaterial;
    [SerializeField] bool start_spawning = false;
    private Dictionary<GameObject, Color> colorcache = new Dictionary<GameObject, Color>();
    

    private Camera renderCamera;
    private void Awake()
    {
        Parent_GameObject = GameObject.Find(Prefab.name+ " SpawnsContainer");
        if (Parent_GameObject == null)   Parent_GameObject = new GameObject(Prefab.name + " SpawnsContainer");
        Parent_Transform = Parent_GameObject.transform;
        SpawnMaterial = gameObject.GetComponent<MeshRenderer>().material;
        renderTexture = new RenderTexture(256, 256, 24);
        renderTexture.Create();
        renderCamera = new GameObject("RenderCamera").AddComponent<Camera>();
        renderCamera.enabled = false;
        renderCamera.targetTexture = renderTexture;
    }
    void Start()
    {

        color = GetObjectColorFromShader(this.gameObject, transform.position);
        if (start_spawning) StartCoroutine(MakeSpawnPoints());
        OnDestroy();
    }

    void Update()
    {
        if (Parent_GameObject == null || Parent_Transform == null)
        {
            Debug.LogError("Parent GameObject or Transform is null!");
            return;
        }
        if (Parent_GameObject.transform.childCount == MaxNumofSpawns) return;
    }

    Color GetObjectColorFromShader(GameObject obj, Vector2 uvCoords)
    {
        Renderer renderer = GetComponent<Renderer>();
        Material material = renderer.material;
        Texture2D texture2D = material.mainTexture as Texture2D;

        if (texture2D != null)
        {
            Color pixelcolor = texture2D.GetPixelBilinear(uvCoords.x, uvCoords.y);
            return pixelcolor;
        }
        return Color.red;
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

    IEnumerator MakeSpawnPoints()
    {
        Debug.Log("Color greyscales: "+ color.grayscale);
        float spawnProbability = 0.001f * color.grayscale; // Grayscale value (0-1)
        Debug.Log("Spawn Probability: "+ spawnProbability);
        float t = Random.value;
        Debug.Log("Random value " + t);

        // Randomly spawn based on the probability
        if (t < spawnProbability)
        {

            if (SpawnMaterial.shader.name == "Unlit/KaiShader")
            {
                if (SpawnMaterial.GetInt("_KaiSpawnMapEnabled") == 1)
                {
                    GameObject Kai = Instantiate(Prefab, this.transform.position, Quaternion.identity);
                    Debug.Log("Kai was instantiated");
                    Kai.transform.SetParent(Parent_GameObject.transform, true);
                }
            }
            else if (SpawnMaterial.shader.name == "Unlit/AlbertShader")
            {
                if (SpawnMaterial.GetInt("_AlbertSpawnMapEnabled") == 1)
                {
                    GameObject Albert = Instantiate(Prefab, this.transform.position, Quaternion.identity);
                    Debug.Log("Albert was instantiated");
                    Albert.transform.SetParent(Parent_Transform, true);
                }
            }
            else if (SpawnMaterial.shader.name == "Unlit/ObjectShader")
            {
                GameObject Object = Instantiate(Prefab, this.transform.position, Quaternion.identity);
                Debug.Log($"{Prefab.name} was instantiated");
                Object.transform.SetParent(Parent_Transform, true);
            }

            yield return new WaitForSeconds(SpawnInterval);
        }
    }
    //private void OnDisable()
    //{
    //    while (Parent_Transform.childCount > 0)
    //    {
    //        DestroyImmediate(Parent_Transform.GetChild(0).gameObject);
    //    }
    //    Destroy(Parent_GameObject);
    //}
}