/*using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(GraphInfo))]
public class GraphInfoDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var root = new VisualElement();

        var usingExtraPrefabsProp = property.FindPropertyRelative("UsingExtraPrefabs");
        var prefabListProp = property.FindPropertyRelative("prefabs");
        var singlePrefabProp = property.FindPropertyRelative("_prefab");

        // Toggle field
        var toggle = new PropertyField(usingExtraPrefabsProp, "Using Extra Prefabs");
        root.Add(toggle);

        // List field
        var prefabListField = new PropertyField(prefabListProp, "Multiple Objects");

        // Single prefab field
        var singleField = new ObjectField("Single Object")
        {
            objectType = typeof(GameObject),
            allowSceneObjects = true
        };
        singleField.BindProperty(singlePrefabProp);

        // Initial display
        void RefreshFields()
        {
            if (usingExtraPrefabsProp.boolValue)
            {
                if (root.Contains(singleField)) root.Remove(singleField);
                if (!root.Contains(prefabListField)) root.Add(prefabListField);
            }
            else
            {
                if (root.Contains(prefabListField)) root.Remove(prefabListField);
                if (!root.Contains(singleField)) root.Add(singleField);
            }
        }

        RefreshFields();

        toggle.RegisterCallback<ChangeEvent<bool>>(evt =>
        {
            usingExtraPrefabsProp.boolValue = evt.newValue;
            property.serializedObject.ApplyModifiedProperties();
            RefreshFields();
        });

        return root;
    }
}*/