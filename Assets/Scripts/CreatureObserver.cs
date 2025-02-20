using UnityEngine;

public class CreatureObserver : MonoBehaviour
{
    private const float DETECTION_RADIUS = 5f;
    private static int timestep = 0;
    private bool isMainAlbert;
    
    private void Start()
    {
        // Check if this is the main Albert (at origin)
        isMainAlbert = transform.position == Vector3.zero && 
                      GetComponent<Creature>().type == Creature.CreatureType.Albert;
    }
    
    public float[] GetObservations(Creature self)
    {
        float[] obs = new float[11];  // Now 11 observations (2 components per vector instead of magnitude+angle)
        
        // Basic stats
        obs[0] = self.health;
        obs[1] = self.energy;
        obs[2] = self.reproduction;
        
        // Get nearby creatures
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, DETECTION_RADIUS);
        
        Vector2 sameTypeSum = Vector2.zero;
        float sameTypeAbsSum = 0f;
        Vector2 oppositeTypeSum = Vector2.zero;
        float oppositeTypeAbsSum = 0f;
        
        foreach (var collider in nearbyColliders)
        {
            if (collider.gameObject == gameObject) continue;
            
            Creature other = collider.GetComponent<Creature>();
            if (other == null) continue;
            
            Vector2 relativePos = (Vector2)(other.transform.position - transform.position);
            
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
        
        // Same type observations (x,y components and absolute sum)
        obs[3] = sameTypeSum.x;
        obs[4] = sameTypeSum.y;
        obs[5] = sameTypeAbsSum;
        
        // Opposite type observations (x,y components and absolute sum)
        obs[6] = oppositeTypeSum.x;
        obs[7] = oppositeTypeSum.y;
        obs[8] = oppositeTypeAbsSum;
        
        // Add normalized direction vector (helps with orientation)
        obs[9] = transform.right.x;   // Current x direction
        obs[10] = transform.right.y;  // Current y direction
        
        return obs;
    }
} 