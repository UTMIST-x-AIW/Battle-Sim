using UnityEngine;

public class CreatureObserver : MonoBehaviour
{
    public static readonly float DETECTION_RADIUS = 4f;
    private static int timestep = 0;
    
    // Cache the ground bounds for edge detection
    private static PolygonCollider2D cachedGroundCollider;
    private static CompositeCollider2D cachedCompositeCollider;
    private static Bounds groundBounds;
    
    private void Start()
    {
        // Get the ground collider if not already cached
        if (cachedGroundCollider == null && cachedCompositeCollider == null)
        {
            GameObject ground = GameObject.FindWithTag("Ground");
            if (ground != null)
            {
                // Try to get PolygonCollider2D first
                cachedGroundCollider = ground.GetComponent<PolygonCollider2D>();
                
                // If not found, try CompositeCollider2D
                if (cachedGroundCollider == null)
                {
                    cachedCompositeCollider = ground.GetComponent<CompositeCollider2D>();
                    if (cachedCompositeCollider != null)
                    {
                        groundBounds = cachedCompositeCollider.bounds;
                        Debug.Log($"Found CompositeCollider2D on Ground with bounds: {groundBounds}");
                    }
                }
                else
                {
                    groundBounds = cachedGroundCollider.bounds;
                    Debug.Log($"Found PolygonCollider2D on Ground with bounds: {groundBounds}");
                }
            }
            else
            {
                Debug.LogError("No GameObject with tag 'Ground' found!");
            }
        }
    }
    
    public float[] GetObservations(Creature self)
    {
        // Expanding to 13 observations to include edge detection
        float[] obs = new float[13];  
        
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
        
        // Edge detection observations (x,y distances to edge)
        // Make sure ground bounds are initialized
        if (cachedGroundCollider == null && cachedCompositeCollider == null)
        {
            GameObject ground = GameObject.FindWithTag("Ground");
            if (ground != null)
            {
                // Try to get PolygonCollider2D first
                cachedGroundCollider = ground.GetComponent<PolygonCollider2D>();
                
                // If not found, try CompositeCollider2D
                if (cachedGroundCollider == null)
                {
                    cachedCompositeCollider = ground.GetComponent<CompositeCollider2D>();
                    if (cachedCompositeCollider != null)
                    {
                        groundBounds = cachedCompositeCollider.bounds;
                        Debug.Log($"Found CompositeCollider2D on Ground with bounds: {groundBounds}");
                    }
                }
                else
                {
                    groundBounds = cachedGroundCollider.bounds;
                    Debug.Log($"Found PolygonCollider2D on Ground with bounds: {groundBounds}");
                }
            }
            else
            {
                Debug.LogError("No GameObject with tag 'Ground' found!");
                // Default to detection radius when ground not found
                obs[11] = DETECTION_RADIUS;
                obs[12] = DETECTION_RADIUS;
                return obs;
            }
        }
        
        // Calculate normalized distances to map edges
        if (cachedGroundCollider != null || cachedCompositeCollider != null)
        {
            Vector2 currentPos = transform.position;
            
            // Calculate distance to boundaries in X direction
            float distanceToRightEdge = groundBounds.max.x - currentPos.x;
            float distanceToLeftEdge = currentPos.x - groundBounds.min.x;
            // Use the closest X edge
            float closestXDistance = Mathf.Min(distanceToRightEdge, distanceToLeftEdge);
            
            // Calculate distance to boundaries in Y direction
            float distanceToTopEdge = groundBounds.max.y - currentPos.y;
            float distanceToBottomEdge = currentPos.y - groundBounds.min.y;
            // Use the closest Y edge
            float closestYDistance = Mathf.Min(distanceToTopEdge, distanceToBottomEdge);
            
            // Cap the distances at DETECTION_RADIUS
            // This way, edges beyond vision radius all look the same
            closestXDistance = Mathf.Min(closestXDistance, DETECTION_RADIUS);
            closestYDistance = Mathf.Min(closestYDistance, DETECTION_RADIUS);
            
            // Assign raw distance values (in world units)
            obs[11] = closestXDistance;
            obs[12] = closestYDistance;
            
            // More frequent logging for debugging edge detection issues
            if (Random.value < 0.01f)
            {
                string debugMsg = $"Edge distances - Creature: {self.gameObject.name}, " +
                    $"Position: {currentPos}, " +
                    $"Raw X: {closestXDistance:F2}, " +
                    $"Raw Y: {closestYDistance:F2}, " +
                    $"Detection radius: {DETECTION_RADIUS:F2}";
                
                Debug.Log(debugMsg);
                
                if (LogManager.Instance != null)
                {
                    LogManager.LogMessage(debugMsg);
                }
            }
        }
        else
        {
            Debug.LogWarning("No valid ground collider found for edge detection!");
            // If ground collider not found, use default values
            obs[11] = DETECTION_RADIUS; // Default to maximum distance (edge not detected)
            obs[12] = DETECTION_RADIUS; // Default to maximum distance (edge not detected)
        }
        
        return obs;
    }
} 