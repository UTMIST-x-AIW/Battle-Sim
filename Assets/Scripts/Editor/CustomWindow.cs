using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class HeatmapVisibilityWindow : EditorWindow
{
    // You can manually assign these, or populate them dynamically
    private List<GameObject> heatmapObjects = new List<GameObject>();
    private Vector2 scrollPos;

    [MenuItem("Tools/Heatmap Visibility")]
    public static void ShowWindow()
    {
        GetWindow<HeatmapVisibilityWindow>("Heatmap Visibility");
    }

    private void OnGUI()
    {
        GUILayout.Label("Heatmap Object Visibility", EditorStyles.boldLabel);

        // Button to auto-find heatmap objects by tag or name
        if (GUILayout.Button("Find Heatmap Objects"))
        {
            heatmapObjects = new List<GameObject>(GameObject.FindGameObjectsWithTag("Heatmap"));
        }

        if (heatmapObjects.Count == 0)
        {
            EditorGUILayout.HelpBox("No heatmap objects found. Try assigning tag 'Heatmap'.", MessageType.Info);
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        for (int i = 0; i < heatmapObjects.Count; i++)
        {
            GameObject go = heatmapObjects[i];
            if (go == null) continue;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(go, typeof(GameObject), true);

            string buttonLabel = go.activeSelf ? "Hide" : "Show";
            if (GUILayout.Button(buttonLabel, GUILayout.Width(60)))
            {
                go.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }
}
