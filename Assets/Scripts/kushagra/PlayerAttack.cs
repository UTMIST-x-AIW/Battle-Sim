using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public float attackRange = 1f;
    [SerializeField] public float attackCooldown = 5f; // Cooldown time in seconds
    private float attackTimer = 0f; // Timer to track cooldown

    private int maxColliders = 3;

    private float rockBreakHpIncrease = 50f;
    private float treeCutCooldownReduction = 1f;
    private float playerKillDamageIncrease = 25f;

    void Update()
    {
        // Decrease the attack timer over time
        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
        }

        // Check for attack input and ensure cooldown is over
        if ((Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift) || Input.GetKeyDown(KeyCode.Space)) && attackTimer <= 0f)
        {
            Attack();
            attackTimer = attackCooldown; // Reset the timer after an attack
        }
    }

    void Attack()
    {
        GameObject closestPlayer = null;
        float closestDistance = attackRange;
        Collider2D[] colliders = new Collider2D[maxColliders];
        int numColliders = Physics2D.OverlapCircleNonAlloc(transform.position, attackRange, colliders);

        foreach (Collider2D other in colliders)
        {
            if (other != null && other.gameObject.CompareTag("rock") && other.gameObject != gameObject)
            {
                Destroy(other.gameObject);
                gameObject.GetComponent<Stats>().UpdateMaxHealth(rockBreakHpIncrease);
                
                Transform healthBar = gameObject.transform.Find("HealthBar");
                Material healthMaterial = healthBar.GetComponent<Renderer>().material;
                float healthPercentage = Mathf.Clamp01(
                    (float) gameObject.GetComponent<Stats>().health / gameObject.GetComponent<Stats>().maxHealth);
                Debug.Log("New Health: " + healthPercentage);
                healthMaterial.SetFloat("_Health", healthPercentage);
            }
            
            if (other != null && other.gameObject.CompareTag("tree") && other.gameObject != gameObject)
            {
                Destroy(other.gameObject);
                gameObject.GetComponent<Stats>().UpdateAttackCooldown(treeCutCooldownReduction);
                attackCooldown -= treeCutCooldownReduction;
                gameObject.GetComponentInChildren<Sword>().attackCooldown -= treeCutCooldownReduction;
            }
            
            if (other != null && other.gameObject.CompareTag("Player") && other.gameObject != gameObject)
            {
                float distance = Vector2.Distance(transform.position, other.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPlayer = other.gameObject;
                }
            }
        }

        if (closestPlayer != null)
        {
            closestPlayer.GetComponent<Stats>().UpdateHealth(-1 * gameObject.GetComponent<Stats>().damage);
            
            Transform healthBar = closestPlayer.gameObject.transform.Find("HealthBar");
            Material healthMaterial = healthBar.GetComponent<Renderer>().material;
            float healthPercentage = Mathf.Clamp01(
                (float) closestPlayer.GetComponent<Stats>().health / closestPlayer.GetComponent<Stats>().maxHealth);
            Debug.Log("New Health: " + healthPercentage);
            healthMaterial.SetFloat("_Health", healthPercentage);
            
            if (closestPlayer.GetComponent<Stats>().health <= 0)
            {
                Destroy(closestPlayer);
                Debug.Log($"Attacked and destroyed {closestPlayer.name}");
                gameObject.GetComponent<Stats>().UpdateDamage(playerKillDamageIncrease);
            }
            else
            {
                Debug.Log($"Attacked {closestPlayer.name}");
            }
        }
        else
        {
            Debug.Log("No target found within range.");
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.431f, 0f);
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