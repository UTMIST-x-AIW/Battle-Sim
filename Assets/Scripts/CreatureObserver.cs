using UnityEngine;

public class CreatureObserver : MonoBehaviour
{
    private const float DETECTION_RADIUS = 5f;
    private bool hasLoggedOnce = false;
    
    public float[] GetObservations(Creature self)
    {
        float[] obs = new float[9];
        bool shouldLog = !hasLoggedOnce && self.gameObject == transform.parent.gameObject;
        
        // Basic stats
        obs[0] = self.health;
        obs[1] = self.energy;
        obs[2] = self.reproduction;
        
        if (shouldLog)
            Debug.Log(string.Format("Basic Stats - Health: {0:F2}, Energy: {1:F2}, Reproduction: {2:F2}", 
                obs[0], obs[1], obs[2]));
        
        // Get nearby creatures
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, DETECTION_RADIUS);
        if (shouldLog)
            Debug.Log(string.Format("Found {0} colliders within {1} unit radius", nearbyColliders.Length, DETECTION_RADIUS));
        
        Vector2 sameTypeSum = Vector2.zero;
        float sameTypeAbsSum = 0f;
        Vector2 oppositeTypeSum = Vector2.zero;
        float oppositeTypeAbsSum = 0f;
        
        foreach (var collider in nearbyColliders)
        {
            if (collider.gameObject == gameObject) 
            {
                if (shouldLog) Debug.Log("Skipping self in collider check");
                continue;
            }
            
            Creature other = collider.GetComponent<Creature>();
            if (other == null)
            {
                if (shouldLog) Debug.Log("Found collider without Creature component");
                continue;
            }
            
            Vector2 relativePos = (Vector2)(other.transform.position - transform.position);
            if (shouldLog)
                Debug.Log(string.Format("Found creature of type {0} at relative position ({1:F2}, {2:F2}), distance: {3:F2}", 
                    other.type, relativePos.x, relativePos.y, relativePos.magnitude));
            
            if (other.type == self.type)
            {
                sameTypeSum += relativePos;
                sameTypeAbsSum += relativePos.magnitude;
                if (shouldLog)
                    Debug.Log(string.Format("Same type - Updated sum to ({0:F2}, {1:F2}), abs sum to {2:F2}", 
                        sameTypeSum.x, sameTypeSum.y, sameTypeAbsSum));
            }
            else
            {
                oppositeTypeSum += relativePos;
                oppositeTypeAbsSum += relativePos.magnitude;
                if (shouldLog)
                    Debug.Log(string.Format("Opposite type - Updated sum to ({0:F2}, {1:F2}), abs sum to {2:F2}", 
                        oppositeTypeSum.x, oppositeTypeSum.y, oppositeTypeAbsSum));
            }
        }
        
        // Same type observations
        obs[3] = sameTypeSum.magnitude;
        obs[4] = Mathf.Atan2(sameTypeSum.y, sameTypeSum.x);
        if (obs[4] < 0) obs[4] += 2 * Mathf.PI;
        obs[5] = sameTypeAbsSum;
        
        if (shouldLog)
            Debug.Log(string.Format("Same Type Final - Vector Magnitude: {0:F2}, Direction: {1:F2} rad, Absolute Sum: {2:F2}", 
                obs[3], obs[4], obs[5]));
        
        // Opposite type observations
        obs[6] = oppositeTypeSum.magnitude;
        obs[7] = Mathf.Atan2(oppositeTypeSum.y, oppositeTypeSum.x);
        if (obs[7] < 0) obs[7] += 2 * Mathf.PI;
        obs[8] = oppositeTypeAbsSum;
        
        if (shouldLog)
        {
            Debug.Log(string.Format("Opposite Type Final - Vector Magnitude: {0:F2}, Direction: {1:F2} rad, Absolute Sum: {2:F2}", 
                obs[6], obs[7], obs[8]));
            hasLoggedOnce = true;
        }
        
        return obs;
    }
} 