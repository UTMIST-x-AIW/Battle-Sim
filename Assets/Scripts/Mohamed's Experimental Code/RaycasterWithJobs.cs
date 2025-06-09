using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;


[BurstCompile]
public class RaycastsWithJobs
{
    private NativeArray<RaycastCommand> _commands;
    private NativeArray<RaycastHit> _results;
    private bool _initialized;
    private int _lastTotalRayCount;

    public void Initialize(int totalRayCount)
    {
        if (_initialized && _lastTotalRayCount == totalRayCount)
            return;

        Dispose();
        _commands = new NativeArray<RaycastCommand>(totalRayCount, Allocator.Persistent);
        _results = new NativeArray<RaycastHit>(totalRayCount, Allocator.Persistent);
        _initialized = true;
        _lastTotalRayCount = totalRayCount;
    }

    public NativeArray<RaycastHit> RayCastCommand(
        NativeArray<float3> creaturePositions,
        int raysPerCreature,
        float maxDistance,
        int minCommandsPerJob = 4)
    {
        int numCreatures = creaturePositions.Length;
        int totalRayCount = numCreatures * raysPerCreature;

        Initialize(totalRayCount);

        var buildJob = new BuildRayCommandsJob
        {
            Commands = _commands,
            Origins = creaturePositions,
            MaxDistance = maxDistance,
            RaysPerCreature = raysPerCreature
        };

        JobHandle buildHandle = buildJob.Schedule(totalRayCount, minCommandsPerJob);
        JobHandle raycastHandle = RaycastCommand.ScheduleBatch(_commands, _results, minCommandsPerJob, buildHandle);
        raycastHandle.Complete();
        for (var i = 0; i < _results.Length; i++)
        {
            var hit = _results[i];
            if (hit.collider != null)
            {
                Debug.DrawRay(_commands[i].from, hit.point, Color.green);
            }
            else
            {
                Debug.DrawRay(_commands[i].from, Vector3.up, Color.magenta);
            }
        }
        return _results;
    }

    public void Dispose()
    {
        if (_initialized)
        {
            if (_commands.IsCreated) _commands.Dispose();
            if (_results.IsCreated) _results.Dispose();
            _initialized = false;
        }
    }

}
[BurstCompile]
public struct BuildRayCommandsJob : IJobParallelFor
{
    public NativeArray<RaycastCommand> Commands;
    [ReadOnly] public NativeArray<float3> Origins;
    public float MaxDistance;
    public int RaysPerCreature;

    public void Execute(int i)
    {
        int creatureIndex = i / RaysPerCreature;
        int rayIndex = i % RaysPerCreature;

        float t = RaysPerCreature > 1 ? rayIndex / (float)(RaysPerCreature - 1) : 0.5f;
        float angle = math.lerp(-math.PI, math.PI, t);
        float3 direction = math.mul(quaternion.AxisAngle(math.up(), angle), math.right());

        Commands[i] = new RaycastCommand(Origins[creatureIndex], direction, QueryParameters.Default, MaxDistance);
    }
}
