using UnityEditor;
using UnityEngine;

public class MyCustomWindow : EditorWindow
{
    GameObject[] heatmaps;
    bool toggleValue = true;
    float floatValue = 1.0f;

    [MenuItem("Window/HeatMap Window")]
    public static void ShowWindow()
    {
        // Show existing window instance. If one doesn't exist, make one.
        GetWindow<MyCustomWindow>("HeatMap Editor");
    }
    private void OnEnable()
    {
        heatmaps = GameObject.FindGameObjectsWithTag("heatmap");
    }

    private void OnGUI()
    {
        GUILayout.Label("This is a custom editor window", EditorStyles.boldLabel);

      // GUILayout.
         toggleValue = EditorGUILayout.Toggle("Toggle", toggleValue);
        floatValue = EditorGUILayout.Slider("Float Slider", floatValue, 0f, 10f);

        if (GUILayout.Button("Click Me"))
        {
           // Debug.Log("Button clicked! Current Text: " + myText);
        }
    }
}
