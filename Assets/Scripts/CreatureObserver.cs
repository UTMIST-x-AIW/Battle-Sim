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
        float[] obs = new float[13];  // Now 13 observations (removed energy)
        
        // Basic stats
        obs[0] = self.health;
        obs[1] = self.reproduction;
        
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
        obs[2] = sameTypeSum.x;
        obs[3] = sameTypeSum.y;
        obs[4] = sameTypeAbsSum;
        
        // Opposite type observations (x,y components and absolute sum)
        obs[5] = oppositeTypeSum.x;
        obs[6] = oppositeTypeSum.y;
        obs[7] = oppositeTypeAbsSum;
        
        // Cherry observations (x,y components and absolute sum)
        obs[8] = cherrySum.x;
        obs[9] = cherrySum.y;
        obs[10] = cherryAbsSum;
        
        // Add normalized direction vector (helps with orientation)
        obs[11] = transform.right.x;   // Current x direction
        obs[12] = transform.right.y;   // Current y direction
        
        return obs;
    }
} 