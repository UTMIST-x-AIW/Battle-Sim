using UnityEngine;

public class MultiRayShooter : MonoBehaviour
{
    public int rayCount = 5; // Number of rays to shoot
    public float raySpreadAngle = 30f; // Total spread angle (degrees)
    public float rayDistance = 10f; // Length of each ray
    public LayerMask rayMask; // Layers the ray should interact with

    private DebugMovement characterMovement;

    void Start()
    {
        // Get the character's movement script
        characterMovement = GetComponent<DebugMovement>();
    }

    void Update()
    {
        // Shoot rays when the character is moving
        if (characterMovement != null && characterMovement.movementdir != Vector3.zero)
        {
            ShootRays(characterMovement.movementdir);
        }
    }

    void ShootRays(Vector3 direction)
    {
        // Calculate the starting angle
        float halfSpread = raySpreadAngle / 2f;

        // Loop to shoot multiple rays
        for (int i = 0; i < rayCount; i++)
        {
            // Calculate the angle for this ray
            float angle = -halfSpread + (i * (raySpreadAngle / (rayCount - 1)));
            Quaternion rotation = Quaternion.Euler(0, angle, 0);

            // Rotate the direction by the calculated angle
            Vector3 rayDirection = rotation * direction;

            // Perform the raycast
            Ray ray = new Ray(transform.position, rayDirection);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, rayDistance, rayMask))
            {
                Debug.DrawLine(transform.position, hit.point, Color.red); // Draw hit ray
                Debug.Log($"Hit: {hit.collider.name}");
            }
            else
            {
                Debug.DrawLine(transform.position, transform.position+rayDirection, Color.red); // Draw miss ray
            }
        }
    }
}