using UnityEngine;

public class Sword : MonoBehaviour
{
    [SerializeField] public float attackCooldown = 3f;
    private float attackTimer = 0f; // Timer to track cooldown
    private bool isSwinging = false;
    private float swingTime = 0f;
    private Quaternion initialRotation;
    private Quaternion targetRotation;
    private Transform swordTransform;
    [SerializeField] public float swingSpeedScale = 20f;

    void Start()
    {
        swordTransform = transform; // Get the sword's transform
        initialRotation = swordTransform.localRotation; // Store the local rotation
    }

    void Update()
    {
        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime * 2;
        }
        
        if ((Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift) || Input.GetKeyDown(KeyCode.Space)) && !isSwinging && attackTimer <= 0f)
        {
            gameObject.GetComponentInParent<PlayerAttack>().Attack();
            attackTimer = attackCooldown;
            isSwinging = true;
            swingTime = 0f;
            targetRotation = Quaternion.Euler(0, 0, -179); // Adjust swing rotation angle
        }

        if (isSwinging)
        {
            swingTime += Time.deltaTime;

            float swingProgress = swingTime * swingSpeedScale / attackCooldown;
            swordTransform.localRotation = Quaternion.Slerp(initialRotation, targetRotation, swingProgress); // Update only the sword's rotation

            if (swingProgress >= 1f || attackTimer <= 0f)
            {
                swordTransform.localRotation = initialRotation; // Reset to original position
                isSwinging = false;
            }
        }
    }
}