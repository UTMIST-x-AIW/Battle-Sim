using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using System;


public class ObjectPoolManager : MonoBehaviour
{
        public static List<PooledObjectInfo> ObjectPools { get; private set; } = new List<PooledObjectInfo>();
        public static List<Creature> ActiveCreatures { get; private set; } = new List<Creature>();

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

                }
                else
                {
                        spawnableObj.transform.position = spawnPosition;
                        spawnableObj.transform.rotation = spawnRotation;
                        pool.InactiveObjects.Remove(spawnableObj);
                        pool.ActiveObjects.Add(spawnableObj);
                        spawnableObj.SetActive(true);
                }

                if (pool.LookupString == "Albert" || pool.LookupString == "Kai")
                {
                        OnListChanged?.Invoke(pool.ActiveObjects);
                }

                var creatureComponent = spawnableObj.GetComponent<Creature>();
                if (creatureComponent != null && !ActiveCreatures.Contains(creatureComponent))
                {
                        ActiveCreatures.Add(creatureComponent);
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

                        var creatureComponent = obj.GetComponent<Creature>();
                        if (creatureComponent != null)
                        {
                                ActiveCreatures.Remove(creatureComponent);
                        }
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
