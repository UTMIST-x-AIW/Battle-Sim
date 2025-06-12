using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewHeatMap", menuName = "HeatMap/Grid HeatMap")]
public class HeatMapData : ScriptableObject
{
    public TilePosData tilePosData;

    public Dictionary<Vector2, float> heatmap = new Dictionary<Vector2, float>();

    [Serializable]
    public class HeatMapEntry
    {
        public Vector2 position;
        public float value;
    }

    [SerializeField] private List<HeatMapEntry> heatMapEntries = new List<HeatMapEntry>();

    private void OnEnable()
    {
        LoadHeatmap();
        if (heatMapEntries == null)
        {
            Initialize();
        }
    }

    public void Initialize()
    {
        heatmap.Clear();
        heatMapEntries.Clear();

        if (tilePosData == null || tilePosData.TilePositions == null)
        {
            Debug.LogError("TilePosData is not initialized!");
            return;
        }

        foreach (var tile in tilePosData.TilePositions)
        {
            heatmap[tile.pos] = 0f; // Default values
            heatMapEntries.Add(new HeatMapEntry { position = tile.pos, value = 0f });
        }
        Debug.Log("Heatmap Initialized!");
    }

    public void ClearHeatmap()
    {
        Initialize();
    }

    public float GetValue(Vector2 valuePos)
    {
        return heatmap.TryGetValue(valuePos, out float value) ? value : 0f;
    }

    public void SetValue(Vector2 valuePos, float value)
    {
        value = Mathf.Clamp(value, 0, 100);
        if (heatmap.ContainsKey(valuePos))
        {
            heatmap[valuePos] = value;
        }
        else
        {
            heatmap.Add(valuePos, value);
        }

        bool found = false;
        for (int i = 0; i < heatMapEntries.Count; i++)
        {
            if (heatMapEntries[i].position == valuePos)
            {
                heatMapEntries[i].value = value;
                found = true;
                break;
            }
        }

        if (!found)
        {
            heatMapEntries.Add(new HeatMapEntry { position = valuePos, value = value });
        }
    }

    private void LoadHeatmap()
    {
        heatmap.Clear();
        foreach (var entry in heatMapEntries)
        {
            heatmap[entry.position] = entry.value;
        }
    }
}
