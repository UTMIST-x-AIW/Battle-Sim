using UnityEngine;
using System.Collections.Generic;

public class PerformanceTest : MonoBehaviour
{
    [Header("Performance Testing")]
    public bool showPerformanceUI = true;
    public KeyCode toggleDetectionKey = KeyCode.T;
    
    private float deltaTime = 0.0f;
    private List<Creature> allCreatures = new List<Creature>();
    private bool rayDetectionActive = true;
    
    void Start()
    {
        // Find all creatures in the scene
        RefreshCreatureList();
        
        // Set initial detection method
        SetDetectionMethod(rayDetectionActive);
    }
    
    void Update()
    {
        // Calculate FPS
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        
        // Toggle detection method
        if (Input.GetKeyDown(toggleDetectionKey))
        {
            ToggleDetectionMethod();
        }
        
        // Refresh creature list periodically (in case new ones spawn)
        if (Time.frameCount % 60 == 0) // Every 60 frames
        {
            RefreshCreatureList();
        }
    }
    
    void RefreshCreatureList()
    {
        allCreatures.Clear();
        allCreatures.AddRange(FindObjectsOfType<Creature>());
    }
    
    void ToggleDetectionMethod()
    {
        rayDetectionActive = !rayDetectionActive;
        SetDetectionMethod(rayDetectionActive);
        Debug.Log($"Switched to {(rayDetectionActive ? "Ray-based" : "OverlapCircle")} detection");
    }
    
    void SetDetectionMethod(bool useRayDetection)
    {
        foreach (Creature creature in allCreatures)
        {
            if (creature != null)
            {
                // Use reflection to set the private useRayDetection field
                var field = typeof(Creature).GetField("useRayDetection", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(creature, useRayDetection);
            }
        }
    }
    
    void OnGUI()
    {
        if (!showPerformanceUI) return;
        
        int w = Screen.width, h = Screen.height;
        GUIStyle style = new GUIStyle();
        Rect rect = new Rect(0, 0, w, h * 2 / 100);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = h * 2 / 100;
        style.normal.textColor = Color.white;
        
        float msec = deltaTime * 1000.0f;
        float fps = 1.0f / deltaTime;
        string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
        text += $"\nCreatures: {allCreatures.Count}";
        text += $"\nDetection: {(rayDetectionActive ? "Ray-based (360°)" : "OverlapCircle (Progressive)")}";
        text += $"\nPress {toggleDetectionKey} to toggle detection method";
        
        GUI.Label(rect, text, style);
    }
} 