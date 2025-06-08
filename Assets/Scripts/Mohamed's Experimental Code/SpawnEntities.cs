using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class SpawnEntities : MonoBehaviour
{
    public Bounds bounds;
    [SerializeField] private GameObject prefab;

    [SerializeField] private int numOfInstances;
        
    [SerializeField] public int rayCount;

    [SerializeField] public float maxDistance = 10f;
        
    [SerializeField] private GameObject[] instances;
        
    void Start() 
    {
        instances = new GameObject[numOfInstances];
        for(int i = 0; i < numOfInstances; i++)
        {
            Vector3 pos  = new Vector3(Random.Range(bounds.min.x, bounds.max.x),Random.Range(bounds.min.y, bounds.max.y), 0f);
            GameObject instance = Instantiate(prefab, pos, Quaternion.identity);
            instances[i]=instance;
        }
    }

    private void LateUpdate()
    {
        foreach (var instance in instances)
        {
            ShootRays(instance);
        }
    }
    private void ShootRays(GameObject instance)
    {
        RaycastHit2D[] rayHits = new RaycastHit2D[2];
        var contactFilter = new ContactFilter2D
        {
            useLayerMask = false,
            useTriggers = false
        };
        for (int i = 0; i < rayCount; i++)
        {
            float t = rayCount > 1 ? i / (float)(rayCount - 1) : 0.5f;
            float rotationAngle = Mathf.Lerp(-180, 180, t);
            Vector2 direction = Quaternion.Euler(0, 0, rotationAngle) * Vector2.right;
           
            int hitCount = Physics2D.Raycast(instance.transform.position, direction, 
                contactFilter, rayHits, maxDistance);
            if (hitCount <= 1 || rayHits[1].collider.gameObject == instance)
            {
                Debug.DrawRay(instance.transform.position, direction * maxDistance, Color.red );
            }
            else
            {
                Debug.DrawLine(instance.transform.position, rayHits[1].transform.position, Color.green ); 
            }

            
            
        }
    }
}
