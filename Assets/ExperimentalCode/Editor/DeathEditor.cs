using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ExperimentalCode.Editor
{
    [CustomEditor(typeof(InstantiatingCreatures))]
    public class DeathEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            InstantiatingCreatures instance = (InstantiatingCreatures)target;

            if (GUILayout.Button("Instantiate Creature Normally"))
            {
                instance.MakeNormalCreatures();
            }
            
            if (GUILayout.Button("Instantiate Creature with Animation"))
            { 
                instance.GrowCreatures();
            }
            
            if (GUILayout.Button("Kill Creature"))
            { 
                instance.DieCreatures();
            }

        }
    }
}