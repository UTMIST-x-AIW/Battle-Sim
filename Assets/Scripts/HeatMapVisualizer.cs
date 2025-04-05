using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NEAT.Genes;

public class HeatMapVisualizer : MonoBehaviour
{
    public HeatMapData heatMapData;
    public float size = 0.6f;
    private float offset = 0.5f;

    private void OnDrawGizmos()
    {
        if (heatMapData == null || heatMapData.heatmap == null || heatMapData.tilePosData == null 
            || heatMapData.tilePosData.TilePositions == null) return;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, Quaternion.Euler(0, 0, 0), Vector3.one);
        foreach (var tile in heatMapData.tilePosData.TilePositions)
        {
            float value = heatMapData.GetValue(tile.pos);
            float new_value = MathEq.Remap(value,0,100,0,5);
            Gizmos.color = new Color(5 - new_value, new_value, 0f, 0.8f);
            Vector3 pos = new Vector3(tile.pos.x , tile.pos.y + 0.5f, 0);
            Gizmos.DrawCube(pos, Vector3.one * (size * 0.5f));
        }
    }
    
}
public static class MathEq
{
    public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
    {
        return toMin + (value - fromMin) * (toMax - toMin) / (fromMax - fromMin);
    }
}

