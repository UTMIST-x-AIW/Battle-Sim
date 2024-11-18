using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    private float attackRange = 25f;
    private float attackCooldown = 1f;
    private float attackTimer = 0f;

    private int maxColliders = 3;

    void Update()
    {
        if ((Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)) && attackTimer <= 0f)
        {
            Attack();
            attackTimer = attackCooldown;
        }

        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
        }
    }

    void Attack()
    {
        GameObject closestPlayer = null;
        float closestDistance = attackRange;
        Collider[] colliders = new Collider[maxColliders];
        int numColliders = Physics.OverlapSphereNonAlloc(transform.position, attackRange, colliders);

        foreach (Collider other in colliders)
        {
            if (other != null)
            {
                if (other.gameObject.CompareTag("Player") && other.gameObject != gameObject)
                {
                    float distance = Vector2.Distance(transform.position, other.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestPlayer = other.gameObject;
                    }
                }
            }
        }

        if (closestPlayer != null)
        {
            Destroy(closestPlayer);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.431f, 0f);;
        int numPoints = 50;
        float angleStep = 360f / numPoints;
        Vector3 startPoint = new Vector3(
            transform.position.x + Mathf.Cos(0) * attackRange, transform.position.y + Mathf.Sin(0) * attackRange, 0);
    
        for (int i = 1; i <= numPoints; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 nextPoint = new Vector3(
                transform.position.x + Mathf.Cos(
                    angle) * attackRange, transform.position.y + Mathf.Sin(angle) * attackRange, 0);
            Gizmos.DrawLine(startPoint, nextPoint);
            startPoint = nextPoint;
        }
    }
}