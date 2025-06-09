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

    // Keys to use for painting and erasing
    private KeyCode paintKey = KeyCode.LeftShift;
    private KeyCode eraseKey = KeyCode.LeftControl;

    // Keys for adjusting brush
    private KeyCode increaseSizeKey = KeyCode.RightBracket;   // ]
    private KeyCode decreaseSizeKey = KeyCode.LeftBracket;    // [
    private KeyCode increaseStrengthKey = KeyCode.Period;     // .
    private KeyCode decreaseStrengthKey = KeyCode.Comma;      // ,

    // Amount to change brush parameters per key press
    private float brushSizeStep = 0.1f;
    private float brushStrengthStep = 0.1f;

    private void OnEnable()
    {
        visualizer = (HeatMapVisualizer)target;
        // Make sure the Scene view gets keyboard events
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Heatmap Editing Controls", EditorStyles.boldLabel);

        if (GUILayout.Button(_enabledediting ? "Disable Editing" : "Enable Editing"))
        {
            _enabledediting = !_enabledediting;
            SceneView.RepaintAll();
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Keyboard Shortcuts:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Hold {paintKey} to paint", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"Hold {eraseKey} to erase", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"{increaseSizeKey}/{decreaseSizeKey} to adjust brush size", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"{increaseStrengthKey}/{decreaseStrengthKey} to adjust strength", EditorStyles.miniLabel);

        EditorGUILayout.Space();

        // Manual mode toggle
        if (GUILayout.Button(EraserOn ? "Switch to Paint Mode" : "Switch to Erase Mode"))
        {
            EraserOn = !EraserOn;
        }

        if (GUILayout.Button("Reset Heatmap"))
        {
            visualizer.heatMapData.ClearHeatmap();
        }

        EditorGUILayout.Space();

        // Brush parameters with labels showing values
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel($"Brush Strength ({brushstrength:F1})");
        brushstrength = EditorGUILayout.Slider(brushstrength, 0.1f, 500.0f);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel($"Brush Radius ({brushRadius:F1})");
        brushRadius = EditorGUILayout.Slider(brushRadius, 0.1f, 100.0f);
        EditorGUILayout.EndHorizontal();
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!_enabledediting || visualizer.heatMapData == null) return;

        Event e = Event.current;

        // Handle keyboard shortcuts for brush adjustments
        if (e.type == EventType.KeyDown && !e.alt && !e.shift && !e.control)
        {
            bool handled = false;

            if (e.keyCode == increaseSizeKey)
            {
                brushRadius = Mathf.Clamp(brushRadius + brushSizeStep, 0.1f, 5.0f);
                handled = true;
            }
            else if (e.keyCode == decreaseSizeKey)
            {
                brushRadius = Mathf.Clamp(brushRadius - brushSizeStep, 0.1f, 5.0f);
                handled = true;
            }
            else if (e.keyCode == increaseStrengthKey)
            {
                brushstrength = Mathf.Clamp(brushstrength + brushStrengthStep, 0.1f, 5.0f);
                handled = true;
            }
            else if (e.keyCode == decreaseStrengthKey)
            {
                brushstrength = Mathf.Clamp(brushstrength - brushStrengthStep, 0.1f, 5.0f);
                handled = true;
            }

            if (handled)
            {
                e.Use();
                Repaint(); // Update the Inspector to show the new values
                return;
            }
        }

        // Get current mouse position in world space
        Vector2 mousePos = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;
        Vector2 worldPos = mousePos + (Vector2)visualizer.transform.position;

        // Draw a more visible brush preview with thicker lines
        Handles.color = new Color(1f, 1f, 1f, 0.5f); // Higher alpha for more visibility
        Handles.DrawWireDisc(worldPos, Vector3.forward, brushRadius);

        // Draw a second, slightly smaller disc for better visibility
        Handles.color = new Color(0.8f, 0.8f, 0.8f, 0.7f);
        Handles.DrawWireDisc(worldPos, Vector3.forward, brushRadius * 0.95f);

        // Check if appropriate modifier keys are pressed
        bool shiftPressed = e.shift;
        bool controlPressed = e.control;

        // Determine if we should paint or erase based on key press and mode
        bool shouldPaint = !EraserOn && shiftPressed;
        bool shouldErase = EraserOn || controlPressed;

        // Apply changes on mousemove, layout, or repaint events when appropriate keys are held
        if ((e.type == EventType.MouseMove || e.type == EventType.Layout || e.type == EventType.Repaint))
        {
            if (shouldPaint)
            {
                ModifyHeatmap(worldPos, brushstrength, brushRadius);
                SceneView.RepaintAll();
                Repaint();
            }
            else if (shouldErase)
            {
                ModifyHeatmap(worldPos, -brushstrength, brushRadius);
                SceneView.RepaintAll();
                Repaint();
            }
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
