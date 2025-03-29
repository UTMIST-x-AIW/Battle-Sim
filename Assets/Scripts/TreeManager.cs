using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeManager : MonoBehaviour
{
    [Header("Tree Settings")]
    public GameObject tree1Prefab;
    public GameObject tree2Prefab;
    public int initialTreeCount = 10;
    public float respawnDelay = 5f; // Time in seconds before a new tree spawns after one is destroyed
    
    [Header("Spawn Area")]
    public PolygonCollider2D spawnArea; // Usually the same as the floor/playable area
    
    // Track the current number of trees in the scene
    private int currentTreeCount = 0;
    
    private void Start()
    {
        // Find the floor collider if not assigned
        if (spawnArea == null)
        {
            GameObject floor = GameObject.FindGameObjectWithTag("Floor");
            if (floor != null)
            {
                spawnArea = floor.GetComponent<PolygonCollider2D>();
            }
        }
        
        if (spawnArea == null)
        {
            Debug.LogError("No spawn area assigned and couldn't find a Floor object with PolygonCollider2D!");
            return;
        }
        
        // Set up the tree destroyed event listener
        TreeHealth.OnTreeDestroyed += HandleTreeDestroyed;
        
        // Spawn initial trees
        SpawnInitialTrees();
    }
    
    private void OnDestroy()
    {
        // Clean up event listener
        TreeHealth.OnTreeDestroyed -= HandleTreeDestroyed;
    }
    
    private void SpawnInitialTrees()
    {
        for (int i = 0; i < initialTreeCount; i++)
        {
            SpawnTree();
        }
    }
    
    private void HandleTreeDestroyed()
    {
        currentTreeCount--;
        // Start respawn timer
        StartCoroutine(RespawnTreeAfterDelay());
    }
    
    private IEnumerator RespawnTreeAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        SpawnTree();
    }
    
    private void SpawnTree()
    {
        // Choose a random position within the spawn area
        Vector2 randomPosition = GetRandomPointInSpawnArea();
        
        // Choose between tree1 and tree2 randomly
        GameObject treePrefab = Random.value < 0.5f ? tree1Prefab : tree2Prefab;
        
        // Spawn the tree
        GameObject tree = Instantiate(treePrefab, randomPosition, Quaternion.identity);
        
        // Set the tag and ensure it has a TreeHealth component
        tree.tag = "Tree";
        if (tree.GetComponent<TreeHealth>() == null)
        {
            TreeHealth treeHealth = tree.AddComponent<TreeHealth>();
        }
        
        // Ensure the tree has a collider
        if (tree.GetComponent<Collider2D>() == null)
        {
            BoxCollider2D collider = tree.AddComponent<BoxCollider2D>();
            collider.isTrigger = false;
            
            // Adjust collider size based on the sprite if available
            SpriteRenderer renderer = tree.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                collider.size = renderer.bounds.size * 0.8f;
            }
        }
        
        currentTreeCount++;
    }
    
    private Vector2 GetRandomPointInSpawnArea()
    {
        // Get the bounds of the spawn area
        Bounds bounds = spawnArea.bounds;
        
        // Try to find a valid point within the spawn area
        int maxAttempts = 30;
        for (int i = 0; i < maxAttempts; i++)
        {
            // Generate a random point within the bounds
            Vector2 randomPoint = new Vector2(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y)
            );
            
            // Check if the point is within the polygon collider
            if (spawnArea.OverlapPoint(randomPoint))
            {
                // Check for spacing from other trees
                bool tooCloseToOtherTree = false;
                Collider2D[] nearbyObjects = Physics2D.OverlapCircleAll(randomPoint, 2.0f);
                
                foreach (Collider2D collider in nearbyObjects)
                {
                    if (collider.CompareTag("Tree"))
                    {
                        tooCloseToOtherTree = true;
                        break;
                    }
                }
                
                if (!tooCloseToOtherTree)
                {
                    return randomPoint;
                }
            }
        }
        
        // If we couldn't find a good point after max attempts, just return a point within the bounds
        return new Vector2(
            Random.Range(bounds.min.x + 2f, bounds.max.x - 2f),
            Random.Range(bounds.min.y + 2f, bounds.max.y - 2f)
        );
    }
} 