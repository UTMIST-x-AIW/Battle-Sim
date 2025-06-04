using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [SerializeField] private GameObject prefab; // Prefab to spawn
    [SerializeField] private HeatMapData heatmapData; // Heatmap data for regular spawn probabilities
    [SerializeField] private HeatMapData extraHeatmapData; // Heatmap data for extra objects spawn probabilities
    [SerializeField, Range(0.01f, 5f)] private float spawnInterval = 0.25f; // Time between spawn attempts
    [SerializeField] private int maxNumOfSpawns = 10; // Maximum number of spawned objects
    [SerializeField] private int numExtraObjects = 5; // Number of extra objects to spawn initially that won't respawn
    [SerializeField, Range(0f, 1f)] private float extraObjectRespawnRate = 0f; // 0 = never respawn, 1 = respawn at full probability
    [SerializeField] private bool useDistinctHeatmaps = true; // Whether to use separate heatmaps for regular and extra objects
    [SerializeField] private float extraObjectMinutesToExtinction = 0f; // Time for respawn rate to go from 1 to 0 (0 = never change)
    [SerializeField] private bool debugLogging = false; // Toggle for debug logging

    // Public accessors so other systems or UI can tweak spawn timing at runtime
    public float SpawnInterval
    {
        get => spawnInterval;
        set => spawnInterval = Mathf.Clamp(value, 0.01f, 5f);
    }

    public float ExtraObjectRespawnRate
    {
        get => extraObjectRespawnRate;
        set => extraObjectRespawnRate = Mathf.Clamp01(value);
    }

    private GameObject prefabParent; // Parent object for organizing spawned prefabs
    private GameObject extraPrefabParent; // Parent for extra objects that don't respawn
    private int regularIndex = 0;
    private int extraIndex = 0;
    private List<TilePosData.TilePos> highProbabilityPositions; // Cache of good spawn positions for regular objects
    private List<TilePosData.TilePos> extraHighProbabilityPositions; // Cache of good spawn positions for extra objects
    private float simulationStartTime; // When the simulation started, used for extinction timing
    private float initialRespawnRate; // Initial value of respawn rate

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

        // Store the initial respawn rate
        initialRespawnRate = extraObjectRespawnRate;
    }

    private void Start()
    {
        // Record simulation start time
        simulationStartTime = Time.time;

        // If we have a non-zero extinction time, start with respawn rate at 1
        if (extraObjectMinutesToExtinction > 0)
        {
            extraObjectRespawnRate = 1.0f;
            if (debugLogging) Debug.Log($"[Spawner] Starting with extraObjectRespawnRate=1.0 (extinction time={extraObjectMinutesToExtinction}m)");
        }
        
        // Create a parent object to organize spawned prefabs
        prefabParent = GameObject.Find($"{prefab.name} Parent");
        if (prefabParent == null) prefabParent = new GameObject($"{prefab.name} Parent");

        // Create a parent for extra objects
        extraPrefabParent = GameObject.Find($"{prefab.name} Extra Parent");
        if (extraPrefabParent == null) extraPrefabParent = new GameObject($"{prefab.name} Extra Parent");

        // Prewarm the object pool so spawning doesn't instantiate during gameplay
        ObjectPoolManager.PrewarmPool(prefab, maxNumOfSpawns + numExtraObjects);
        
        // Pre-calculate and cache high probability positions
        CacheHighProbabilityPositions();
        
        // Start continuous spawning loop
        StartCoroutine(SpawnLoop());
    }

    private void Update()
    {
        // Update extinction rate if configured
        if (extraObjectMinutesToExtinction > 0)
        {
            // Calculate how much time has passed since the start
            float elapsedTime = Time.time - simulationStartTime;
            float extinctionTimeInSeconds = extraObjectMinutesToExtinction * 60f;
            
            // Calculate the new respawn rate (linear decrease from 1 to 0)
            if (elapsedTime < extinctionTimeInSeconds)
            {
                // Linear interpolation: 1.0 at time 0, 0.0 at extinction time
                extraObjectRespawnRate = 1.0f - (elapsedTime / extinctionTimeInSeconds);
                
                if (debugLogging && Time.frameCount % 300 == 0) // Log every 300 frames to avoid spam
                {
                    Debug.Log($"[Spawner] Current extraObjectRespawnRate={extraObjectRespawnRate:F3}, elapsed={elapsedTime:F1}s/{extinctionTimeInSeconds:F1}s");
                }
            }
            else
            {
                // After extinction time, set to 0
                extraObjectRespawnRate = 0f;
                
                if (debugLogging && extraObjectRespawnRate > 0) // Only log when it first reaches 0
                {
                    Debug.Log("[Spawner] Extinction period complete, respawn rate set to 0");
                }
            }
        }
        
        // Spawning is handled by a coroutine
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
        

        
        // Shuffle the array for random access
        Shuffle(highProbPositions);

        return highProbPositions;
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            AttemptRegularSpawn();
            AttemptExtraSpawn();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void AttemptRegularSpawn()
    {
        if (prefabParent.transform.childCount >= maxNumOfSpawns) return;
        if (highProbabilityPositions == null || highProbabilityPositions.Count == 0) return;

        var tile = highProbabilityPositions[regularIndex];
        regularIndex = (regularIndex + 1) % highProbabilityPositions.Count;

        float probability = heatmapData.GetValue(tile.pos);
        if (probability > Random.Range(0f, 100f))
        {
            if (SpawnPrefab(tile.pos, false) && debugLogging)
            {
                Debug.Log($"[Spawner] Spawned regular object {prefabParent.transform.childCount}/{maxNumOfSpawns}");
            }
        }
    }

    private void AttemptExtraSpawn()
    {
        if (extraObjectRespawnRate <= 0f) return;
        if (extraPrefabParent.transform.childCount >= numExtraObjects) return;
        if (extraHighProbabilityPositions == null || extraHighProbabilityPositions.Count == 0) return;

        var tile = extraHighProbabilityPositions[extraIndex];
        extraIndex = (extraIndex + 1) % extraHighProbabilityPositions.Count;

        HeatMapData map = useDistinctHeatmaps && extraHeatmapData != null ? extraHeatmapData : heatmapData;
        float probability = map.GetValue(tile.pos) * extraObjectRespawnRate;

        if (probability > Random.Range(0f, 100f))
        {
            if (SpawnPrefab(tile.pos, true) && debugLogging)
            {
                Debug.Log($"[Spawner] Spawned extra object {extraPrefabParent.transform.childCount}/{numExtraObjects}");
            }
        }
    }


    private bool SpawnPrefab(Vector2 position, bool isExtra)
    {
        // Check if there's already something at this position
        Collider2D existingObject = Physics2D.OverlapCircle(position, 0.5f);
        if (existingObject != null)
        {
            return false; // Skip spawning to avoid overlaps
        }
        
        // Instantiate the prefab and set its parent
        GameObject spawnedPrefab = ObjectPoolManager.SpawnObject(prefab, position, Quaternion.identity);
        
        // Assign to the appropriate parent based on whether it's an extra object
        if (isExtra)
        {
            spawnedPrefab.transform.SetParent(extraPrefabParent.transform, false);
        }
        else
        {
        spawnedPrefab.transform.SetParent(prefabParent.transform, false);
        }
        
        return true;
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