using UnityEngine;
using System.Diagnostics;

public class PerformanceTest : MonoBehaviour
{
    [Header("Performance Testing")]
    public int testIterations = 1000;
    public float testRadius = 20f;
    public LayerMask testLayerMask = -1;
    
    [Header("Results")]
    public float overlapCircleTime;
    public float raycastTime;
    public float speedupRatio;

    void Start()
    {
        // Run performance comparison
        CompareDetectionMethods();
    }

    void CompareDetectionMethods()
    {
        // Test OverlapCircle method
        Stopwatch sw = new Stopwatch();
        
        // Warm up
        for (int i = 0; i < 100; i++)
        {
            Physics2D.OverlapCircleAll(transform.position, testRadius, testLayerMask);
        }
        
        // Test OverlapCircleAll (old method)
        sw.Start();
        for (int i = 0; i < testIterations; i++)
        {
            Physics2D.OverlapCircleAll(transform.position, testRadius, testLayerMask);
        }
        sw.Stop();
        overlapCircleTime = sw.ElapsedMilliseconds;
        
        sw.Reset();
        
        // Warm up raycast
        for (int i = 0; i < 100; i++)
        {
            Physics2D.Raycast(transform.position, Vector2.up, testRadius, testLayerMask);
        }
        
        // Test Raycast method (new method) - simulate 16 rays like our creature detection
        sw.Start();
        for (int i = 0; i < testIterations; i++)
        {
            for (int rayIndex = 0; rayIndex < 16; rayIndex++)
            {
                float angle = (360f / 16) * rayIndex * Mathf.Deg2Rad;
                Vector2 rayDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Physics2D.Raycast(transform.position, rayDirection, testRadius, testLayerMask);
            }
        }
        sw.Stop();
        raycastTime = sw.ElapsedMilliseconds;
        
        // Calculate speedup
        speedupRatio = overlapCircleTime / raycastTime;
        
        UnityEngine.Debug.Log($"Performance Test Results:");
        UnityEngine.Debug.Log($"OverlapCircleAll: {overlapCircleTime}ms for {testIterations} iterations");
        UnityEngine.Debug.Log($"Raycast (16 rays): {raycastTime}ms for {testIterations} iterations");
        UnityEngine.Debug.Log($"Speedup: {speedupRatio:F2}x faster");
        UnityEngine.Debug.Log($"Per creature per frame old: {overlapCircleTime / (float)testIterations:F4}ms");
        UnityEngine.Debug.Log($"Per creature per frame new: {raycastTime / (float)testIterations:F4}ms");
    }
} 