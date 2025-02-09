using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class MultiRayShooter : MonoBehaviour
{
    [SerializeField] int lineCount = 5;
    [SerializeField] float SpreadAngle = 30f;
    [SerializeField] float LineDistance = 10f;
    [SerializeField, Range(0.05f, 0.3f)] float linewidth = 0.2f;
    [SerializeField] GameObject linePrefab;
    [SerializeField, Min(0)] private int fade_duration = 100;

    private DebugMovement characterMovement;
    private List<GameObject> lines = new List<GameObject>();
    private List<Quaternion> lineRotations = new List<Quaternion>();
    private float rotationSpeed = 5f;
    

    void Start()
    {
        characterMovement = GetComponent<DebugMovement>();
        for (int i = 0; i < lineCount; i++)
        {
            GameObject line = Instantiate(linePrefab);
            lines.Add(line);
            lineRotations.Add(Quaternion.identity);
        }
    }

    void Update()
    {
        if (characterMovement != null && characterMovement.lastdirection != Vector2.zero)
        {
            ShootRays(characterMovement.lastdirection);
        }
    }

    void SetLineProperties(LineRenderer line, Vector3 start, Vector3 end, Color constColor)
    {
        line.SetPositions(new Vector3[] { start, end });
        line.startWidth = linewidth;
        line.endWidth = linewidth;
        line.material.color = constColor; // Simplified color setup
    }

    void ShootRays(Vector2 direction)
    {
        foreach (GameObject line in lines)
        {
            if (line.GetComponent<LineRenderer>().enabled == false) line.GetComponent<LineRenderer>().enabled = true;
        }
        direction.Normalize();
        float halfSpread = SpreadAngle / 2f;
        for (int i = 0; i < lineCount; i++)
        {
            float t = lineCount > 1 ? i / (float)(lineCount - 1) : 0.5f;
            float angle = Mathf.Lerp(-halfSpread, halfSpread, t);
            float angleRad = angle * Mathf.Deg2Rad;
            Vector2 rayDir = new Vector2(
                direction.x * Mathf.Cos(angleRad) - direction.y * Mathf.Sin(angleRad),
                direction.x * Mathf.Sin(angleRad) + direction.y * Mathf.Cos(angleRad)
            );
            /*Quaternion resultantRotation =  Quaternion.Euler(0,0,angle);
            lineRotations[i] = resultantRotation;
            Quaternion current_rotation = lines[i].transform.rotation;
            lines[i].transform.rotation = Quaternion.Slerp(current_rotation, resultantRotation, Time.deltaTime * rotationSpeed);*/
            Vector2 endPos = new Vector2(transform.position.x, transform.position.y) + rayDir * LineDistance;

            LineRenderer line = lines[i].GetComponent<LineRenderer>();
            SetLineProperties(line, transform.position, endPos, Color.yellow);
            FinalFade(line, fade_duration);
        }
    }

    void FinalFade(LineRenderer Line, int duration)
    {
        Color initialColor = Line.material.color;
        float t = Mathf.Cos(2 * Mathf.PI * Time.fixedTime * duration * Mathf.Deg2Rad)*0.4f + 0.5f;
        t = Mathf.Clamp(t, 0f, 1.5f);
        Line.material.color = new Color(initialColor.r, initialColor.g, initialColor.b, t);
    }

    private void OnDisable()
    {
        foreach (var line in lines)
        {
            Destroy(line);
        }

        lines.Clear();
        lineRotations.Clear();
    }
}