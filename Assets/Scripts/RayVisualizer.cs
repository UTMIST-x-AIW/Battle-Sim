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
    
    private DebugMovement characterMovement;
    private List<GameObject> lines = new List<GameObject>();
    

    void Start()
    {
        characterMovement = GetComponent<DebugMovement>();
        for (int i = 0; i < _rayCount; i++)
        {
            GameObject line = Instantiate(linePrefab);
            line.hideFlags = HideFlags.HideInHierarchy;
            lines.Add(line);
        }
    }

    void Update()
    {
        Vector2 CharacterMovDir = characterMovement.lastdirection;
        if (characterMovement != null)
        {
            UpdateTargetRotations(characterMovement.lastdirection);
        }
    }


    void UpdateTargetRotations(Vector2 direction)
    {
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
            RaycastHit2D hit = Physics2D.Raycast(this.transform.position, endPos,_rayDistance,layer.value);
            if (hit)
            {
                Debug.Log(hit.collider.gameObject.name + " was hit.");
            }
            SetLineProperties(line, transform.position, endPos, lineColor);
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
}