using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SavingParentTransforms
{
    public static Dictionary<string, Transform> parentCache = new Dictionary<string, Transform>();

    public static Transform GetParentForPrefab(GameObject prefab)
    {
        string parentName = prefab.name + " Parent";

        if (!parentCache.TryGetValue(parentName, out Transform parent))
        {
            GameObject parentObj = GameObject.Find(parentName);
            if (parentObj == null)
            {
                parentObj = new GameObject(parentName);
            }
            parent = parentObj.transform;
            parentCache[parentName] = parent;
        }

        return parent;
    }

    public static Transform GetParentForPrefabByName(string prefabName)
    {
        string parentName = prefabName + " Parent";

        if (!parentCache.TryGetValue(parentName, out Transform parent))
        {
            GameObject parentObj = GameObject.Find(parentName);
            if (parentObj == null)
            {
                parentObj = new GameObject(parentName);
            }
            parent = parentObj.transform;
            parentCache[parentName] = parent;
        }

        return parent;
    }
}
