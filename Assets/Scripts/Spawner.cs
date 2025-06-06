using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private HeatMapData heatmapData;
    [SerializeField] private HeatMapData extraHeatmapData;
    [SerializeField, Range(0.01f, 5f)] private float spawnInterval = 0.25f;
    [SerializeField] private int maxNumOfSpawns = 10;
    [SerializeField] private int numExtraObjects = 5;
    [SerializeField, Range(0f, 1f)] private float extraObjectRespawnRate = 0f;
    [SerializeField] private bool useDistinctHeatmaps = true;
    [SerializeField] private float extraObjectMinutesToExtinction = 0f;
    [SerializeField] private bool debugLogging = true;

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

    private GameObject prefabParent;
    private GameObject extraPrefabParent;
    private int regularIndex = 0;
    private int extraIndex = 0;
    private List<TilePosData.TilePos> highProbabilityPositions;
    private List<TilePosData.TilePos> extraHighProbabilityPositions;
    private float simulationStartTime;
    private float initialRespawnRate;

    private void OnEnable()
    {
        if (prefab == null || heatmapData == null)
        {
            Debug.LogError("Error: Prefab or HeatMapData are uninitialized", this);
            enabled = false;
        }

        if (useDistinctHeatmaps && extraHeatmapData == null)
        {
            Debug.LogWarning("Extra heatmap data is null but useDistinctHeatmaps is true. Will use regular heatmap for both.", this);
        }

        initialRespawnRate = extraObjectRespawnRate;
    }

    private void Start()
    {
        simulationStartTime = Time.time;

        if (extraObjectMinutesToExtinction > 0)
        {
            extraObjectRespawnRate = 1.0f;
            if (debugLogging) Debug.Log($"[Spawner] Starting with extraObjectRespawnRate=1.0 (extinction time={extraObjectMinutesToExtinction}m)");
        }

        prefabParent = GameObject.Find($"{prefab.name} Parent") ?? new GameObject($"{prefab.name} Parent");
        extraPrefabParent = GameObject.Find($"{prefab.name} Extra Parent") ?? new GameObject($"{prefab.name} Extra Parent");

        CacheHighProbabilityPositions();
        StartCoroutine(SpawnLoop());
    }

    private void Update()
    {
        if (extraObjectMinutesToExtinction > 0)
        {
            float elapsedTime = Time.time - simulationStartTime;
            float extinctionTimeInSeconds = extraObjectMinutesToExtinction * 60f;

            if (elapsedTime < extinctionTimeInSeconds)
            {
                extraObjectRespawnRate = 1.0f - (elapsedTime / extinctionTimeInSeconds);
                if (debugLogging && Time.frameCount % 300 == 0)
                    Debug.Log($"[Spawner] Current extraObjectRespawnRate={extraObjectRespawnRate:F3}, elapsed={elapsedTime:F1}s/{extinctionTimeInSeconds:F1}s");
            }
            else if (extraObjectRespawnRate > 0f)
            {
                extraObjectRespawnRate = 0f;
                Debug.Log("[Spawner] Extinction period complete, respawn rate set to 0");
            }
        }
    }

    private void CacheHighProbabilityPositions()
    {
        highProbabilityPositions = CacheProbabilityPositionsForHeatmap(heatmapData, maxNumOfSpawns * 2);

        HeatMapData extraMap = useDistinctHeatmaps && extraHeatmapData != null ? extraHeatmapData : heatmapData;
        extraHighProbabilityPositions = CacheProbabilityPositionsForHeatmap(extraMap, numExtraObjects * 2);
    }

    private List<TilePosData.TilePos> CacheProbabilityPositionsForHeatmap(HeatMapData map, int neededPositions)
    {
        var allPositions = map.tilePosData.TilePositions.ToList();
        var highProbPositions = new List<TilePosData.TilePos>();

        foreach (var tile in allPositions)
        {
            float probability = map.GetValue(tile.pos);
            if (probability > 50f)
            {
                highProbPositions.Add(tile);
            }
        }

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
        Collider2D existingObject = Physics2D.OverlapCircle(position, 0.5f);
        if (existingObject != null) return false;

        GameObject spawnedPrefab = Instantiate(prefab, position, Quaternion.identity);
        Transform parent = isExtra ? extraPrefabParent.transform : prefabParent.transform;
        spawnedPrefab.transform.SetParent(parent, false);
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
