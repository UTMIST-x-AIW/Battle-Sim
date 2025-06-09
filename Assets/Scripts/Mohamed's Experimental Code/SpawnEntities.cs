using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
using Utils;

public class SpawnEntities : MonoBehaviour
{
    public Bounds bounds;
    [SerializeField] private GameObject prefab;

    [SerializeField] private int numOfInstances;
        
    [SerializeField] public int rayCount;

    [SerializeField] public float maxDistance = 10f;
        
    [SerializeField] private List<MinimalCreature> instances;
    
    private RaycastsWithJobs _raycastsWithJobs;
        
    [SerializeField] int minCommandsPerJob = 21;
        
    void Start() 
    {
        instances = new List<MinimalCreature>();
        _raycastsWithJobs = new RaycastsWithJobs();
        _raycastsWithJobs.Initialize(rayCount);
        for(int i = 0; i < numOfInstances; i++)
        {
            Vector3 pos  = new Vector3(Random.Range(bounds.min.x, bounds.max.x),Random.Range(bounds.min.y, bounds.max.y), 0f);
            Instantiate(prefab, pos, Quaternion.identity);
        }
        instances = FindObjectsOfType<MinimalCreature>().ToList(); ;
    }

    private void LateUpdate()
    {
        // foreach (var instance in instances)
        // {
        //     ShootRays(instance);
        // }
        int instancesLength = instances.Count;
        var positions = new NativeArray<float3>(instancesLength, Allocator.TempJob);
        //var rayInInstances = new NativeArray<RaycastHit>(instancesLength * rayCount, Allocator.TempJob);

        for (int i = 0; i < instancesLength; i++)
        {
            var obj = instances[i];
            positions[i] = obj.transform.position;
        }
        var hits = _raycastsWithJobs.RayCastCommand(positions, rayCount, maxDistance, minCommandsPerJob);
        for (var index = 0; index < instances.Count; index++)
        {
            var creature = instances[index];
            NativeArray<RaycastHit> creatureHits = hits.GetSubArray(index * rayCount, rayCount);
            creature.Hits = creatureHits;
            ProcessHitsForCreature(creature, creatureHits);

           // creatureHits.Dispose();
        }

        positions.Dispose();
    }
    
    private void ProcessHitsForCreature(MinimalCreature creature, NativeArray<RaycastHit> hits)
    {
        for (int i = 0; i < hits.Length; i++)
        {
            var hit = hits[i];
            if (hit.collider != null)
            {
                Debug.DrawRay(creature.transform.position, hit.point, Color.cyan);
            }
            else
            {
                Debug.DrawRay(creature.transform.position, Vector3.up, Color.red);
            }
        }
    }
    private void ShootRays(MinimalCreature instance)
    {

        for (int i = 0; i < rayCount; i++)
        {
            float t = rayCount > 1 ? i / (float)(rayCount - 1) : 0.5f;
            float rotationAngle = Mathf.Lerp(-180, 180, t);
            Vector2 direction = Quaternion.Euler(0, 0, rotationAngle) * Vector2.right;
           
            RaycastHit2D hit = Physics2DExtensions.RaycastWithoutSelfCollision(instance.transform.position, direction, maxDistance,
                instance.gameObject);
            if (!hit)
            {
                Debug.DrawRay(instance.transform.position, direction * maxDistance, Color.red );
            }
            else
            {
                Debug.DrawLine(instance.transform.position, hit.transform.position, Color.green ); 
            }
        }
    }
}