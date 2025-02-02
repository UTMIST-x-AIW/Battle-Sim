using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(UISphereGrid))]

public class InspectorButton : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default Inspector
        DrawDefaultInspector();
            
        // Add a button
        UISphereGrid script = (UISphereGrid)target;

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
}
