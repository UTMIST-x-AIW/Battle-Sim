using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;

namespace Utils
{
    public static class Physics2DExtensions
    {
        public static RaycastHit2D RaycastWithoutSelfCollision(Vector2 origin, Vector2 direction,
            float maxDistance, GameObject self, ContactFilter2D contactFilter= default)
        {
            RaycastHit2D[] rayHits = new RaycastHit2D[2];
            int hitCount = Physics2D.Raycast(origin, direction, contactFilter, rayHits, maxDistance);

            for (int i = 0; i < hitCount; i++)
            {
                if (rayHits[i].collider.gameObject != self)
                    return rayHits[i];
            }

            return new RaycastHit2D();
        }
    }

    /*
    public static class RaycastCommandExtensions
    {
        public static unsafe JobHandle ScheduleBatch2D(
            NativeArray<RaycastCommand> commands,
            NativeArray<RaycastHit2D> results,
            int minCommandsPerJob,
            int maxHits,
            JobHandle dependsOn = default (JobHandle))
        {
            if (maxHits < 1)
            {
                Debug.LogWarning((object) "maxHits should be greater than 0.");
                return new JobHandle();
            }
            if (results.Length < maxHits * commands.Length)
            {
                Debug.LogWarning((object) "The supplied results buffer is too small, there should be at least maxHits space per each command in the batch.");
                return new JobHandle();
            }
            BatchQueryJob<RaycastCommand, RaycastHit2D> output = new BatchQueryJob<RaycastCommand, RaycastHit2D>(commands, results);
            JobsUtility.JobScheduleParameters parameters = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf<BatchQueryJob<RaycastCommand, RaycastHit2D>>(ref output), BatchQueryJobStruct<BatchQueryJob<RaycastCommand, RaycastHit2D>>.Initialize(), dependsOn, ScheduleMode.Parallel);
            return ScheduleRaycastBatch(ref parameters, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks<RaycastCommand>(commands), commands.Length, NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks<RaycastHit2D>(results), results.Length, minCommandsPerJob, maxHits);
        }
        private static unsafe JobHandle ScheduleRaycastBatch(
            ref JobsUtility.JobScheduleParameters parameters,
            void* commands,
            int commandLen,
            void* result,
            int resultLen,
            int minCommandsPerJob,
            int maxHits)
        {
            JobHandle ret;
            ScheduleRaycastBatch_Injected(ref parameters, commands, commandLen, result, resultLen, minCommandsPerJob, maxHits, out ret);
            return ret;
        }
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe void ScheduleRaycastBatch_Injected(
            ref JobsUtility.JobScheduleParameters parameters,
            void* commands,
            int commandLen,
            void* result,
            int resultLen,
            int minCommandsPerJob,
            int maxHits,
            out JobHandle ret);
        
        public static JobHandle ScheduleBatch(
            NativeArray<RaycastCommand> commands,
            NativeArray<RaycastHit2D> results,
            int minCommandsPerJob,
            JobHandle dependsOn = default (JobHandle))
        {
            return ScheduleBatch2D(commands, results, minCommandsPerJob, 1, dependsOn);
        }
    }
    */
    
}