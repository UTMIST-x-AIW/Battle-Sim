using UnityEngine;

public class CreatureObserver : MonoBehaviour
{
    private static int timestep = 0;
    private Collider2D[] nearbyColliders = new Collider2D[20];  // Pre-allocated array for better performance
    
    public float[] GetObservations(Creature self)
    {
        float[] obs = new float[NEATTest.OBSERVATION_COUNT]; 
        
        // Basic stats - normalize health to 0-1 range
        obs[0] = self.health / self.maxHealth; // Normalized health
        obs[1] = self.energyMeter; // Energy meter (already 0-1)
        obs[2] = self.reproductionMeter; // Reproduction meter (already 0-1)
        
        // Get nearby objects using the creature's vision range
        int numColliders = Physics2D.OverlapCircleNonAlloc(transform.position, self.visionRange, nearbyColliders);
        
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
        
        GameObject nearestOppositeCreature = null;
        float nearestOppositeCreatureHealthNormalized = 0f;
        
        // Only process up to numColliders to avoid processing null entries
        for (int i = 0; i < numColliders; i++)
        {
            var collider = nearbyColliders[i];
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
            else if (collider.CompareTag("Creature"))
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
                        nearestOppositeCreature = collider.gameObject;
                        nearestOppositeCreatureHealthNormalized = other.health / other.maxHealth;
                    }
                }
            }
        }
        
        // Transform the observations according to the new formula:
        // 0 when outside FOV, 0 at FOV border, increases linearly to visionRange when hugging creature
        
        // Same type observations (x,y components)
        if (sameTypeDistance <= self.visionRange)
        {
            if (sameTypeDistance > 0)
            {
                // Calculate intensity (0 at border, visionRange when hugging)
                float intensityFactor = 1.0f - sameTypeDistance / self.visionRange;
                
                // Apply intensity factor directly to the relative position vector
                sameTypePos *= intensityFactor;
            }
        }
        else
        {
            sameTypePos = Vector2.zero; // Outside detection radius
        }
        
        // Opposite type observations (x,y components)
        if (oppositeTypeDistance <= self.visionRange)
        {
            if (oppositeTypeDistance > 0)
            {
                // Calculate intensity (0 at border, visionRange when hugging)
                float intensityFactor = 1.0f - oppositeTypeDistance / self.visionRange;
                
                // Apply intensity factor directly to the relative position vector
                oppositeTypePos *= intensityFactor;
            }
        }
        else
        {
            oppositeTypePos = Vector2.zero; // Outside detection radius
        }
        
        // Cherry observations (x,y components)
        if (cherryDistance <= self.visionRange)
        {
            if (cherryDistance > 0)
            {
                // Calculate intensity (0 at border, visionRange when hugging)
                float intensityFactor = 1.0f - cherryDistance / self.visionRange;
                
                // Apply intensity factor directly to the relative position vector
                cherryPos *= intensityFactor;
            }
        }
        else
        {
            cherryPos = Vector2.zero; // Outside detection radius
        }
        
        // Tree observations (x,y components)
        if (treeDistance <= self.visionRange)
        {
            if (treeDistance > 0)
            {
                // Calculate intensity (0 at border, visionRange when hugging)
                float intensityFactor = 1.0f - treeDistance / self.visionRange;
                
                // Apply intensity factor directly to the relative position vector
                treePos *= intensityFactor;
            }
        }
        else
        {
            treePos = Vector2.zero; // Outside detection radius
        }

        // Ground observations (x,y components)
        if (groundDistance <= self.visionRange)
        {
            if (groundDistance > 0)
            {
                // Calculate intensity (0 at border, visionRange when hugging)
                float intensityFactor = 1.0f - groundDistance / self.visionRange;
                
                // Apply intensity factor directly to the relative position vector
                groundPos *= intensityFactor;
            }
        }
        else
        {
            groundPos = Vector2.zero; // Outside detection radius
        }
        
        // Line of sight to nearest opposite type creature
        float inBowRange = 0f;
        if (nearestOppositeCreature != null && oppositeTypeDistance <= self.bowRange) //TODO: make this self.bowRange
        {
            Vector2 directionToOpposite = nearestOppositeCreature.transform.position - transform.position;
            RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, directionToOpposite, oppositeTypeDistance);
            
            // Debug.DrawRay(transform.position, directionToOpposite.normalized * self.visionRange, Color.red, 0.1f);
            
            bool hitOppositeTypeFirst = false;
            bool blockedByObstacle = false;
            
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.gameObject == gameObject) continue; // Skip self
                
                // Check what we hit
                if (hit.collider.CompareTag("Tree") || hit.collider.CompareTag("Ground"))
                {
                    // If we hit an obstacle before the opposite type creature
                    blockedByObstacle = true;
                    break;
                }
                else
                {
                    Creature hitCreature = hit.collider.GetComponent<Creature>();
                    if (hitCreature != null)
                    {
                        if (hitCreature.type != self.type && hit.collider.gameObject == nearestOppositeCreature)
                        {
                            // We hit the opposite type creature before any obstacles
                            hitOppositeTypeFirst = true;
                            break;
                        }
                        // Creatures of same type are ignored and we continue checking
                    }
                }
            }
            
            if (hitOppositeTypeFirst && !blockedByObstacle)
            {
                inBowRange = 1f;
            }
        }

        float inChopRange = 0f;
        if (treeDistance <= self.closeRange)
        {
            inChopRange = 1f;
        }

        float inSwordRange = 0f;
        if (oppositeTypeDistance <= self.closeRange)
        {
            inSwordRange = 1f;
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
        
        obs[13] = inChopRange;
        obs[14] = inSwordRange;
        obs[15] = inBowRange;

        obs[16] = nearestOppositeCreatureHealthNormalized;
        
        return obs;
    }
} 