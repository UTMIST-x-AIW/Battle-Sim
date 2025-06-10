using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SavingTilePos))]
public class SavingTilePositionsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        SavingTilePos script = (SavingTilePos)target;

            int boundSize;
        if (GUILayout.Button("Reinitialize the tilemap positions"))
        {
            script.tiledata.Initialize(script.tilemap, script.boundaryLength);
        }
    }
}
