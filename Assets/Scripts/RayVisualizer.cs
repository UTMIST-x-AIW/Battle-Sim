using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class MultiRayShooter : MonoBehaviour
{
    [SerializeField] int _rayCount = 0;
    [SerializeField] float SpreadAngle = 30f;
    [SerializeField] float _rayDistance = 10f;
    [SerializeField, Range(0.05f, 0.1f)] float _raywidth = 0.06f;
    [SerializeField] GameObject linePrefab;
    [SerializeField] private bool fadeOn;
    [SerializeField, Min(0)] private int fadeDuration = 100;
    [SerializeField] Color lineColor = Color.white;
    [SerializeField] LayerMask layer;
    
    private Movement characterMovement;
    private List<GameObject> lines = new List<GameObject>();
    
    // Hit detection storage for other systems to access
    private List<RaycastHit2D> allHits = new List<RaycastHit2D>();
    private Dictionary<string, RaycastHit2D> nearestHitsByTag = new Dictionary<string, RaycastHit2D>();
    
    // Public properties to access hit data
    public List<RaycastHit2D> AllHits => allHits;
    public Dictionary<string, RaycastHit2D> NearestHitsByTag => nearestHitsByTag;

    void Start()
    {
        characterMovement = GetComponent<Movement>();
        for (int i = 0; i < _rayCount; i++)
        {
            GameObject line = Instantiate(linePrefab);
            line.hideFlags = HideFlags.HideInHierarchy;
            lines.Add(line);
        }
    }

    void Update()
    {
        // For 360-degree detection, direction doesn't matter, use fixed direction
        if (SpreadAngle >= 360f)
        {
            UpdateTargetRotations(Vector2.right); // Use any direction since we cover 360°
        }
        else
        {
            // Original behavior - follow character movement direction
            Vector2 CharacterMovDir = characterMovement.lastdirection;
            if (characterMovement != null)
            {
                UpdateTargetRotations(characterMovement.lastdirection);
            }
        }
    }


    void UpdateTargetRotations(Vector2 direction)
    {
        // Clear previous frame's data
        allHits.Clear();
        nearestHitsByTag.Clear();
        
        /*
        foreach (GameObject line in lines)
        {
            if (line.GetComponent<LineRenderer>().enabled == false) line.GetComponent<LineRenderer>().enabled = true;
        }*/
        
        direction.Normalize();
        float halfSpread = SpreadAngle / 2f;
        for (int i = 0; i < _rayCount; i++)
        {
            float t = _rayCount > 1 ? i / (float)(_rayCount - 1) : 0.5f;
            float rotation_angle = Mathf.Lerp(-halfSpread, halfSpread, t);
            Quaternion resultantRotation =  Quaternion.Euler(0,0,rotation_angle);
            Transform LineTransform = lines[i].transform;
            LineRenderer line = lines[i].GetComponent<LineRenderer>();
            float angleRad = rotation_angle * Mathf.Deg2Rad;
            Vector2 rayDir = new Vector2(
                direction.x * Mathf.Cos(angleRad) - direction.y * Mathf.Sin(angleRad),
                direction.x * Mathf.Sin(angleRad) + direction.y * Mathf.Cos(angleRad)
            );
            Vector2 endPos =  new Vector2(transform.position.x,transform.position.y) +
                             rayDir * _rayDistance;
            Ray ray = new Ray(transform.position, rayDir);
            RaycastHit2D hit = Physics2D.Raycast(this.transform.position, rayDir, _rayDistance, layer.value);
            
            if (hit.collider != null)
            {
                Debug.Log(hit.collider.gameObject.name + " was hit.");
                
                // Store all hits
                allHits.Add(hit);
                
                // Store nearest hit by tag
                string tag = hit.collider.tag;
                if (!nearestHitsByTag.ContainsKey(tag) || 
                    hit.distance < nearestHitsByTag[tag].distance)
                {
                    nearestHitsByTag[tag] = hit;
                }
                
                // Update visual line to hit point
                SetLineProperties(line, transform.position, hit.point, lineColor);
            }
            else
            {
                // No hit, draw full ray
                SetLineProperties(line, transform.position, endPos, lineColor);
            }
            
            if (fadeOn) Fade(line, fadeDuration);
        }
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
            Destroy(line);
        }

        lines.Clear();
    }

    // Convenience methods for accessing specific object types
    public RaycastHit2D GetNearestHitByTag(string tag)
    {
        return nearestHitsByTag.ContainsKey(tag) ? nearestHitsByTag[tag] : new RaycastHit2D();
    }
    
    public bool HasHitWithTag(string tag)
    {
        return nearestHitsByTag.ContainsKey(tag);
    }
    
    public List<RaycastHit2D> GetAllHitsWithTag(string tag)
    {
        List<RaycastHit2D> hitsWithTag = new List<RaycastHit2D>();
        foreach (var hit in allHits)
        {
            if (hit.collider != null && hit.collider.CompareTag(tag))
            {
                hitsWithTag.Add(hit);
            }
        }
        return hitsWithTag;
    }
    
    // Configure for 360-degree detection (for Creature system)
    public void ConfigureFor360Detection(int rayCount = 36, float maxRange = 20f, LayerMask detectionLayers = default)
    {
        _rayCount = rayCount;
        SpreadAngle = 360f;
        _rayDistance = maxRange;
        layer = detectionLayers;
        fadeOn = false; // Disable visual effects for performance
        
        // Create a simple line prefab if none exists
        if (linePrefab == null)
        {
            linePrefab = new GameObject("RayLine");
            LineRenderer lr = linePrefab.AddComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.material.color = Color.white;
            lr.startWidth = 0.1f;
            lr.endWidth = 0.1f;
            lr.positionCount = 2;
            lr.enabled = false; // Start disabled for performance
        }
        
        // Recreate lines for new ray count
        foreach (var line in lines)
        {
            if (line != null) Destroy(line);
        }
        lines.Clear();
        
        for (int i = 0; i < _rayCount; i++)
        {
            GameObject line = Instantiate(linePrefab);
            line.hideFlags = HideFlags.HideInHierarchy;
            lines.Add(line);
        }
    }
}