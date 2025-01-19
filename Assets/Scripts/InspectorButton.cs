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

        if (GUILayout.Button("Make the Bastards"))
        {
            script.LoadMapper();
        }
        
        if (GUILayout.Button("Kill the Bastards"))
        {
            script.KillChildren();
        }
       
        
    }
}
