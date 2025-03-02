using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Collections;

[CreateAssetMenu(fileName = "NewHeatMap", menuName = "HeatMap/Grid HeatMap")]
public class HeatMapData : ScriptableObject
{
    public TilePosData tilePosData;

    
    public Dictionary<Vector2, float> heatmap = new Dictionary<Vector2, float>();
    [Serializable]
    public struct HeatMapEntry
    {
        [ReadOnly]
        public  Vector2 position;  // Key
        [ReadOnly]
        public float value;       // Value
    }
    [SerializeField] private List<HeatMapEntry> heatMapEntries = new List<HeatMapEntry>();
    private void OnEnable()
    {
        if (heatmap == null || heatmap.Count == 0 || tilePosData == null || tilePosData.TilePositions == null)
        {
            Initialize();
        }
    }

    public void Initialize()
    {
        heatmap.Clear();
        heatMapEntries.Clear();
        foreach(var tile in tilePosData.TilePositions)
        {
            heatmap[tile.pos] = 0f; // Default values
            HeatMapEntry heatMapEntry = new HeatMapEntry();
            heatMapEntry.position = tile.pos;
            heatMapEntry.value = 0f;
            heatMapEntries.Add(heatMapEntry);
        }
        Debug.Log("Heatmap Initialized!");
    }

    public void ClearHeatmap()
    {
        Initialize();
    }

    public float GetValue(Vector2 valuePos)
    {
        if (heatmap.ContainsKey(valuePos)){
            return heatmap[valuePos];
        }
        return 0f;
    }

    public void SetValue(Vector2 valuepos, float value)
    {
        heatmap[valuepos] = Mathf.Max(0, Mathf.Clamp(value, 0, 100));
        HeatMapEntry heatMapEntry = new HeatMapEntry();
        heatMapEntry.position = valuepos;
        heatMapEntry.value = value;
        foreach (var mapEntry in heatMapEntries)
        {
            if (mapEntry.position == valuepos)
            {
                heatMapEntries.Remove(mapEntry);
                heatMapEntries.Add(heatMapEntry);
                return;
            }
        }
        heatMapEntries.Add(heatMapEntry);

    }
}
