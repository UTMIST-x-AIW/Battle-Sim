using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [SerializeField] private GameObject prefab; // Prefab to spawn
    [SerializeField] private HeatMapData heatmapData; // Heatmap data for regular spawn probabilities
    [SerializeField] private HeatMapData extraHeatmapData; // Heatmap data for extra objects spawn probabilities
    [SerializeField, Range(0.01f, 5f)] private float spawnInterval = 1f; // Time between spawn attempts
    [SerializeField] private int maxNumOfSpawns = 10; // Maximum number of spawned objects
    [SerializeField] private int numExtraObjects = 5; // Number of extra objects to spawn initially that won't respawn
    [SerializeField, Range(0.01f, 2f)] private float checkInterval = 0.5f; // How often to check for replenishment (seconds)
    [SerializeField] private bool useDistinctHeatmaps = true; // Whether to use separate heatmaps for regular and extra objects

    private GameObject prefabParent; // Parent object for organizing spawned prefabs
    private GameObject extraPrefabParent; // Parent for extra objects that don't respawn
    private bool isSpawning = false; // Flag to track if a spawn is in progress
    private List<TilePosData.TilePos> highProbabilityPositions; // Cache of good spawn positions for regular objects
    private List<TilePosData.TilePos> extraHighProbabilityPositions; // Cache of good spawn positions for extra objects
    private float nextCheckTime = 0f; // Time of next replenishment check

    private void OnEnable()
    {
        // Validate required references
        if (prefab == null || heatmapData == null)
        {
            Debug.LogError("Error: Prefab or HeatMapData are uninitialized", this);
            enabled = false; // Disable the script if critical references are missing
        }
        
        // For extra heatmap, only check if we're using distinct heatmaps
        if (useDistinctHeatmaps && extraHeatmapData == null)
        {
            Debug.LogWarning("Extra heatmap data is null but useDistinctHeatmaps is true. Will use regular heatmap for both.", this);
        }
    }

    private void Start()
    {
        // Create a parent object to organize spawned prefabs
        prefabParent = GameObject.Find($"{prefab.name} Parent");
        if (prefabParent == null) prefabParent = new GameObject($"{prefab.name} Parent");
        
        // Create a parent for extra objects
        extraPrefabParent = GameObject.Find($"{prefab.name} Extra Parent");
        if (extraPrefabParent == null) extraPrefabParent = new GameObject($"{prefab.name} Extra Parent");
        
        // Pre-calculate and cache high probability positions
        CacheHighProbabilityPositions();
        
        // Initial population of objects
        StartCoroutine(InitialSpawning());
    }

    private void Update()
    {
        // Only check periodically instead of every frame
        if (Time.time >= nextCheckTime)
        {
            nextCheckTime = Time.time + checkInterval;
            
            // Check if we need to spawn more regular objects (not counting extras)
            int currentCount = prefabParent.transform.childCount;
            if (currentCount < maxNumOfSpawns && !isSpawning)
            {
                // Spawn objects to reach the desired count
                int objectsToSpawn = maxNumOfSpawns - currentCount;
                StartCoroutine(ReplenishSpawns(objectsToSpawn));
            }
        }
    }

    private void CacheHighProbabilityPositions()
    {
        // Cache positions for regular spawns
        highProbabilityPositions = CacheProbabilityPositionsForHeatmap(heatmapData, maxNumOfSpawns * 2);
        
        // Cache positions for extra objects - use specific heatmap if available, otherwise use the regular one
        HeatMapData extraMap = useDistinctHeatmaps && extraHeatmapData != null ? extraHeatmapData : heatmapData;
        extraHighProbabilityPositions = CacheProbabilityPositionsForHeatmap(extraMap, numExtraObjects * 2);
    }
    
    private List<TilePosData.TilePos> CacheProbabilityPositionsForHeatmap(HeatMapData map, int neededPositions)
    {
        // Get all tile positions
        var allPositions = map.tilePosData.TilePositions.ToList();
        
        // Pre-filter positions with decent spawn probability
        var highProbPositions = new List<TilePosData.TilePos>();
        
        foreach (var tile in allPositions)
        {
            float probability = map.GetValue(tile.pos);
            if (probability > 50f) // Only keep positions with >50% spawn probability
            {
                highProbPositions.Add(tile);
            }
        }
        
        // If we don't have enough high probability positions, use all positions
        if (highProbPositions.Count < neededPositions)
        {
            highProbPositions = allPositions;
        }
        
        // Shuffle the array for random access
        Shuffle(highProbPositions);
        
        return highProbPositions;
    }

    private IEnumerator InitialSpawning()
    {
        // Ensure tile positions are available for regular objects
        if (highProbabilityPositions == null || highProbabilityPositions.Count == 0)
        {
            Debug.LogError("Error: No valid spawn positions available for regular objects", this);
            yield break;
        }
        
        // Ensure tile positions are available for extra objects
        if (extraHighProbabilityPositions == null || extraHighProbabilityPositions.Count == 0)
        {
            Debug.LogError("Error: No valid spawn positions available for extra objects", this);
            yield break;
        }

        isSpawning = true;
        int regularPositionIndex = 0;
        int extraPositionIndex = 0;
        int numAttempts = 0;
        const int maxAttempts = 100; // Safety limit
        
        // First spawn the regular objects
        while (prefabParent.transform.childCount < maxNumOfSpawns && numAttempts < maxAttempts)
        {
            // Get the next position
            var tile = highProbabilityPositions[regularPositionIndex];
            regularPositionIndex = (regularPositionIndex + 1) % highProbabilityPositions.Count; // Wrap around
            
            numAttempts++;
            
            // Calculate spawn probability and compare with a random value
            float spawnProbability = heatmapData.GetValue(tile.pos);
            float randomVal = Random.Range(0f, 100f);

            if (spawnProbability > randomVal)
            {
                // Spawn the regular prefab at the tile's position
                SpawnPrefab(tile.pos, false);

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
        
        // Then spawn the extra objects that won't respawn
        numAttempts = 0;
        while (extraPrefabParent.transform.childCount < numExtraObjects && numAttempts < maxAttempts)
        {
            // Get the next position from the extra positions
            var tile = extraHighProbabilityPositions[extraPositionIndex];
            extraPositionIndex = (extraPositionIndex + 1) % extraHighProbabilityPositions.Count; // Wrap around
            
            numAttempts++;
            
            // Calculate spawn probability using the appropriate heatmap
            HeatMapData mapToUse = useDistinctHeatmaps && extraHeatmapData != null ? extraHeatmapData : heatmapData;
            float spawnProbability = mapToUse.GetValue(tile.pos);
            float randomVal = Random.Range(0f, 100f);

            if (spawnProbability > randomVal)
            {
                // Spawn the extra prefab at the tile's position
                SpawnPrefab(tile.pos, true);

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
            // Get the next position from regular positions
            var tile = highProbabilityPositions[positionIndex];
            positionIndex = (positionIndex + 1) % highProbabilityPositions.Count; // Wrap around
            
            // Calculate spawn probability and compare with a random value
            float spawnProbability = heatmapData.GetValue(tile.pos);
            float randomVal = Random.Range(0f, 100f);

            if (spawnProbability > randomVal)
            {
                // Spawn the regular prefab at the tile's position (never respawn extras)
                SpawnPrefab(tile.pos, false);
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

    private void SpawnPrefab(Vector2 position, bool isExtra)
    {
        // Check if there's already something at this position
        Collider2D existingObject = Physics2D.OverlapCircle(position, 0.5f);
        if (existingObject != null)
        {
            return; // Skip spawning to avoid overlaps
        }
        
        // Instantiate the prefab and set its parent
        GameObject spawnedPrefab = Instantiate(prefab, position, Quaternion.identity);
        
        // Assign to the appropriate parent based on whether it's an extra object
        if (isExtra)
        {
            spawnedPrefab.transform.SetParent(extraPrefabParent.transform, false);
        }
        else
        {
            spawnedPrefab.transform.SetParent(prefabParent.transform, false);
        }
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