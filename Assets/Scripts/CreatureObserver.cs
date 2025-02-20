using UnityEngine;

public class CreatureObserver : MonoBehaviour
{
    private const float DETECTION_RADIUS = 5f;
    private static int timestep = 0;
    
    private void Start()
    {

    }
    
    public float[] GetObservations(Creature self)
    {
        float[] obs = new float[14];  // Now 14 observations (added 3 for cherries)
        
        // Basic stats
        obs[0] = self.health;
        obs[1] = self.energy;
        obs[2] = self.reproduction;
        
        // Get nearby objects
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, DETECTION_RADIUS);
        
        Vector2 sameTypeSum = Vector2.zero;
        float sameTypeAbsSum = 0f;
        Vector2 oppositeTypeSum = Vector2.zero;
        float oppositeTypeAbsSum = 0f;
        Vector2 cherrySum = Vector2.zero;
        float cherryAbsSum = 0f;
        
        foreach (var collider in nearbyColliders)
        {
            if (collider.gameObject == gameObject) continue;
            
            Vector2 relativePos = (Vector2)(collider.transform.position - transform.position);
            
            if (collider.CompareTag("Cherry"))
            {
                cherrySum += relativePos;
                cherryAbsSum += relativePos.magnitude;
            }
            else
            {
                Creature other = collider.GetComponent<Creature>();
                if (other == null) continue;
                
                if (other.type == self.type)
                {
                    sameTypeSum += relativePos;
                    sameTypeAbsSum += relativePos.magnitude;
                }
                else
                {
                    oppositeTypeSum += relativePos;
                    oppositeTypeAbsSum += relativePos.magnitude;
                }
            }
        }
        
        // Same type observations (x,y components and absolute sum)
        obs[3] = sameTypeSum.x;
        obs[4] = sameTypeSum.y;
        obs[5] = sameTypeAbsSum;
        
        // Opposite type observations (x,y components and absolute sum)
        obs[6] = oppositeTypeSum.x;
        obs[7] = oppositeTypeSum.y;
        obs[8] = oppositeTypeAbsSum;
        
        // Cherry observations (x,y components and absolute sum)
        obs[9] = cherrySum.x;
        obs[10] = cherrySum.y;
        obs[11] = cherryAbsSum;
        
        // Add normalized direction vector (helps with orientation)
        obs[12] = transform.right.x;   // Current x direction
        obs[13] = transform.right.y;   // Current y direction
        
        return obs;
    }
} 