using System.Collections.Generic;
using UnityEngine;

public static class ParenthoodManager
{
    private static readonly Dictionary<string, Transform> ParentsTransformNameDict = new();

    /// <summary>
    /// Makes a parent of the GameObject go using a stripped-down name that had all (Clone) removed,
    /// and then adds the parent name and transform to ParentsTransformNameDict dictionary;
    /// </summary>
    /// <param name="go"></param>
    /// <returns>GameObject of the parent that was made, if ParentsTransformNameDict
    /// already had a parent with go's name then return the parent</returns>
    static GameObject MakeParent(GameObject go)
    {
        string goName = go.name.Replace("(Clone)", "") + "---Parent";

        if (ParentsTransformNameDict.TryGetValue(goName, out var existingParent) && existingParent != null)
            return existingParent.gameObject;

        // Remove destroyed reference if it exists
        if (existingParent == null && ParentsTransformNameDict.ContainsKey(goName))
        {
            ParentsTransformNameDict.Remove(goName);
        }

        GameObject goParent = new GameObject(goName);


        ParentsTransformNameDict[goParent.name] = goParent.transform;
        return goParent;
    }

    /// <summary>
    /// Assign a parent to the GameObject go;
    /// if parent didn't already exist call MakeParent
    /// </summary>
    /// <param name="go"></param>
    public static void AssignParent(GameObject go)
    {
        string goName = go.name.Replace("(Clone)", "") + "---Parent";
        if (ParentsTransformNameDict.TryGetValue(goName, out var value) && value != null)
        {
            go.transform.SetParent(value, false);
        }
        else
        {
            // Remove destroyed reference if it exists
            if (value == null && ParentsTransformNameDict.ContainsKey(goName))
            {
                ParentsTransformNameDict.Remove(goName);
            }

            GameObject goParent = MakeParent(go);
            go.transform.SetParent(goParent.transform, false);
        }
    }


    public static Transform GetParent(GameObject go)
    {
        string goName = go.name.Replace("(Clone)", "") + "---Parent";
        if (ParentsTransformNameDict.TryGetValue(goName, out var value) && value != null)
        {
            return value;
        }
        else
        {
            // Remove destroyed reference if it exists
            if (value == null && ParentsTransformNameDict.ContainsKey(goName))
            {
                ParentsTransformNameDict.Remove(goName);
            }

            // Don't log warning - parent objects are created on-demand when creatures are spawned
            return null;
        }
    }

    public static int GetTotalChildCount(GameObject child)
    {
        Transform parent = GetParent(child);
        if (parent == null)
        {
            return 0;
        }
        return parent.childCount;
    }


    /// <summary>
    /// Clears ParentsTransformNameDict
    /// </summary>
    public static void ClearParentDict()
    {
        ParentsTransformNameDict.Clear();
    }
}
