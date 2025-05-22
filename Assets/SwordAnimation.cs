using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwordAnimation : MonoBehaviour
{
    [SerializeField] public WaypointEntry[] waypointEntries = new WaypointEntry[4];
    [SerializeField] private float swingSpeed = 5f; // Reduced speed for smoother animation
    [SerializeField] private float swingAngle = 30f; // How far to rotate down in degrees
    [SerializeField] private bool debugMode = true; // Toggle for debug logs
    
    private Coroutine currentSwingCoroutine;
    private bool isSwinging = false;
    private int swingCount = 0; // To track number of swings
    
    // Rotation at the start of animation
    private Quaternion startRotation;
    
    /// <summary>
    /// Returns whether a sword swing animation is currently playing
    /// </summary>
    public bool IsAnimationPlaying()
    {
        return isSwinging;
    }
    
    private void Update()
    {
        // Add periodic debug in Update to see if animation is running
        if (isSwinging && debugMode && Time.frameCount % 30 == 0)
        {
            Debug.Log($"[SwordAnimation] Animation still running for swing #{swingCount}, frame {Time.frameCount}");
        }
    }
    
    /// <summary>
    /// Triggers a simple sword swing animation.
    /// </summary>
    public void SwingSword()
    {
        // If already swinging, don't start a new animation
        if (isSwinging)
        {
            if (debugMode) Debug.Log($"[SwordAnimation] Swing request ignored - already swinging (count: {swingCount})");
            return;
        }
            
        // Start a new swing
        swingCount++;
        if (debugMode) Debug.Log($"[SwordAnimation] Starting swing #{swingCount} (Time: {Time.time:F3})");
        
        // Store the current rotation as our starting point
        startRotation = transform.rotation;
        
        isSwinging = true; // Set flag BEFORE starting coroutine
        
        try
        {
            currentSwingCoroutine = StartCoroutine(SwingSwordCoroutine());
        }
        catch (Exception e)
        {
            if (debugMode) Debug.LogError($"[SwordAnimation] Error starting swing coroutine: {e.Message}");
            isSwinging = false;
        }
    }
    
    private IEnumerator SwingSwordCoroutine()
    {
        // Double-check that isSwinging is true
        if (!isSwinging)
        {
            if (debugMode) Debug.LogWarning("[SwordAnimation] Flag inconsistency detected!");
            isSwinging = true;
        }
        
        // Better animation approach - animate relative to startRotation
        // We'll use absolute rotations instead of incremental to avoid jittering
        
        if (debugMode) Debug.Log($"[SwordAnimation] Swing #{swingCount} down phase starting (Time: {Time.time:F3})");
        
        // Swing down phase - move from start to start+swing
        float elapsed = 0;
        float duration = 0.1f; // Duration of the down swing
        Quaternion targetRotation = startRotation * Quaternion.Euler(0, 0, swingAngle);
        
        while (elapsed < duration && isSwinging)
        {
            try
            {
                // Use slerp for smooth rotation between start and target
                transform.rotation = Quaternion.Slerp(
                    startRotation, 
                    targetRotation, 
                    elapsed / duration
                );
            }
            catch (Exception e)
            {
                if (debugMode) Debug.LogError($"[SwordAnimation] Error in swing down phase: {e.Message}");
                CleanupAnimation();
                yield break;
            }
            
            elapsed += Time.deltaTime * swingSpeed;
            yield return null;
        }
        
        // Ensure we reach the target
        if (isSwinging)
        {
            transform.rotation = targetRotation;
        }
        
        // Log progress between phases
        if (debugMode) Debug.Log($"[SwordAnimation] Swing #{swingCount} down phase completed (Time: {Time.time:F3})");
        
        // Swing back phase - move from target back to start
        elapsed = 0;
        duration = 0.15f; // Slightly longer duration for return swing
        
        if (debugMode) Debug.Log($"[SwordAnimation] Swing #{swingCount} back phase starting (Time: {Time.time:F3})");
        
        while (elapsed < duration && isSwinging)
        {
            try
            {
                // Use slerp for smooth rotation between target and start
                transform.rotation = Quaternion.Slerp(
                    targetRotation,
                    startRotation,
                    elapsed / duration
                );
            }
            catch (Exception e)
            {
                if (debugMode) Debug.LogError($"[SwordAnimation] Error in swing back phase: {e.Message}");
                CleanupAnimation();
                yield break;
            }
            
            elapsed += Time.deltaTime * swingSpeed;
            yield return null;
        }
        
        try
        {
            // Ensure we end at the exact start rotation
            transform.rotation = startRotation;
        }
        catch (Exception e)
        {
            if (debugMode) Debug.LogError($"[SwordAnimation] Error in final rotation reset: {e.Message}");
        }
        
        if (debugMode) Debug.Log($"[SwordAnimation] Swing #{swingCount} completed (Time: {Time.time:F3})");
        
        CleanupAnimation();
    }
    
    private void CleanupAnimation()
    {
        currentSwingCoroutine = null;
        isSwinging = false;
    }
    
    private void OnDisable()
    {
        // Make sure we clean up if disabled
        if (currentSwingCoroutine != null)
        {
            StopCoroutine(currentSwingCoroutine);
            currentSwingCoroutine = null;
        }
        isSwinging = false;
    }
}

[Serializable]
public struct WaypointEntry {
    [SerializeField] 
    public Transform waypointTransform;
    
}