using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ObjectPoolManager : MonoBehaviour
{
    public static Dictionary<string, ObjectPool<GameObject>> Pools { get; } = new();

    public static void PrewarmPool(GameObject prefab, int count)
    {
        if (prefab == null || count <= 0) return;
        var pool = GetOrCreatePool(prefab, count);
        for (int i = 0; i < count; i++)
        {
            var obj = pool.Get();
            pool.Release(obj);
        }
    }

    public static GameObject SpawnObject(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        var pool = GetOrCreatePool(prefab);
        GameObject obj = pool.Get();
        obj.transform.SetPositionAndRotation(position, rotation);
        return obj;
    }

    public static void ReturnObjectToPool(GameObject obj)
    {
        if (obj == null) return;
        string key = obj.name.Replace("(Clone)", string.Empty);
        if (Pools.TryGetValue(key, out var pool))
        {
            pool.Release(obj);
        }
        else
        {
            Object.Destroy(obj);
        }
    }

    static ObjectPool<GameObject> GetOrCreatePool(GameObject prefab, int defaultCapacity = 10)
    {
        if (!Pools.TryGetValue(prefab.name, out var pool))
        {
            pool = new ObjectPool<GameObject>(
                () => Instantiate(prefab),
                obj => obj.SetActive(true),
                obj => obj.SetActive(false),
                obj => Destroy(obj),
                true,
                defaultCapacity,
                1000);
            Pools[prefab.name] = pool;
        }
        return pool;
    }

    void OnDisable()
    {
        foreach (var pool in Pools.Values)
        {
            pool.Clear();
        }
        Pools.Clear();
    }
}
