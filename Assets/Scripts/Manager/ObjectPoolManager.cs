using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[Serializable]
public class ObjectPoolManager : MonoBehaviour
{
        public static List<PooledObjectInfo> ObjectPools { get; private set; } = new List<PooledObjectInfo>();

        private const int MAX_INACTIVE_PER_POOL = 200; // Prevent unbounded growth

	[SerializeField]
	public List<PooledObjectInfo> pooledObjectInfos = new List<PooledObjectInfo>();

	public static event Action<List<GameObject>> OnListChanged;

	private void LateUpdate()
	{
		pooledObjectInfos.Clear();
		pooledObjectInfos.AddRange(ObjectPools);
	}

	public static GameObject SpawnObject(GameObject objectToSpawn, Vector3 spawnPosition, Quaternion spawnRotation)
	{
		PooledObjectInfo pool = ObjectPools.Find(p => p.LookupString == objectToSpawn.name);

		if (pool == null)
		{
			pool = new PooledObjectInfo() { LookupString = objectToSpawn.name };
			ObjectPools.Add(pool);
		}

		GameObject spawnableObj = null;
		foreach (GameObject obj in pool.InactiveObjects)
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
			pool.ActiveObjects.Add(spawnableObj);
			if (pool.LookupString == "Albert" || pool.LookupString == "Kai")
			{
				OnListChanged?.Invoke(pool.ActiveObjects);
			}

		}

		else

		{
			spawnableObj.transform.position = spawnPosition;
			spawnableObj.transform.rotation = spawnRotation;
			pool.InactiveObjects.Remove(spawnableObj);
			pool.ActiveObjects.Add(spawnableObj);
			if (pool.LookupString == "Albert" || pool.LookupString == "Kai")
			{
				OnListChanged?.Invoke(pool.ActiveObjects);
			}
			spawnableObj.SetActive(true);
		}

		return spawnableObj;
	}

        public static void ReturnObjectToPool(GameObject obj)
        {
                string goName = obj.name.Replace("(Clone)", "");

                PooledObjectInfo pool = ObjectPools.Find(p => p.LookupString == goName);

                if (pool == null)
                {
                        Debug.LogWarning($"Trying to release an object that is not pooled: {goName}. Destroying.");
                        Destroy(obj);
                        return;
                }
                else
                {
                        obj.SetActive(false);
                        pool.InactiveObjects.Add(obj);
                        pool.ActiveObjects.Remove(obj);
                        if (pool.InactiveObjects.Count > MAX_INACTIVE_PER_POOL)
                        {
                                var excess = pool.InactiveObjects[0];
                                pool.InactiveObjects.RemoveAt(0);
                                Destroy(excess);
                        }
                        if (pool.LookupString == "Albert" || pool.LookupString == "Kai")
                        {
                                OnListChanged?.Invoke(pool.ActiveObjects);
                        }
                }
        }

        public static void ClearPools()
        {
                foreach (PooledObjectInfo pool in ObjectPools)
                {
                        foreach (var obj in pool.InactiveObjects)
                        {
                                if (obj != null)
                                {
                                        Destroy(obj);
                                }
                        }
                        pool.ActiveObjects.Clear();
                        pool.InactiveObjects.Clear();
                }
        }


        private void OnDisable()
        {
                ClearPools();
        }
}

[Serializable]
public class PooledObjectInfo
{
	public string LookupString;
	public List<GameObject> InactiveObjects = new List<GameObject>();
	public List<GameObject> ActiveObjects = new List<GameObject>();
}