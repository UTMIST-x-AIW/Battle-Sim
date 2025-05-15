using UnityEngine;
using System.Collections.Generic;

//[CreateAssetMenu(fileName = "PooledObjectInfo", menuName = "Pools/PooledObjectInfo")]
//public class PooledObjectInfo : ScriptableObject
//{
//    public string LookupString;
//    public List<GameObject> InactiveObject = new List<GameObject>();

//    // This method is now handled by the ObjectPoolManager
//    // Keeping it for backward compatibility
//    public void PreloadPool(GameObject prefab, int count)
//    {
//        Debug.Log($"PreloadPool is now handled by ObjectPoolManager. " +
//                  $"Please add this PooledObjectInfo ({name}) to the ObjectPoolManager.");
//    }

//    // Clear the pool (useful for editor reset)
//    public void ClearPool()
//    {
//        InactiveObject.Clear();
//    }
//}
