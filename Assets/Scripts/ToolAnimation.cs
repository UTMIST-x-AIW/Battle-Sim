using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolAnimation : MonoBehaviour
{
    [SerializeField] public WaypointEntry[] waypointEntries = new WaypointEntry[4];
    [SerializeField] private float axeSwingSpeed = 10f;
    [SerializeField] private float axeSwingAngle = 45f;
    [SerializeField] private float swordSwingSpeed = 5f;
    [SerializeField] private float swordSwingAngle = 45f;
    
    private Coroutine currentSwingCoroutine;
    private bool isSwinging = false;
    
    // Rotation at the start of animation
    private Quaternion startRotation;

    public enum ToolType {
        Axe,
        Sword
    }
    
    /// <summary>
    /// Returns whether a tool swing animation is currently playing
    /// </summary>
    public bool IsAnimationPlaying()
    {
        return isSwinging;
    }
    
    /// <summary>
    /// Triggers a simple tool swing animation.
    /// </summary>
    public void SwingTool(ToolType toolType)
    {
        // If already swinging, don't start a new animation
        if (isSwinging)
            return;
            
        // Store the current rotation as our starting point
        startRotation = transform.rotation;
        
        isSwinging = true;

        switch (toolType) {
            case ToolType.Axe:
                currentSwingCoroutine = StartCoroutine(SwingToolCoroutine(axeSwingSpeed, axeSwingAngle));
                break;
            case ToolType.Sword:
                currentSwingCoroutine = StartCoroutine(SwingToolCoroutine(swordSwingSpeed, swordSwingAngle));
                break;
        }
    }
    
    private IEnumerator SwingToolCoroutine(float swingSpeed, float swingAngle)
    {
        // Swing down phase - move from start to start+swing
        float elapsed = 0;
        float duration = 0.1f; // Duration of the down swing
        Quaternion targetRotation = startRotation * Quaternion.Euler(0, 0, swingAngle);
        
        while (elapsed < duration && isSwinging)
        {
            // Use slerp for smooth rotation between start and target
            transform.rotation = Quaternion.Slerp(
                startRotation, 
                targetRotation, 
                elapsed / duration
            );
            
            elapsed += Time.deltaTime * swingSpeed;
            yield return null;
        }
        
        // Ensure we reach the target
        if (isSwinging)
        {
            transform.rotation = targetRotation;
        }
        
        // Swing back phase - move from target back to start
        elapsed = 0;
        duration = 0.15f; // Slightly longer duration for return swing
        
        while (elapsed < duration && isSwinging)
        {
            // Use slerp for smooth rotation between target and start
            transform.rotation = Quaternion.Slerp(
                targetRotation,
                startRotation,
                elapsed / duration
            );
            
            elapsed += Time.deltaTime * swingSpeed;
            yield return null;
        }
        
        // Ensure we end at the exact start rotation
        transform.rotation = startRotation;
        
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