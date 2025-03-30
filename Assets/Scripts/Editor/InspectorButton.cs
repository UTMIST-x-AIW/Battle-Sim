using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
[CustomEditor(typeof(UIQuadGrid))]

public class InspectorButton : Editor
{
    #if UNITY_EDITOR
    public override void OnInspectorGUI()
    {
        // Draw the default Inspector
        DrawDefaultInspector();
            
        // Add a button
        UIQuadGrid script = (UIQuadGrid)target;

        if (GUILayout.Button("Make the Albert Spawn points"))
        {
            script.LoadAlbertMap();
        }
        if (GUILayout.Button("Make the Kai Spawn points"))
        {
            script.LoadKaiMap();
        }
        
        if (GUILayout.Button("Destroy the points"))
        {
            script.KillChildren();
        }
       
        if (GUILayout.Button("Hide the points"))
        {
            script.HideChildren();
        }

        
    }
    #endif
}
