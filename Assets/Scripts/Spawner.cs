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

    private GameObject prefabParent; // Parent object for organizing spawned prefabs

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
        StartCoroutine(Spawning());
    }

    private IEnumerator Spawning()
    {
        // Ensure tile positions are available
        if (heatmapData.tilePosData == null || heatmapData.tilePosData.TilePositions == null)
        {
            Debug.LogError("Error: TilePosData or TilePositions are uninitialized", this);
            yield break;
        }
        List<TilePosData.TilePos> tilePositions = heatmapData.tilePosData.TilePositions.ToList();
        Shuffle(tilePositions);

        // Continue spawning until the maximum number of spawns is reached
        while (prefabParent.transform.childCount < maxNumOfSpawns)
        {
            // Iterate through all tile positions
            
            foreach (var tile in tilePositions)
            {
                // Exit if the maximum number of spawns is reached
                if (prefabParent.transform.childCount >= maxNumOfSpawns)
                {
                    yield break;
                }

                // Calculate spawn probability and compare with a random value
                float spawnProbability = heatmapData.GetValue(tile.pos);
                float randomVal = Random.Range(0f, 100f);

                if (spawnProbability > randomVal)
                {
                    // Spawn the prefab at the tile's position
                    SpawnPrefab(tile.pos);

                    // Wait for the specified interval before the next spawn attempt
                    yield return new WaitForSeconds(spawnInterval);
                }
            }

            // Yield control to the engine after checking all tiles
            yield return null;
        }
    }

    private void SpawnPrefab(Vector2 position)
    {
        // Instantiate the prefab and set its parent
        GameObject spawnedPrefab = Instantiate(prefab, position, Quaternion.identity);
        spawnedPrefab.transform.SetParent(prefabParent.transform, false);

        // Optional: Log the spawn for debugging
        Debug.Log($"Spawned {prefab.name} at {position}");
    }
    void Shuffle<T>(List<T> list)
    {
        int n = list.Count;
        for (int i = n - 1; i >0; i--)
        {
            int j = Random.Range(0, i - 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}