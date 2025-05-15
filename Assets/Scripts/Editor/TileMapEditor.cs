using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SavingTilePos))]
public class TileMapEditor : Editor
{
    
    
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SavingTilePos instance = (SavingTilePos)target;
        

        EditorGUILayout.Space();

        if (GUILayout.Button("Recalculate the Tile Positions"))
        {
            instance.RecalculatePos();
            
        
        }
    }
}




