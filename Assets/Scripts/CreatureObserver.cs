using UnityEngine;

public class CreatureObserver : MonoBehaviour
{
    public static readonly float DETECTION_RADIUS = 4f;
    private static int timestep = 0;
    
    private void Start()
    {

    }
    
    public float[] GetObservations(Creature self)
    {
        float[] obs = new float[13];  // Now 13 observations (added ground x,y)
        
        // Basic stats - normalize health to 0-1 range
        obs[0] = self.health / self.maxHealth; // Normalized health
        obs[1] = self.energyMeter; // Energy meter (already 0-1)
        obs[2] = self.reproductionMeter; // Reproduction meter (already 0-1)
        
        // Get nearby objects
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, DETECTION_RADIUS);
        
        Vector2 sameTypePos = Vector2.zero;
        Vector2 oppositeTypePos = Vector2.zero;
        Vector2 cherryPos = Vector2.zero;
        Vector2 treePos = Vector2.zero;
        Vector2 groundPos = Vector2.zero;

        float sameTypeDistance = float.MaxValue;
        float oppositeTypeDistance = float.MaxValue;
        float cherryDistance = float.MaxValue;
        float treeDistance = float.MaxValue;
        float groundDistance = float.MaxValue;
        
        foreach (var collider in nearbyColliders)
        {
            if (collider.gameObject == gameObject) continue;
            
            // Calculate relative position from the object to the creature (reversed direction)
            Vector2 relativePos = (Vector2)(transform.position - collider.transform.position);
            float distance = relativePos.magnitude;

            if (collider.CompareTag("Cherry"))
            {
                if (distance < cherryDistance)
                {
                    cherryPos = relativePos;
                    cherryDistance = distance;
                }
            }
            else if (collider.CompareTag("Tree"))
            {
                if (distance < treeDistance)
                {
                    treePos = relativePos;
                    treeDistance = distance;
                }
            }
            else if (collider.CompareTag("Ground"))
            {
                // For ground tiles, we want the closest point on the collider
                Vector2 closestPoint = collider.ClosestPoint(transform.position);
                Vector2 groundRelativePos = (Vector2)transform.position - closestPoint;
                float groundPointDistance = groundRelativePos.magnitude;
                
                if (groundPointDistance < groundDistance)
                {
                    groundPos = groundRelativePos;
                    groundDistance = groundPointDistance;
                }
            }
            else
            {
                Creature other = collider.GetComponent<Creature>();
                if (other == null) continue;
                
                if (other.type == self.type)
                {
                    if (distance < sameTypeDistance)
                    {
                        sameTypePos = relativePos;
                        sameTypeDistance = distance;
                    }
                }
                else
                {
                    if (distance < oppositeTypeDistance)
                    {
                        oppositeTypePos = relativePos;
                        oppositeTypeDistance = distance;
                    }
                }
            }
        }
        
        // Transform the observations according to the new formula:
        // 0 when outside FOV, 0 at FOV border, increases linearly to DETECTION_RADIUS when hugging creature
        
        // Same type observations (x,y components)
        if (sameTypeDistance <= DETECTION_RADIUS)
        {
            if (sameTypeDistance > 0)
            {
                // Calculate intensity (0 at border, DETECTION_RADIUS when hugging)
                float intensityFactor = 1.0f - sameTypeDistance / DETECTION_RADIUS;
                
                // Apply intensity factor directly to the relative position vector
                sameTypePos *= intensityFactor;
            }
        }
        else
        {
            sameTypePos = Vector2.zero; // Outside detection radius
        }
        
        // Opposite type observations (x,y components)
        if (oppositeTypeDistance <= DETECTION_RADIUS)
        {
            if (oppositeTypeDistance > 0)
            {
                // Calculate intensity (0 at border, DETECTION_RADIUS when hugging)
                float intensityFactor = 1.0f - oppositeTypeDistance / DETECTION_RADIUS;
                
                // Apply intensity factor directly to the relative position vector
                oppositeTypePos *= intensityFactor;
            }
        }
        else
        {
            oppositeTypePos = Vector2.zero; // Outside detection radius
        }
        
        // Cherry observations (x,y components)
        if (cherryDistance <= DETECTION_RADIUS)
        {
            if (cherryDistance > 0)
            {
                // Calculate intensity (0 at border, DETECTION_RADIUS when hugging)
                float intensityFactor = 1.0f - cherryDistance / DETECTION_RADIUS;
                
                // Apply intensity factor directly to the relative position vector
                cherryPos *= intensityFactor;
            }
        }
        else
        {
            cherryPos = Vector2.zero; // Outside detection radius
        }
        
        // Tree observations (x,y components)
        if (treeDistance <= DETECTION_RADIUS)
        {
            if (treeDistance > 0)
            {
                // Calculate intensity (0 at border, DETECTION_RADIUS when hugging)
                float intensityFactor = 1.0f - treeDistance / DETECTION_RADIUS;
                
                // Apply intensity factor directly to the relative position vector
                treePos *= intensityFactor;
            }
        }
        else
        {
            treePos = Vector2.zero; // Outside detection radius
        }

        // Ground observations (x,y components)
        if (groundDistance <= DETECTION_RADIUS)
        {
            if (groundDistance > 0)
            {
                // Calculate intensity (0 at border, DETECTION_RADIUS when hugging)
                float intensityFactor = 1.0f - groundDistance / DETECTION_RADIUS;
                
                // Apply intensity factor directly to the relative position vector
                groundPos *= intensityFactor;
            }
        }
        else
        {
            groundPos = Vector2.zero; // Outside detection radius
        }
        
        // Assign the transformed values to the observation array
        obs[3] = sameTypePos.x;
        obs[4] = sameTypePos.y;
        
        obs[5] = oppositeTypePos.x;
        obs[6] = oppositeTypePos.y;
        
        obs[7] = cherryPos.x;
        obs[8] = cherryPos.y;
        
        obs[9] = treePos.x;
        obs[10] = treePos.y;

        obs[11] = groundPos.x;
        obs[12] = groundPos.y;
        
        return obs;
    }
} 