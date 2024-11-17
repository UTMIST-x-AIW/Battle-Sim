using UnityEngine;

public class SwordSwing : MonoBehaviour
{
    private float attackCooldown = 1f;
    private bool isSwinging = false;
    private float swingTime = 0f;
    private Quaternion initialRotation;
    private Quaternion targetRotation;
    private Transform swordTransform;
    private Transform pivotTransform;
    private float swingSpeedScale = 2f; 

    void Start()
    {
        swordTransform = transform;
        pivotTransform = swordTransform.parent;
        initialRotation = swordTransform.localRotation;
    }

    void Update()
    {
        if ((Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)) && !isSwinging)
        {
            isSwinging = true;
            swingTime = 0f;

            targetRotation = Quaternion.Euler(0, 0, -179);
        }

        if (isSwinging)
        {
            swingTime += Time.deltaTime;

            float swingProgress = swingTime * swingSpeedScale / attackCooldown;
            pivotTransform.localRotation = Quaternion.Slerp(initialRotation, targetRotation, swingProgress);

            if (swingProgress >= 0.5f * swingSpeedScale)
            {
                pivotTransform.localRotation = initialRotation;
            }
            if (swingProgress >= 1f * swingSpeedScale)
            {
                isSwinging = false;
            }
        }
    }
}
