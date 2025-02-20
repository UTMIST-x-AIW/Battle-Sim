using UnityEngine;
using System.Collections;

public class FoodManager : MonoBehaviour
{
    [Header("Food Prefab")]
    public GameObject cherryPrefab;
    
    [Header("Spawn Settings")]
    public float spawnInterval = 2f;  // Time between spawn attempts
    public int maxCherries = 20;      // Maximum number of cherries in the scene
    
    private PolygonCollider2D floorCollider;
    private Bounds floorBounds;
    
    private void Start()
    {
        // Get floor collider
        floorCollider = GameObject.FindGameObjectWithTag("Floor").GetComponent<PolygonCollider2D>();
        if (floorCollider != null)
        {
            floorBounds = floorCollider.bounds;
        }
        
        // Start spawning coroutine
        StartCoroutine(SpawnFood());
    }
    
    private IEnumerator SpawnFood()
    {
        WaitForSeconds wait = new WaitForSeconds(spawnInterval);
        
        while (true)
        {
            // Only spawn if we're under the maximum
            if (GameObject.FindGameObjectsWithTag("Cherry").Length < maxCherries)
            {
                SpawnCherry();
            }
            
            yield return wait;
        }
    }
    
    private void SpawnCherry()
    {
        if (floorCollider == null) return;
        
        // Try to find a valid spawn position
        for (int attempts = 0; attempts < 10; attempts++)
        {
            // Generate random position within floor bounds
            Vector2 randomPos = new Vector2(
                Random.Range(floorBounds.min.x, floorBounds.max.x),
                Random.Range(floorBounds.min.y, floorBounds.max.y)
            );
            
            // Check if position is valid (inside floor collider)
            if (floorCollider.OverlapPoint(randomPos))
            {
                // Spawn cherry
                GameObject cherry = Instantiate(cherryPrefab, randomPos, Quaternion.identity);
                cherry.tag = "Cherry";  // Make sure the prefab has this tag
                break;
            }
        }
    }
} 