using System.Collections.Generic;
using UnityEngine;

namespace ExperimentalCode
{
    public static class ParenthoodManager
    {
        private static readonly Dictionary<string,Transform> ParentsTransformNameDict = new();

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
            
            if (ParentsTransformNameDict.TryGetValue(goName, out var existingParent))
                return existingParent.gameObject;
                        
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
            if (ParentsTransformNameDict.TryGetValue(goName, out var value))
            {
                go.transform.SetParent(value, false);
            }
            else
            {
                GameObject goParent = MakeParent(go);
                go.transform.SetParent(goParent.transform, false);
            }
        }

        
        /// <summary>
        /// Clears ParentsTransformNameDict
        /// </summary>
        public static void ClearParentDict()
        {
            ParentsTransformNameDict.Clear();
        }
    }
}