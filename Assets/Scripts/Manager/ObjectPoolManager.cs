using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[Serializable]
public class ObjectPoolManager : MonoBehaviour
{
        public static List<PooledObjectInfo> ObjectPools { get; private set; } = new List<PooledObjectInfo>();

	[SerializeField]
	public List<PooledObjectInfo> pooledObjectInfos = new List<PooledObjectInfo>();

        public static event Action<List<GameObject>> OnListChanged;

        /// <summary>
        /// Optionally pre-populate a pool with inactive objects so spawns do not
        /// incur the cost of instantiation during gameplay.
        /// </summary>
        /// <param name="prefab">Prefab to prewarm the pool with</param>
        /// <param name="count">How many instances to create</param>
        public static void PrewarmPool(GameObject prefab, int count)
        {
                if (count <= 0 || prefab == null)
                {
                        return;
                }

                PooledObjectInfo pool = ObjectPools.Find(p => p.LookupString == prefab.name);
                if (pool == null)
                {
                        pool = new PooledObjectInfo() { LookupString = prefab.name };
                        ObjectPools.Add(pool);
                }

                int toCreate = Mathf.Max(0, count - (pool.InactiveObjects.Count + pool.ActiveObjects.Count));
                for (int i = 0; i < toCreate; i++)
                {
                        GameObject obj = Instantiate(prefab);
                        obj.SetActive(false);
                        pool.InactiveObjects.Add(obj);
                }
        }

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
			Debug.LogWarning("Trying to release an object that is not pooled: " + goName);
		}
		else
		{
			obj.SetActive(false);
			pool.InactiveObjects.Add(obj);
			pool.ActiveObjects.Remove(obj);
			if (pool.LookupString == "Albert" || pool.LookupString == "Kai")
			{
				OnListChanged?.Invoke(pool.ActiveObjects);
			}
		}
	}


	void ClearingPools()
	{
		foreach (PooledObjectInfo pool in ObjectPools)
		{
			pool.ActiveObjects.Clear();
			pool.InactiveObjects.Clear();
		}
	}

	private void OnDisable()
	{
		ClearingPools();
	}
}

[Serializable]
public class PooledObjectInfo
{
	public string LookupString;
	public List<GameObject> InactiveObjects = new List<GameObject>();
	public List<GameObject> ActiveObjects = new List<GameObject>();
}