using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using System.Linq;

[Serializable]
public class ObjectPoolManager : MonoBehaviour
{
        public static List<PooledObjectInfo> ObjectPools { get; private set; } = new List<PooledObjectInfo>();

        // Registry of active creature components for quick lookup
        public static List<Creature> ActiveCreatures { get; private set; } = new List<Creature>();

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

                // Track active creatures
                var creatureComp = spawnableObj.GetComponent<Creature>();
                if (creatureComp != null && !ActiveCreatures.Contains(creatureComp))
                {
                        ActiveCreatures.Add(creatureComp);
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

                // Remove from active creature registry if applicable
                var creatureComp = obj.GetComponent<Creature>();
                if (creatureComp != null)
                {
                        ActiveCreatures.Remove(creatureComp);
                }
        }


        void ClearingPools()
        {
                foreach (PooledObjectInfo pool in ObjectPools)
                {
                        pool.ActiveObjects.Clear();
                        pool.InactiveObjects.Clear();
                }
                ActiveCreatures.Clear();
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