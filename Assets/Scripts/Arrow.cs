using System.Collections;
using UnityEngine;

public class Arrow : MonoBehaviour
{
    [SerializeField] private float attackCooldown = 4.5f;
    [SerializeField] private float arrowSpeed = 5f;
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private int maxColliders = 3;
    [SerializeField] private int rockBreakHpIncrease = 5;
    [SerializeField] private float treeCutCooldownReduction = 0.5f;
    [SerializeField] private int playerKillDamageIncrease = 25;
    public bool isArcher = false;

    private float attackTimer = 0f;
    private Transform arrowTransform;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private bool isMoving = false;
    private Collider2D[] colliders;

    [SerializeField] GameObject parentGameObject;

    void Start()
    {
        arrowTransform = arrowPrefab.transform;
        initialPosition = arrowTransform.localPosition;
        initialRotation = arrowTransform.localRotation;
        colliders = new Collider2D[maxColliders];
    }

    void Update()
    {
        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
        }

        if ((Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift) || Input.GetKeyDown(KeyCode.Space)) && !isMoving && attackTimer <= 0f)
        {
            StartCoroutine(ArrowAttack());
            attackTimer = attackCooldown;
        }
    }

    private IEnumerator ArrowAttack()
    {
        if (isArcher)
        {
            isMoving = true;

            // Detect objects within attack range
            GameObject closestPlayer = null;
            float closestDistance = attackRange;
            int numColliders = Physics2D.OverlapCircleNonAlloc(transform.position, attackRange, colliders);

            for (int i = 0; i < numColliders; i++)
            {
                Collider2D other = colliders[i];

                if (other != null)
                {
                    if (other.CompareTag("rock") && other.gameObject != parentGameObject)
                    {
                        Destroy(other.gameObject);
                        parentGameObject.GetComponent<Stats>().UpdateMaxHealth(rockBreakHpIncrease);
                        UpdateHealthBar();
                    }
                    else if (other.CompareTag("tree") && other.gameObject != parentGameObject)
                    {
                        Destroy(other.gameObject);
                        parentGameObject.GetComponent<Stats>().UpdateAttackCooldown(treeCutCooldownReduction);
                        attackCooldown -= treeCutCooldownReduction;
                    }
                    else if (other.CompareTag("Player") && other.gameObject != parentGameObject)
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

            if (closestPlayer == null)
            {
                isMoving = false;
                Debug.Log("No target found within range.");
                yield break;
            }

            // Rotate and move towards target
            Vector3 direction = (closestPlayer.transform.position - arrowTransform.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            arrowTransform.rotation = Quaternion.Euler(0, 0, angle-90);

            while (Vector3.Distance(arrowTransform.position, closestPlayer.transform.position) > 0.1f)
            {
                arrowTransform.position = Vector3.MoveTowards(arrowTransform.position, closestPlayer.transform.position, arrowSpeed * Time.deltaTime);
                yield return null;
            }

            // Deal damage to the closest player
            Stats targetStats = closestPlayer.GetComponent<Stats>();
            targetStats.UpdateHealth(-1 * parentGameObject.GetComponent<Stats>().damage);
            UpdateHealthBar(closestPlayer);

            if (targetStats.health <= 0)
            {
                Destroy(closestPlayer);
                Debug.Log($"Attacked and destroyed {closestPlayer.name}");
                parentGameObject.GetComponent<Stats>().UpdateDamage(playerKillDamageIncrease);
            }
            else
            {
                Debug.Log($"Attacked {closestPlayer.name}");
            }

            // Vanish and reset arrow
            arrowTransform.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.5f);
            arrowTransform.localPosition = initialPosition;
            arrowTransform.localRotation = initialRotation;
            arrowTransform.gameObject.SetActive(true);

            isMoving = false;
        }
    }

    private void UpdateHealthBar(GameObject target = null)
    {
        if (target == null)
        {
            target = gameObject;
        }

        Transform healthBar = target.transform.Find("HealthBar");
        if (healthBar)
        {
            Material healthMaterial = healthBar.GetComponent<Renderer>().material;
            float healthPercentage = Mathf.Clamp01((float)target.GetComponent<Stats>().health / target.GetComponent<Stats>().maxHealth);
            healthMaterial.SetFloat("_Health", healthPercentage);
        }
    }
}
