using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [SerializeField] private GameObject regularPrefab; // Regular prefab to spawn
    [SerializeField] private GameObject extraPrefab; // Extra prefab to spawn
    [SerializeField] private HeatMapData heatmapData; // Heatmap data for regular spawn probabilities
    [SerializeField] private HeatMapData extraHeatmapData; // Heatmap data for extra objects spawn probabilities
    [SerializeField, Range(0.01f, 5f)] private float spawnInterval = 1f; // Time between spawn attempts
    [SerializeField] private int maxNumOfSpawns = 10; // Maximum number of spawned objects
    [SerializeField] private int numExtraObjects = 5; // Number of extra objects to spawn initially that won't respawn
    [SerializeField, Range(0f, 1f)] private float extraObjectRespawnRate = 0f; // 0 = never respawn, 1 = respawn at full probability
    [SerializeField, Range(0.01f, 2f)] private float checkInterval = 0.5f; // How often to check for replenishment (seconds)
    [SerializeField] private bool useDistinctHeatmaps = true; // Whether to use separate heatmaps for regular and extra objects
    [SerializeField] private float extraObjectMinutesToExtinction = 0f; // Time for respawn rate to go from 1 to 0 (0 = never change)
    [SerializeField] private bool debugLogging = false; // Toggle for debug logging

    private bool isSpawningRegular = false; // Flag to track if regular spawn is in progress
    private bool isSpawningExtra = false; // Flag to track if extra spawn is in progress
    private List<TilePosData.TilePos> highProbabilityPositions; // Cache of good spawn positions for regular objects
    private List<TilePosData.TilePos> extraHighProbabilityPositions; // Cache of good spawn positions for extra objects
    private float nextCheckTime = 0f; // Time of next replenishment check
    private float simulationStartTime; // When the simulation started, used for extinction timing
    private float initialRespawnRate; // Initial value of respawn rate

    private void OnEnable()
    {
        // Validate required references
        if (regularPrefab == null || heatmapData == null)
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

        // Pre-calculate and cache high probability positions
        CacheHighProbabilityPositions();

        // Initial population of objects
        StartCoroutine(InitialSpawning());
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

        // Only check periodically instead of every frame
        if (Time.time >= nextCheckTime)
        {
            nextCheckTime = Time.time + checkInterval;

            // Check if we need to spawn more regular objects
            int currentCount = GetCount(regularPrefab);
            if (currentCount < maxNumOfSpawns && !isSpawningRegular)
            {
                // Spawn objects to reach the desired count
                int objectsToSpawn = maxNumOfSpawns - currentCount;
                StartCoroutine(ReplenishSpawns(objectsToSpawn));
            }

            // Check if we need to respawn extra objects (only if respawn rate > 0)
            int currentExtraCount = GetCount(extraPrefab);
            if (extraObjectRespawnRate > 0 && !isSpawningExtra)
            {
                if (currentExtraCount < numExtraObjects)
                {
                    // Spawn extra objects to reach the desired count
                    int extraObjectsToSpawn = numExtraObjects - currentExtraCount;

                    if (debugLogging)
                    {
                        Debug.Log($"[Spawner] Starting extra object replenish: need {extraObjectsToSpawn} more, rate={extraObjectRespawnRate:F3}");
                    }

                    StartCoroutine(ReplenishExtraObjects(extraObjectsToSpawn));
                }
                else if (debugLogging && Time.frameCount % 300 == 0)
                {
                    Debug.Log($"[Spawner] Extra objects at max capacity ({currentExtraCount}/{numExtraObjects})");
                }
            }
            else if (debugLogging && Time.frameCount % 300 == 0 && extraObjectMinutesToExtinction > 0)
            {
                Debug.Log($"[Spawner] Extra spawn check - respawnRate={extraObjectRespawnRate:F3}, isSpawningExtra={isSpawningExtra}, count={currentExtraCount}/{numExtraObjects}");
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
            if (probability > 50f) // Only keep positions with >50% spawn probability //TODO: try making this like 0 instead
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

        isSpawningRegular = true;
        isSpawningExtra = true;
        int regularPositionIndex = 0;
        int extraPositionIndex = 0;
        int regularAttempts = 0;
        int extraAttempts = 0;
        const int maxAttempts = 100; // Safety limit

        // Interleave spawning of regular and extra objects
        while ((GetCount(regularPrefab) < maxNumOfSpawns || GetCount(extraPrefab) < numExtraObjects) &&
               regularAttempts < maxAttempts && extraAttempts < maxAttempts)
        {
            // First attempt to spawn a regular object (if needed)
            if (GetCount(regularPrefab) < maxNumOfSpawns)
            {
                // Get the next position
                var regularTile = highProbabilityPositions[regularPositionIndex];
                regularPositionIndex = (regularPositionIndex + 1) % highProbabilityPositions.Count; // Wrap around

                regularAttempts++;

                // Calculate spawn probability and compare with a random value
                float regularSpawnProbability = heatmapData.GetValue(regularTile.pos);
                float regularRandomVal = Random.Range(0f, 100f);

                if (regularSpawnProbability > regularRandomVal)
                {
                    // Spawn the regular prefab at the tile's position
                    bool regularSuccess = SpawnPrefab(regularTile.pos, false);

                    if (regularSuccess)
                    {
                        // Reset attempts counter on successful spawn
                        regularAttempts = 0;

                        if (debugLogging)
                        {
                            Debug.Log($"[Spawner] Spawned regular object {GetCount(regularPrefab)}/{maxNumOfSpawns}");
                        }

                        // Wait for a short interval before attempting to spawn an extra object
                        yield return new WaitForSeconds(spawnInterval * 0.5f);
                    }
                }
            }

            // Then attempt to spawn an extra object (if needed)
            if (GetCount(extraPrefab) < numExtraObjects)
            {
                // Get the next position from the extra positions
                var extraTile = extraHighProbabilityPositions[extraPositionIndex];
                extraPositionIndex = (extraPositionIndex + 1) % extraHighProbabilityPositions.Count; // Wrap around

                extraAttempts++;

                // Calculate spawn probability using the appropriate heatmap
                HeatMapData mapToUse = useDistinctHeatmaps && extraHeatmapData != null ? extraHeatmapData : heatmapData;
                float extraSpawnProbability = mapToUse.GetValue(extraTile.pos);
                float extraRandomVal = Random.Range(0f, 100f);

                if (extraSpawnProbability > extraRandomVal)
                {
                    // Spawn the extra prefab at the tile's position
                    bool extraSuccess = SpawnPrefab(extraTile.pos, true);

                    if (extraSuccess)
                    {
                        // Reset attempts counter on successful spawn
                        extraAttempts = 0;

                        if (debugLogging)
                        {
                            Debug.Log($"[Spawner] Spawned extra object {GetCount(extraPrefab)}/{numExtraObjects}");
                        }

                        // Wait for the specified interval before the next spawn attempt
                        yield return new WaitForSeconds(spawnInterval * 0.5f);
                    }
                }
            }

            // Check if we've reached both target counts
            if (GetCount(regularPrefab) >= maxNumOfSpawns && GetCount(extraPrefab) >= numExtraObjects)
            {
                break;
            }

            // Yield every few attempts to avoid freezing
            if ((regularAttempts + extraAttempts) % 10 == 0)
            {
                yield return null;
            }
        }

        isSpawningRegular = false;
        isSpawningExtra = false;

        if (debugLogging)
        {
            Debug.Log($"[Spawner] Initial spawning complete: {GetCount(regularPrefab)} regular objects, {GetCount(extraPrefab)} extra objects");
        }
    }

    private IEnumerator ReplenishSpawns(int count)
    {
        if (count <= 0) yield break;

        isSpawningRegular = true;
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
                // Spawn the regular prefab at the tile's position
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

        isSpawningRegular = false;
    }

    private IEnumerator ReplenishExtraObjects(int count)
    {
        if (count <= 0 || extraObjectRespawnRate <= 0)
        {
            if (debugLogging)
            {
                Debug.Log($"[Spawner] Skipping extra replenish: count={count}, rate={extraObjectRespawnRate:F3}");
            }
            yield break;
        }

        isSpawningExtra = true;
        int spawned = 0;
        int attempts = 0;
        int maxAttempts = 100; // Safety limit
        int positionIndex = Random.Range(0, extraHighProbabilityPositions.Count);

        if (debugLogging)
        {
            Debug.Log($"[Spawner] Starting extra replenish for {count} objects, rate={extraObjectRespawnRate:F3}");
        }

        while (spawned < count && attempts < maxAttempts)
        {
            attempts++;

            // Get the next position from extra positions
            var tile = extraHighProbabilityPositions[positionIndex];
            positionIndex = (positionIndex + 1) % extraHighProbabilityPositions.Count; // Wrap around

            // Calculate spawn probability using the appropriate heatmap
            HeatMapData mapToUse = useDistinctHeatmaps && extraHeatmapData != null ? extraHeatmapData : heatmapData;
            float spawnProbability = mapToUse.GetValue(tile.pos);

            // Apply the respawn rate to reduce the probability
            spawnProbability *= extraObjectRespawnRate;

            // Compare with a random value
            float randomVal = Random.Range(0f, 100f);

            if (debugLogging && attempts % 10 == 0)
            {
                Debug.Log($"[Spawner] Extra spawn attempt {attempts}: prob={spawnProbability:F1}, roll={randomVal:F1}, pos={tile.pos}");
            }

            if (spawnProbability > randomVal)
            {
                // Spawn the extra prefab at the tile's position
                bool success = SpawnPrefab(tile.pos, true);

                if (success)
                {
                    spawned++;
                    if (debugLogging)
                    {
                        Debug.Log($"[Spawner] Successfully spawned extra object {spawned}/{count} at {tile.pos}");
                    }
                }
                else if (debugLogging)
                {
                    Debug.Log($"[Spawner] Failed to spawn extra object at {tile.pos} (position occupied)");
                }

                // Wait for the specified interval
                yield return new WaitForSeconds(spawnInterval);
            }

            // Occasionally yield to avoid freezing
            if (attempts % 10 == 0)
            {
                yield return null;
            }
        }

        if (debugLogging)
        {
            Debug.Log($"[Spawner] Extra replenish complete: spawned {spawned}/{count} objects after {attempts} attempts");
        }

        isSpawningExtra = false;
    }

    private bool SpawnPrefab(Vector2 position, bool isExtra)
    {
        // Check if there's already something at this position
        Collider2D existingObject = Physics2D.OverlapCircle(position, 0.5f);
        if (existingObject != null)
        {
            return false; // Skip spawning to avoid overlaps
        }

        // // Instantiate the prefab and set its parent

        if (isExtra)
        {
            // GameObject spawnedPrefab = ObjectPoolManager.SpawnObject(prefab, position, Quaternion.identity); //TODO-OBJECTPOOL: return to this after implementing reset
            GameObject spawnedPrefab = Instantiate(extraPrefab, position, Quaternion.identity);
            ParenthoodManager.AssignParent(spawnedPrefab);
        }
        else
        {
            GameObject spawnedPrefab = Instantiate(regularPrefab, position, Quaternion.identity);
            ParenthoodManager.AssignParent(spawnedPrefab);
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


    public int GetCount(GameObject child)
    {
        Transform parent = ParenthoodManager.GetParent(child);
        if (parent == null)
        {
            return 0;
        }
        return parent.childCount;
    }
}