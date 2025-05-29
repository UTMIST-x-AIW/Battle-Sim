using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class MultiRayDetector : MonoBehaviour
{
    [SerializeField] int _rayCount = 16;
    [SerializeField] float SpreadAngle = 360f; // Changed to 360 for full circle detection
    [SerializeField] float _rayDistance = 10f;
    [SerializeField, Range(0.05f, 0.1f)] float _raywidth = 0.06f;
    [SerializeField] GameObject linePrefab;
    [SerializeField] private bool fadeOn;
    [SerializeField, Min(0)] private int fadeDuration = 100;
    [SerializeField] Color lineColor = Color.white;
    [SerializeField] bool enableVisualization = true;
    
    [Header("Detection Settings")]
    [SerializeField] LayerMask detectionLayers = -1; // All layers by default
    [SerializeField] bool useMovementDirection = true; // Whether to orient rays based on movement
    
    private Movement characterMovement;
    private List<GameObject> lines = new List<GameObject>();
    
    // Detection results - organized by layer
    private Dictionary<int, List<RaycastHit2D>> detectionResults = new Dictionary<int, List<RaycastHit2D>>();
    private Dictionary<int, RaycastHit2D> nearestByLayer = new Dictionary<int, RaycastHit2D>();

    void Start()
    {
        characterMovement = GetComponent<Movement>();
        
        if (enableVisualization && linePrefab != null)
        {
            for (int i = 0; i < _rayCount; i++)
            {
                GameObject line = Instantiate(linePrefab);
                line.hideFlags = HideFlags.HideInHierarchy;
                lines.Add(line);
            }
        }
    }

    void Update()
    {
        if (useMovementDirection && characterMovement != null)
        {
            UpdateDetectionWithDirection(characterMovement.lastdirection);
        }
        else
        {
            UpdateDetectionOmnidirectional();
        }
    }

    void UpdateDetectionWithDirection(Vector2 direction)
    {
        if (direction == Vector2.zero) direction = Vector2.up; // Default direction
        direction.Normalize();
        
        ClearDetectionResults();
        
        float halfSpread = SpreadAngle / 2f;
        for (int i = 0; i < _rayCount; i++)
        {
            float t = _rayCount > 1 ? i / (float)(_rayCount - 1) : 0.5f;
            float rotation_angle = Mathf.Lerp(-halfSpread, halfSpread, t);
            
            float angleRad = rotation_angle * Mathf.Deg2Rad;
            Vector2 rayDir = new Vector2(
                direction.x * Mathf.Cos(angleRad) - direction.y * Mathf.Sin(angleRad),
                direction.x * Mathf.Sin(angleRad) + direction.y * Mathf.Cos(angleRad)
            );
            
            PerformRaycast(rayDir, i);
        }
    }
    
    void UpdateDetectionOmnidirectional()
    {
        ClearDetectionResults();
        
        for (int i = 0; i < _rayCount; i++)
        {
            // 360 degree spread
            float angle = (i / (float)_rayCount) * 360f * Mathf.Deg2Rad;
            Vector2 rayDir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            
            PerformRaycast(rayDir, i);
        }
    }
    
    void PerformRaycast(Vector2 rayDirection, int rayIndex)
    {
        Vector2 startPos = transform.position;
        Vector2 endPos = startPos + rayDirection * _rayDistance;
        
        // Cast ray for each layer we're interested in
        for (int layer = 0; layer < 32; layer++)
        {
            if ((detectionLayers.value & (1 << layer)) != 0)
            {
                LayerMask layerMask = 1 << layer;
                RaycastHit2D hit = Physics2D.Raycast(startPos, rayDirection, _rayDistance, layerMask);
                
                if (hit.collider != null && hit.collider.gameObject != gameObject)
                {
                    // Store detection result
                    if (!detectionResults.ContainsKey(layer))
                    {
                        detectionResults[layer] = new List<RaycastHit2D>();
                    }
                    detectionResults[layer].Add(hit);
                    
                    // Update nearest for this layer
                    if (!nearestByLayer.ContainsKey(layer) || hit.distance < nearestByLayer[layer].distance)
                    {
                        nearestByLayer[layer] = hit;
                    }
                }
            }
        }
        
        // Visualization
        if (enableVisualization && rayIndex < lines.Count)
        {
            LineRenderer line = lines[rayIndex].GetComponent<LineRenderer>();
            SetLineProperties(line, startPos, endPos, lineColor);
            if (fadeOn) Fade(line, fadeDuration);
        }
    }
    
    void ClearDetectionResults()
    {
        detectionResults.Clear();
        nearestByLayer.Clear();
    }
    
    // Public API for detection results
    public RaycastHit2D GetNearestHit(LayerMask layerMask)
    {
        RaycastHit2D nearest = new RaycastHit2D();
        float nearestDistance = float.MaxValue;
        
        for (int layer = 0; layer < 32; layer++)
        {
            if ((layerMask.value & (1 << layer)) != 0 && nearestByLayer.ContainsKey(layer))
            {
                if (nearestByLayer[layer].distance < nearestDistance)
                {
                    nearest = nearestByLayer[layer];
                    nearestDistance = nearestByLayer[layer].distance;
                }
            }
        }
        
        return nearest;
    }
    
    public List<RaycastHit2D> GetAllHits(LayerMask layerMask)
    {
        List<RaycastHit2D> allHits = new List<RaycastHit2D>();
        
        for (int layer = 0; layer < 32; layer++)
        {
            if ((layerMask.value & (1 << layer)) != 0 && detectionResults.ContainsKey(layer))
            {
                allHits.AddRange(detectionResults[layer]);
            }
        }
        
        return allHits;
    }
    
    public RaycastHit2D GetNearestHitWithTag(string tag)
    {
        RaycastHit2D nearest = new RaycastHit2D();
        float nearestDistance = float.MaxValue;
        
        foreach (var layerResults in detectionResults.Values)
        {
            foreach (var hit in layerResults)
            {
                if (hit.collider.CompareTag(tag) && hit.distance < nearestDistance)
                {
                    nearest = hit;
                    nearestDistance = hit.distance;
                }
            }
        }
        
        return nearest;
    }
    
    public void SetDetectionRange(float range)
    {
        _rayDistance = range;
    }
    
    public void SetRayCount(int count)
    {
        _rayCount = count;
        
        // Recreate visualization lines if needed
        if (enableVisualization && lines.Count != _rayCount)
        {
            foreach (GameObject line in lines)
            {
                if (line != null) Destroy(line);
            }
            lines.Clear();
            
            if (linePrefab != null)
            {
                for (int i = 0; i < _rayCount; i++)
                {
                    GameObject line = Instantiate(linePrefab);
                    line.hideFlags = HideFlags.HideInHierarchy;
                    lines.Add(line);
                }
            }
        }
    }
    
    public void SetDetectionLayers(LayerMask layers)
    {
        detectionLayers = layers;
    }

    void SetLineProperties(LineRenderer line, Vector3 start, Vector3 end, Color constColor)
    {
        line.SetPositions(new Vector3[] { start, end });
        line.startWidth = _raywidth;
        line.endWidth = _raywidth;
        line.material.color = constColor; 
    }

    void Fade(LineRenderer line, int duration)
    {
        Color initialColor = line.material.color;
        float t = Mathf.Cos(2 * Mathf.PI * Time.fixedTime * duration * Mathf.Deg2Rad)*0.4f + 0.5f;
        t = Mathf.Clamp(t, 0f, 0.2f);
        line.material.color = new Color(initialColor.r, initialColor.g, initialColor.b, t);
    }
    
    private void OnDisable()
    {
        foreach (var line in lines)
        {
            if (line != null) Destroy(line);
        }
        lines.Clear();
    }
}