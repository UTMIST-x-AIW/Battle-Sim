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

    private IEnumerator InitialSpawning()
    {
        // Ensure tile positions are available
        if (heatmapData.tilePosData == null || heatmapData.tilePosData.TilePositions == null)
        {
            Debug.LogError("Error: TilePosData or TilePositions are uninitialized", this);
            yield break;
        }

        isSpawning = true;
        
        // Get and shuffle tile positions for regular objects
        List<TilePosData.TilePos> regularTilePositions = heatmapData.tilePosData.TilePositions.ToList();
        Shuffle(regularTilePositions);
        
        // Get and shuffle tile positions for extra objects
        List<TilePosData.TilePos> extraTilePositions;
        if (useDistinctHeatmaps && extraHeatmapData != null)
        {
            extraTilePositions = extraHeatmapData.tilePosData.TilePositions.ToList();
        }
        else
        {
            extraTilePositions = new List<TilePosData.TilePos>(regularTilePositions);
        }
        Shuffle(extraTilePositions);
        
        // First spawn the regular objects
        int regularAttempts = 0;
        const int maxAttempts = 100; // Safety limit
        
        int regularIdx = 0;
        while (prefabParent.transform.childCount < maxNumOfSpawns && regularAttempts < maxAttempts)
        {
            // Get the next position
            var tile = regularTilePositions[regularIdx];
            regularIdx = (regularIdx + 1) % regularTilePositions.Count; // Wrap around
            
            regularAttempts++;
            
            // Calculate spawn probability and compare with a random value
            float spawnProbability = heatmapData.GetValue(tile.pos);
            float randomVal = Random.Range(0f, 100f);

            if (spawnProbability > randomVal)
            {
                // Spawn the regular prefab at the tile's position
                SpawnPrefab(tile.pos, false);

                // Reset attempts counter on successful spawn
                regularAttempts = 0;
                
                // Wait for the specified interval before the next spawn attempt
                yield return new WaitForSeconds(spawnInterval);
            }
            
            // Yield every few attempts to avoid freezing
            if (regularAttempts % 10 == 0)
            {
                yield return null;
            }
        }
        
        // Then spawn the extra objects that won't respawn
        int extraAttempts = 0;
        int extraIdx = 0;
        
        while (extraPrefabParent.transform.childCount < numExtraObjects && extraAttempts < maxAttempts)
        {
            // Get the next position
            var tile = extraTilePositions[extraIdx];
            extraIdx = (extraIdx + 1) % extraTilePositions.Count; // Wrap around
            
            extraAttempts++;
            
            // Calculate spawn probability using the appropriate heatmap
            HeatMapData mapToUse = useDistinctHeatmaps && extraHeatmapData != null ? extraHeatmapData : heatmapData;
            float spawnProbability = mapToUse.GetValue(tile.pos);
            float randomVal = Random.Range(0f, 100f);

            if (spawnProbability > randomVal)
            {
                // Spawn the extra prefab at the tile's position
                SpawnPrefab(tile.pos, true);

                // Reset attempts counter on successful spawn
                extraAttempts = 0;
                
                // Wait for the specified interval before the next spawn attempt
                yield return new WaitForSeconds(spawnInterval);
            }
            
            // Yield every few attempts to avoid freezing
            if (extraAttempts % 10 == 0)
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
        
        // Get and shuffle tile positions for replenishment
        List<TilePosData.TilePos> tilePositions = heatmapData.tilePosData.TilePositions.ToList();
        Shuffle(tilePositions);
        
        int posIndex = 0;
        
        while (spawned < count)
        {
            // Get the next position
            var tile = tilePositions[posIndex];
            posIndex = (posIndex + 1) % tilePositions.Count; // Wrap around
            
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
            if (posIndex % 10 == 0)
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