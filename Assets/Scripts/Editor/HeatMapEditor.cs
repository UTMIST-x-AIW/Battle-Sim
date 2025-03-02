using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
[CustomEditor(typeof(HeatMapVisualizer))]
public class HeatMapEditor : Editor
{
    private HeatMapVisualizer visualizer;
    private bool _enabledediting = false;
    private bool EraserOn = false;
    private float brushstrength = 0.6f;
    private float brushRadius = 0.6f;


    private void OnEnable()
    {
        visualizer = (HeatMapVisualizer)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button(_enabledediting ? "Disable Editing": "Enable Editing"))
        {
            _enabledediting = !_enabledediting;
            SceneView.RepaintAll();
        }
        if (GUILayout.Button(EraserOn ? "Eraser Mode" : "Paint Mode"))
        {
            EraserOn = !EraserOn;
        }
        if (GUILayout.Button("Reset Heatmap"))
        {
            visualizer.heatMapData.ClearHeatmap();
        }
        brushstrength = EditorGUILayout.Slider("Brush Strength", brushstrength, 0.6f, 0.9f);
        brushRadius = EditorGUILayout.Slider("Brush Radius", brushRadius, 0.0f, 1.5f);

    }

    private void OnSceneGUI()
    {
        if (!_enabledediting || visualizer.heatMapData == null) return;

        Event e = Event.current;
        Vector2 mousePos = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;
        Vector2 mousePos_transformed = mousePos + (Vector2)visualizer.transform.position;


        if (e.type == EventType.MouseDrag || !EraserOn)
        {
            ModifyHeatmap(mousePos_transformed, brushstrength, brushRadius);
           // e.Use();
        }
        else if (e.type == EventType.MouseDown || EraserOn)
        {
            ModifyHeatmap(mousePos_transformed, -brushstrength, brushRadius);
            //e.Use();
        }
    }

    /// <summary>
    /// Modifies heatmap values in a small radius around the given position.
    /// </summary>
    private void ModifyHeatmap(Vector2 center, float strength, float radius)
    {
        foreach (var tile in visualizer.heatMapData.tilePosData.TilePositions)
        {
            float distance = Vector2.Distance(tile.pos, center);
            if (distance <= radius)
            {
                // Apply a smooth falloff based on distance (closer = stronger)
                float falloff = Mathf.InverseLerp(radius, 0, distance); // Value between 0 and 1
                float adjustedStrength = strength * falloff; // Scale strength based on falloff

                visualizer.heatMapData.SetValue(tile.pos, visualizer.heatMapData.GetValue(tile.pos) + adjustedStrength);
            }
        }

        EditorUtility.SetDirty(visualizer.heatMapData);
    }

}
