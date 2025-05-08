using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[Serializable]
public class ObjectPoolManager : MonoBehaviour
{
    public static List<PooledObjectInfo> ObjectPools = new List<PooledObjectInfo>();

    public static GameObject SpawnObject(GameObject objectToSpawn, Vector3 spawnPosition, Quaternion spawnRotation)
    {
        PooledObjectInfo pool = ObjectPools.Find(p => p.LookupString == objectToSpawn.name);

        if (pool == null)
        {
            Debug.Log("The pool " + objectToSpawn.name + " was null");
            pool = new PooledObjectInfo() { LookupString = objectToSpawn.name };
            ObjectPools.Add(pool);
        }

        GameObject spawnableObj = null;
        foreach (GameObject obj in pool.InactiveObject)
        {
            if (obj != null)
            {
                spawnableObj = obj;
                break; 
            }
        }

        if (spawnableObj == null)
        {
            spawnableObj = Instantiate(objectToSpawn, spawnPosition, spawnRotation);
            Debug.Log(spawnableObj.name + " was instantiated and added to the pool");
        }
        
        else
        
        {
            spawnableObj.transform.position = spawnPosition;
            spawnableObj.transform.rotation = spawnRotation;
            pool.InactiveObject.Remove(spawnableObj);
            spawnableObj.SetActive(true);
        }

        return spawnableObj;
    }

    public static void ReturnObjectToPool(GameObject obj)
    {
        string goName = obj.name.Substring(0, obj.name.Length - 7);

        PooledObjectInfo pool = ObjectPools.Find(p => p.LookupString == goName);

        if (pool == null)
        {
            Debug.LogWarning("Trying to release an object that is not pooled: " + goName);
        }
        else
        {
            obj.SetActive(false);
            pool.InactiveObject.Add(obj);
            Debug.Log(obj.name + " was killed and was added to the pool inactive list");
        }


    }
}


public class PooledObjectInfo
{
    public string LookupString;
    public List<GameObject> InactiveObject = new List<GameObject>();
}
