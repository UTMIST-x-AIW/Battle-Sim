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
        float[] obs = new float[13];  // Now 13 observations (removed unused reproduction)
        
        // Basic stats
        obs[0] = self.health;
        obs[1] = self.energyMeter; // Energy meter
        obs[2] = self.reproductionMeter; // Reproduction meter
        
        // Get nearby objects
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, DETECTION_RADIUS);
        
        Vector2 sameTypePos = Vector2.zero;
        Vector2 oppositeTypePos = Vector2.zero;
        Vector2 cherryPos = Vector2.zero;
        Vector2 treePos = Vector2.zero;

        //  Debug.Log(nearbyColliders.Length);
        
        foreach (var collider in nearbyColliders)
        {
            if (collider.gameObject == gameObject) continue;
            
            Vector2 relativePos = (Vector2)(collider.transform.position - transform.position);

            if (collider.CompareTag("Cherry"))
            {
                if (relativePos.magnitude < cherryPos.magnitude || cherryPos.magnitude == 0)
                {
                    cherryPos = relativePos;
                }
            }
            else if (collider.CompareTag("Tree"))
            {
                if (relativePos.magnitude < treePos.magnitude || treePos.magnitude == 0)
                {
                    treePos = relativePos;
                    
                }
            }
            else
            {
                Creature other = collider.GetComponent<Creature>();
                if (other == null) continue;
                
                if (other.type == self.type)
                {
                    if (relativePos.magnitude < sameTypePos.magnitude || sameTypePos.magnitude == 0)
                    {
                        sameTypePos = relativePos;
                    }
                }
                else
                {
                    if (relativePos.magnitude < oppositeTypePos.magnitude || oppositeTypePos.magnitude == 0)
                    {
                        oppositeTypePos = relativePos;
                    }
                }
            }
        }
        
        // Same type observations (x,y components)
        obs[3] = sameTypePos.x;
        obs[4] = sameTypePos.y;
        
        // Opposite type observations (x,y components)
        obs[5] = oppositeTypePos.x;
        obs[6] = oppositeTypePos.y;
        
        // Cherry observations (x,y components)
        obs[7] = cherryPos.x;
        obs[8] = cherryPos.y;
        
        // Tree observations (x,y components)
        obs[9] = treePos.x;
        obs[10] = treePos.y;
        
        // Add current velocity
        Rigidbody2D rb = self.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            obs[11] = rb.velocity.x;   // Current x velocity
            obs[12] = rb.velocity.y;   // Current y velocity
        }
        else
        {
            obs[11] = 0;
            obs[12] = 0;
        }
        
        return obs;
    }
} 