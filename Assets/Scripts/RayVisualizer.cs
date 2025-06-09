using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class MultiRayShooter : MonoBehaviour
{
    [SerializeField] int _rayCount = 12;
    [SerializeField] float SpreadAngle = 360f;
    [SerializeField] public float rayDistance = 20f;
    [SerializeField, Range(0.05f, 0.1f)] float _raywidth = 0.06f;
    [SerializeField] GameObject linePrefab;
    [SerializeField] private bool fadeOn = false;
    [SerializeField, Min(0)] private int fadeDuration = 100;
    [SerializeField] Color lineColor = Color.red;
    [SerializeField] LayerMask layer; // Exclude Selection layer to bypass big colliders

    [SerializeField] private bool showLines = false;

    private Movement characterMovement;
    private List<GameObject> lines = new List<GameObject>();
    [SerializeField] private int maxRaycastHits = 50;
    private RaycastHit2D[] raycastResults;

    // Hit detection storage for other systems to access
    private List<RaycastHit2D> allHits = new List<RaycastHit2D>();
    private Dictionary<string, RaycastHit2D> nearestHitsByTag = new Dictionary<string, RaycastHit2D>();

    // Public properties to access hit data
    public List<RaycastHit2D> AllHits => allHits;
    public Dictionary<string, RaycastHit2D> NearestHitsByTag => nearestHitsByTag;

    void Start()
    {
        characterMovement = GetComponent<Movement>();
        raycastResults = new RaycastHit2D[maxRaycastHits];
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

        direction.Normalize();
        float halfSpread = SpreadAngle / 2f;
        if (lines.Count == 0)
        {
            return;
        }

        for (int i = 0; i < _rayCount; i++)
        {
            float t = _rayCount > 1 ? i / (float)(_rayCount - 1) : 0.5f;
            float rotation_angle = Mathf.Lerp(-halfSpread, halfSpread, t);
            Quaternion resultantRotation = Quaternion.Euler(0, 0, rotation_angle);
            Transform LineTransform = lines[i].transform;
            LineRenderer line = lines[i].GetComponent<LineRenderer>();
            float angleRad = rotation_angle * Mathf.Deg2Rad;
            Vector2 rayDir = new Vector2(
                direction.x * Mathf.Cos(angleRad) - direction.y * Mathf.Sin(angleRad),
                direction.x * Mathf.Sin(angleRad) + direction.y * Mathf.Cos(angleRad)
            );
            Vector2 endPos = new Vector2(transform.position.x, transform.position.y) +
                             rayDir * rayDistance;
            Ray ray = new Ray(transform.position, rayDir);
            int hitCount = Physics2D.RaycastNonAlloc(this.transform.position, rayDir, raycastResults, rayDistance, layer.value);

            // Process all non-self hits from this ray
            RaycastHit2D closestHit = new RaycastHit2D();
            float closestDistance = float.MaxValue;

            for (int h = 0; h < hitCount; h++)
            {
                RaycastHit2D rayHit = raycastResults[h];
                if (rayHit.collider != null && rayHit.collider.gameObject != gameObject)
                {
                    // Store all valid hits
                    allHits.Add(rayHit);

                    // Update nearest hit by tag
                    string tag = rayHit.collider.tag;
                    if (!nearestHitsByTag.ContainsKey(tag) ||
                        rayHit.distance < nearestHitsByTag[tag].distance)
                    {
                        nearestHitsByTag[tag] = rayHit;
                    }

                    // Track closest hit for visual line
                    if (rayHit.distance < closestDistance)
                    {
                        closestHit = rayHit;
                        closestDistance = rayHit.distance;
                    }
                }
            }

            if (showLines)
            {
                if (closestHit.collider != null)
                {
                    // Update visual line to closest hit point
                    SetLineProperties(line, transform.position, closestHit.point, lineColor);
                }
                else
                {
                    // No hit, draw full ray
                    SetLineProperties(line, transform.position, endPos, lineColor);
                }

                if (fadeOn) Fade(line, fadeDuration);
            }

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
        float t = Mathf.Cos(2 * Mathf.PI * Time.fixedTime * duration * Mathf.Deg2Rad) * 0.4f + 0.5f;
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

    // Public method to reset the ray shooter when reused from object pool
    public void ResetRayShooter()
    {
        // Reinitialize arrays and references
        characterMovement = GetComponent<Movement>();
        raycastResults = new RaycastHit2D[maxRaycastHits];

        // Clear any existing line objects
        foreach (var line in lines)
        {
            if (line != null)
                Destroy(line);
        }
        lines.Clear();

        // Create new line objects
        for (int i = 0; i < _rayCount; i++)
        {
            GameObject line = Instantiate(linePrefab);
            line.hideFlags = HideFlags.HideInHierarchy;
            lines.Add(line);
        }

        // Clear hit data
        allHits.Clear();
        nearestHitsByTag.Clear();
    }
}