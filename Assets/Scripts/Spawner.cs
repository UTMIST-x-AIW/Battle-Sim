using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [SerializeField] private GameObject prefab; // Prefab to spawn
    [SerializeField] private HeatMapData heatmapData; // Heatmap data for spawn probabilities
    [SerializeField, Range(0.1f, 5f)] private float spawnInterval = 1f; // Time between spawn attempts
    [SerializeField] private int maxNumOfSpawns = 10; // Maximum number of spawned objects
    [SerializeField, Range(0.1f, 2f)] private float checkInterval = 0.5f; // How often to check for replenishment (seconds)

    private GameObject prefabParent; // Parent object for organizing spawned prefabs
    private bool isSpawning = false; // Flag to track if a spawn is in progress
    private List<TilePosData.TilePos> highProbabilityPositions; // Cache of good spawn positions
    private float nextCheckTime = 0f; // Time of next replenishment check

    private void OnEnable()
    {
        // Validate required references
        if (prefab == null || heatmapData == null)
        {
            Debug.LogError("Error: Prefab or HeatMapData are uninitialized", this);
            enabled = false; // Disable the script if critical references are missing
        }
    }

    private void Start()
    {
        // Create a parent object to organize spawned prefabs
        prefabParent = GameObject.Find($"{prefab.name} Parent");
        if (prefabParent == null) prefabParent = new GameObject($"{prefab.name} Parent");
        
        // Pre-calculate and cache high probability positions
        CacheHighProbabilityPositions();
        
        // Initial population of trees
        StartCoroutine(InitialSpawning());
    }

    private void Update()
    {
        // Only check periodically instead of every frame
        if (Time.time >= nextCheckTime)
        {
            nextCheckTime = Time.time + checkInterval;
            
            // Check if we need to spawn more trees
            int currentCount = prefabParent.transform.childCount;
            if (currentCount < maxNumOfSpawns && !isSpawning)
            {
                // Spawn trees to reach the desired count
                int treesToSpawn = maxNumOfSpawns - currentCount;
                StartCoroutine(ReplenishSpawns(treesToSpawn));
            }
        }
    }

    private void CacheHighProbabilityPositions()
    {
        // Get all tile positions
        var allPositions = heatmapData.tilePosData.TilePositions.ToList();
        
        // Pre-filter positions with decent spawn probability
        highProbabilityPositions = new List<TilePosData.TilePos>();
        
        foreach (var tile in allPositions)
        {
            float probability = heatmapData.GetValue(tile.pos);
            if (probability > 50f) // Only keep positions with >50% spawn probability
            {
                highProbabilityPositions.Add(tile);
            }
        }
        
        // If we don't have enough high probability positions, use all positions
        if (highProbabilityPositions.Count < maxNumOfSpawns * 2)
        {
            highProbabilityPositions = allPositions;
        }
        
        // Shuffle the array for random access
        Shuffle(highProbabilityPositions);
    }

    private IEnumerator InitialSpawning()
    {
        // Ensure tile positions are available
        if (highProbabilityPositions == null || highProbabilityPositions.Count == 0)
        {
            Debug.LogError("Error: No valid spawn positions available", this);
            yield break;
        }

        isSpawning = true;
        int positionIndex = 0;
        int numAttempts = 0;
        const int maxAttempts = 100; // Safety limit

        // Continue spawning until the maximum number of spawns is reached
        while (prefabParent.transform.childCount < maxNumOfSpawns && numAttempts < maxAttempts)
        {
            // Get the next position
            var tile = highProbabilityPositions[positionIndex];
            positionIndex = (positionIndex + 1) % highProbabilityPositions.Count; // Wrap around
            
            numAttempts++;
            
            // Calculate spawn probability and compare with a random value
            float spawnProbability = heatmapData.GetValue(tile.pos);
            float randomVal = Random.Range(0f, 100f);

            if (spawnProbability > randomVal)
            {
                // Spawn the prefab at the tile's position
                SpawnPrefab(tile.pos);

                // Reset attempts counter on successful spawn
                numAttempts = 0;
                
                // Wait for the specified interval before the next spawn attempt
                yield return new WaitForSeconds(spawnInterval);
            }
            
            // Yield every few attempts to avoid freezing
            if (numAttempts % 10 == 0)
            {
                yield return null;
            }
        }

        isSpawning = false;
    }

    private IEnumerator ReplenishSpawns(int count)
    {
        if (count <= 0) yield break;
        
        isSpawning = true;
        int spawned = 0;
        int positionIndex = Random.Range(0, highProbabilityPositions.Count);
        
        while (spawned < count)
        {
            // Get the next position
            var tile = highProbabilityPositions[positionIndex];
            positionIndex = (positionIndex + 1) % highProbabilityPositions.Count; // Wrap around
            
            // Calculate spawn probability and compare with a random value
            float spawnProbability = heatmapData.GetValue(tile.pos);
            float randomVal = Random.Range(0f, 100f);

            if (spawnProbability > randomVal)
            {
                // Spawn the prefab at the tile's position
                SpawnPrefab(tile.pos);
                spawned++;
                
                // Wait for the specified interval
                yield return new WaitForSeconds(spawnInterval);
            }
            
            // Occasionally yield to avoid freezing
            if (positionIndex % 10 == 0)
            {
                yield return null;
            }
        }

        isSpawning = false;
    }

    private void SpawnPrefab(Vector2 position)
    {
        // Check if there's already something at this position
        Collider2D existingObject = Physics2D.OverlapCircle(position, 0.5f);
        if (existingObject != null)
        {
            return; // Skip spawning to avoid overlaps
        }
        
        // Instantiate the prefab and set its parent
        GameObject spawnedPrefab = Instantiate(prefab, position, Quaternion.identity);
        spawnedPrefab.transform.SetParent(prefabParent.transform, false);
    }

    void Shuffle<T>(List<T> list)
    {
        int n = list.Count;
        for (int i = n - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}