using UnityEngine;

public class CreatureObserver : MonoBehaviour
{
    private const float DETECTION_RADIUS = 5f;
    
    public float[] GetObservations(Creature self)
    {
        float[] obs = new float[9];
        
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
        
        // Same type observations
        obs[3] = sameTypeSum.magnitude;
        obs[4] = Mathf.Atan2(sameTypeSum.y, sameTypeSum.x);
        if (obs[4] < 0) obs[4] += 2 * Mathf.PI;
        obs[5] = sameTypeAbsSum;
        
        // Opposite type observations
        obs[6] = oppositeTypeSum.magnitude;
        obs[7] = Mathf.Atan2(oppositeTypeSum.y, oppositeTypeSum.x);
        if (obs[7] < 0) obs[7] += 2 * Mathf.PI;
        obs[8] = oppositeTypeAbsSum;
        
        return obs;
    }
} 