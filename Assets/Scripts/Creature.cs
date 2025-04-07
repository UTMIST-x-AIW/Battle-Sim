using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using NEAT.Genes;

public class Creature : MonoBehaviour
{
    // Add static counter at the top of the class
    private static int totalCreatures = 0;
    private static NEATTest neatTest;  // Cache NEATTest reference
    
    // Make TotalCreatures accessible through a property
    public static int TotalCreatures { get { return totalCreatures; } }
    
    // Method to reset the static reference when scene changes
    public static void ClearStaticReferences()
    {
        neatTest = null;
        totalCreatures = 0;
        Debug.Log("Creature static references cleared");
    }
    
    // maxCreatures is now accessed from NEATTest

    [Header("Basic Stats")]
    public float health = 3f;
    public float energyMeter = 0f;  // Renamed from energy
    public float maxHealth = 3f;
    public float maxEnergy = 1f;
    public float energyRechargeRate = 0.333f; // Fill from 0 to 1 in 3 seconds
    public float reproductionRechargeRate = 0.1f; // Fill from 0 to 1 in 10 seconds
    
    [Header("Aging Settings")]
    public float agingStartTime = 20f;  // Start aging after 20 seconds
    public float agingRate = 0.005f;    // Reduced from 0.01 to 0.005 for slower aging
    private float lifetime = 0f;        // How long the creature has lived
    public float Lifetime { get { return lifetime; } set { lifetime = value; } }  // Public property to access lifetime
    public int generation = 0;          // The generation number of this creature
    
    [Header("Movement Settings")]
    public float moveSpeed = 5f;  // Maximum speed in any direction
    
    [Header("Reproduction Settings")]
    public float weightMutationRate = 0.8f;  // Chance of mutating each connection weight
    public float mutationRange = 0.5f;       // Maximum weight change during mutation
    public float addNodeRate = 0.2f;         // Chance of adding a new node
    public float addConnectionRate = 0.5f;    // Chance of adding a new connection
    public float deleteConnectionRate = 0.2f; // Chance of deleting a connection
    
    [Header("Network Settings")]
    public int maxHiddenLayers = 10;  // Maximum number of hidden layers allowed (set by NEATTest)
    
    [Header("Action Settings")]
    public float actionEnergyCost = 1.0f;
    public float chopDamage = 1.0f;
    public float attackDamage = 1.0f;
    public float visionRange = 10f;  // Range at which creatures can see other entities
    public float chopRange = 1.5f;   // Range at which creatures can chop trees
    
    // Type
    public enum CreatureType { Albert, Kai }
    public CreatureType type;
    
    // Neural Network
    private NEAT.NN.FeedForwardNetwork brain;
    private CreatureObserver observer;
    private Rigidbody2D rb;
    
    // Add method to get brain
    public NEAT.NN.FeedForwardNetwork GetBrain()
    {
        return brain;
    }
    
    // Add at the top with other private fields
    private bool isReproducing = false;  // Flag to prevent multiple reproduction attempts
    private bool isMovingToMate = false;
    private bool isWaitingForMate = false;
    public bool canStartReproducing = false;  // New flag to control reproduction start, now public
    private Creature targetMate = null;
    public float reproductionMeter = 0f; // Renamed from reproductionCooldown, now public

    // Animator reference
    private CreatureAnimator creatureAnimator;

    // Cache floor collider to avoid FindGameObjectWithTag every frame
    private static PolygonCollider2D cachedFloorCollider;
    private static Bounds floorBounds;

    private bool hasCheckedNeatTest = false;

    private void Awake()
    {
        try
        {
            // Cache NEATTest reference if not already cached
            if (neatTest == null)
            {
                neatTest = FindObjectOfType<NEATTest>();
                if (neatTest == null)
                {
                    Debug.LogError("NEATTest component not found in the scene!");
                }
            }

            // Initialize stats
            health = maxHealth;
            reproductionMeter = 0f; // Initialize reproduction meter to 0
            lifetime = 0f;
            canStartReproducing = false;
            
            // Get CreatureAnimator reference
            creatureAnimator = GetComponent<CreatureAnimator>();
            if (creatureAnimator == null)
            {
                creatureAnimator = gameObject.AddComponent<CreatureAnimator>();
            }
            
            // Increment counter when creature is created
            totalCreatures++;
            // Debug.Log(string.Format("Creature created. Total creatures: {0}", totalCreatures));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in Creature Awake: {e.Message}\n{e.StackTrace}");
        }
    }

    private IEnumerator DelayedReproductionStart()
    {
        canStartReproducing = false;
        reproductionMeter = 0f; // Reset reproduction meter to 0
        // Debug.Log(string.Format("{0}: Starting reproduction delay timer", gameObject.name));
        
        // Wait for 2 seconds before allowing reproduction
        yield return new WaitForSeconds(2f);
        
        // Double check we're still alive before enabling reproduction
        if (this != null && gameObject != null)
        {
            canStartReproducing = true;
            // Debug.Log(string.Format("{0} can now start reproducing", gameObject.name));
        }
    }
    
    private void Start()
    {
        observer = gameObject.GetComponent<CreatureObserver>();
        
        // Setup Rigidbody2D
        rb = gameObject.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        
        // Configure Rigidbody2D
        rb.gravityScale = 0f;
        rb.drag = 1f;
        rb.angularDrag = 1f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        
        // Update the animator with the creature type
        if (creatureAnimator != null)
        {
            creatureAnimator.SetCreatureType(type);
        }
    }

    public void InitializeNetwork(NEAT.NN.FeedForwardNetwork network)
    {
        brain = network;
    }
    
    private double[] ConvertToDouble(float[] floatArray)
    {
        double[] doubleArray = new double[floatArray.Length];
        for (int i = 0; i < floatArray.Length; i++)
        {
            doubleArray[i] = (double)floatArray[i];
        }
        return doubleArray;
    }
    
    private float[] ConvertToFloat(double[] doubleArray)
    {
        float[] floatArray = new float[doubleArray.Length];
        for (int i = 0; i < doubleArray.Length; i++)
        {
            floatArray[i] = (float)doubleArray[i];
        }
        return floatArray;
    }
    
    public float[] GetActions()
    {
        if (brain == null)
        {
            // Debug.LogWarning(string.Format("{0}: Brain is null, returning zero movement", gameObject.name));
            return new float[] { 0f, 0f, 0f, 0f };  // 4 outputs: move x, move y, chop, attack
        }
        
        float[] observations = observer.GetObservations(this);
        double[] doubleObservations = ConvertToDouble(observations);
        
        double[] doubleOutputs = brain.Activate(doubleObservations);
        float[] outputs = ConvertToFloat(doubleOutputs);
        
        // Ensure outputs are in range [-1, 1]
        for (int i = 0; i < outputs.Length; i++)
        {
            outputs[i] = Mathf.Clamp(outputs[i], -1f, 1f);
        }
        
        return outputs;
    }

    // Note: The ApplyMutations method has been moved to Reproduction.cs and is no longer used here.
    // All mutations are now handled during reproduction in the Reproduction.cs script.
    // This includes weight mutations, node additions/deletions, and connection mutations.

    private void FixedUpdate()
    {
        try
        {
            // Update lifetime and health
            lifetime += Time.fixedDeltaTime;
            
            // Start aging process after a threshold time
            if (lifetime > agingStartTime)
            {
                // Debug.Log(string.Format("{0}: Aging process started at time {1}", gameObject.name, lifetime));
                health -= agingRate * Time.fixedDeltaTime;
            }
            
            // Recharge energy gradually
            energyMeter = Mathf.Min(maxEnergy, energyMeter + energyRechargeRate * Time.fixedDeltaTime);
            
            // Check if we're dead
            if (health <= 0f)
            {
                // Log the death
                if (LogManager.Instance != null)
                {
                    LogManager.LogMessage($"Creature dying due to health <= 0 - Type: {type}, Health: {health}, Age: {lifetime}, Generation: {generation}");
                }
                else
                {
                    Debug.Log($"Creature dying due to health <= 0 - Type: {type}, Health: {health}, Age: {lifetime}, Generation: {generation}");
                }
                
                // Die
                Destroy(gameObject);
                return;
            }
            
            // Only get actions if we're not reproducing or moving to mate
            if (!isReproducing && !isMovingToMate && !isWaitingForMate)
            {
                try
                {
                    // Fill reproduction meter if not in cooldown
                    if (!canStartReproducing)
                    {
                        reproductionMeter = Mathf.Min(1f, reproductionMeter + reproductionRechargeRate * Time.fixedDeltaTime);
                        
                        // When meter is full, start cooldown before next reproduction attempt
                        if (reproductionMeter >= 1f)
                        {
                            StartCoroutine(DelayedReproductionStart());
                        }
                    }
                    
                    // Get network outputs (x, y velocities, chop desire, attack desire)
                    float[] actions = GetActions();
                    
                    // Process the network's action commands
                    ProcessActionCommands(actions);
                }
                catch (System.Exception e)
                {
                    // Log detailed error information
                    string errorMessage = $"Error in Creature FixedUpdate action processing: {e.Message}\nStack trace: {e.StackTrace}";
                    if (LogManager.Instance != null)
                    {
                        LogManager.LogError(errorMessage);
                    }
                    else
                    {
                        Debug.LogError(errorMessage);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            // Log very detailed error information if the main loop fails
            string errorMessage = $"CRITICAL ERROR in Creature FixedUpdate: {e.Message}\nStack trace: {e.StackTrace}\nCreature info: Type={type}, Generation={generation}, Health={health}, Age={lifetime}";
            if (LogManager.Instance != null)
            {
                LogManager.LogError(errorMessage);
            }
            else
            {
                Debug.LogError(errorMessage);
            }
        }
    }
    
    private void ApplyMovementWithBoundsCheck(Vector2 desiredVelocity)
    {
        // Find and cache floor collider if not already cached
        if (cachedFloorCollider == null)
        {
            GameObject floorObj = GameObject.FindGameObjectWithTag("Floor");
            if (floorObj != null)
            {
                cachedFloorCollider = floorObj.GetComponent<PolygonCollider2D>();
                if (cachedFloorCollider != null)
                {
                    floorBounds = cachedFloorCollider.bounds;
                }
            }
        }
        
        if (cachedFloorCollider == null)
        {
            // No floor found, just apply the movement
            rb.velocity = desiredVelocity;
                return;
            }
        
        // Current position and desired position
        Vector2 currentPos = rb.position;
        Vector2 desiredPos = currentPos + desiredVelocity * Time.fixedDeltaTime;
        
        // Quick bounds check first (much faster than OverlapPoint)
        bool inBounds = floorBounds.Contains(new Vector3(desiredPos.x, desiredPos.y, 0));
        
        // If definitely outside bounds, stop or redirect
        if (!inBounds)
        {
            // Try to redirect toward the center of the floor
            Vector2 centerOfFloor = new Vector2(floorBounds.center.x, floorBounds.center.y);
            Vector2 directionToCenter = (centerOfFloor - currentPos).normalized;
            rb.velocity = directionToCenter * moveSpeed * 0.5f;
            return;
        }
        
        // For positions near the edge, do a more precise check using the polygon collider
        // Only do this check if we're moving significantly
        if (desiredVelocity.sqrMagnitude > 0.1f)
        {
            // Cast a ray in the movement direction to check for boundary
            Vector2 rayDirection = desiredVelocity.normalized;
            float rayDistance = desiredVelocity.magnitude * Time.fixedDeltaTime * 2; // Look a bit ahead
            
            // Use a point slightly inward from the creature's edge
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            float insetDistance = spriteRenderer != null ? spriteRenderer.bounds.extents.x * 0.8f : 0.5f;
            
            // Multiple raycasts from different points on the creature
            bool anyRayHitsEdge = false;
            
            // Center raycast
            RaycastHit2D centerHit = Physics2D.Raycast(currentPos, rayDirection, rayDistance, LayerMask.GetMask("Default"));
            if (centerHit.collider != null && centerHit.collider != cachedFloorCollider)
            {
                anyRayHitsEdge = true;
            }
            
            // Bottom raycast (for ground detection)
            Vector2 bottomPoint = currentPos - new Vector2(0, insetDistance);
            RaycastHit2D bottomHit = Physics2D.Raycast(bottomPoint, rayDirection, rayDistance, LayerMask.GetMask("Default"));
            if (bottomHit.collider != null && bottomHit.collider != cachedFloorCollider)
            {
                anyRayHitsEdge = true;
            }
            
            if (anyRayHitsEdge)
            {
                // We're about to hit an edge - redirect toward the center
                Vector2 centerOfFloor = new Vector2(floorBounds.center.x, floorBounds.center.y);
                Vector2 directionToCenter = (centerOfFloor - currentPos).normalized;
                
                // Blend between current direction and center direction
                Vector2 blendedDirection = Vector2.Lerp(rayDirection, directionToCenter, 0.7f).normalized;
                rb.velocity = blendedDirection * moveSpeed;
                return;
            }
            
            // Final precise check - use OverlapPoint for the exact boundary
            bool pointInFloor = cachedFloorCollider.OverlapPoint(desiredPos);
            if (!pointInFloor)
            {
                // Deflect along the boundary instead of stopping
                Vector2 centerOfFloor = new Vector2(floorBounds.center.x, floorBounds.center.y);
                Vector2 directionToCenter = (centerOfFloor - currentPos).normalized;
                
                // Project desired velocity onto the direction to center to allow sliding along edges
                Vector2 projectedVelocity = Vector2.Dot(desiredVelocity, directionToCenter) * directionToCenter;
                Vector2 tangentialVelocity = desiredVelocity - projectedVelocity;
                
                // Use mostly tangential movement with a bit of inward movement
                rb.velocity = tangentialVelocity * 0.8f + directionToCenter * moveSpeed * 0.3f;
                return;
            }
        }
        
        // All checks passed, apply the original movement
        rb.velocity = desiredVelocity;
    }
    
    private void ProcessActionCommands(float[] actions)
    {
        try
        {
            if (actions == null || actions.Length < 4)
            {
                Debug.LogError($"Invalid actions array: {(actions == null ? "null" : $"length {actions.Length}")}");
                return;
            }
            
            // Apply movement based on neural network output (first two values)
            Vector2 moveDirection = new Vector2(actions[0], actions[1]);
            
            // Normalize to ensure diagonal movement isn't faster
            if (moveDirection.magnitude > 1f)
            {
                moveDirection.Normalize();
            }
            
            // Apply move speed with bounds check
            Vector2 desiredVelocity = moveDirection * moveSpeed;
            ApplyMovementWithBoundsCheck(desiredVelocity);
            
            // Process action commands for chop and attack (third and fourth values)
            float chopDesire = actions[2];
            float attackDesire = actions[3];
            
            // Energy-limited action: Allow action only if we have sufficient energy
            if (energyMeter >= actionEnergyCost)
            {
                // Check for stronger desire between chop and attack
                if (chopDesire > 0.5f && chopDesire >= attackDesire)
                {
                    // Try to chop a tree if strongly desired
                    bool didChop = TryChopTree();
                    
                    // Consume energy if we successfully chopped
                    if (didChop)
                    {
                        energyMeter -= actionEnergyCost;
                    }
                }
                else if (attackDesire > 0.5f)
                {
                    // Try to attack another creature if strongly desired
                    bool didAttack = TryAttackCreature();
                    
                    // Consume energy if we successfully attacked
                    if (didAttack)
                    {
                        energyMeter -= actionEnergyCost;
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            string errorMsg = $"Error in ProcessActionCommands: {e.Message}\nStack trace: {e.StackTrace}";
            if (LogManager.Instance != null)
            {
                LogManager.LogError(errorMsg);
            }
            else
            {
                Debug.LogError(errorMsg);
            }
        }
    }

    private bool TryChopTree()
    {
        // Find the nearest tree within detection radius
        TreeHealth nearestTree = null;
        float nearestDistance = float.MaxValue;

        float ACTION_RADIUS = 1.5f;
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, ACTION_RADIUS);
        
        foreach (var collider in nearbyColliders)
        {
            if (collider.CompareTag("Tree"))
            {
                float distance = Vector2.Distance(transform.position, collider.transform.position);
                if (distance < nearestDistance)
                {
                    TreeHealth treeHealth = collider.GetComponent<TreeHealth>();
                    if (treeHealth != null)
                    {
                        nearestTree = treeHealth;
                        nearestDistance = distance;
                    }
                }
            }
        }
        
        // If we found a tree, damage it
        if (nearestTree != null)
        {
            nearestTree.TakeDamage(chopDamage);
            
            // Restore the creature's health to maximum
            health = maxHealth;
            
            // Visual feedback for health restoration
            StartCoroutine(FlashHealthRestoration());
            
            return true;
        }
        
        return false;
    }

    private IEnumerator FlashHealthRestoration()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            Color originalColor = renderer.color;
            renderer.color = Color.green;
            yield return new WaitForSeconds(0.1f);
            renderer.color = originalColor;
        }
    }

    private bool TryAttackCreature()
    {
        // Find the nearest opposing creature within detection radius
        Creature nearestOpponent = null;
        float nearestDistance = float.MaxValue;
        
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, CreatureObserver.DETECTION_RADIUS);
        
        foreach (var collider in nearbyColliders)
        {
            Creature otherCreature = collider.GetComponent<Creature>();
            if (otherCreature != null && otherCreature.type != this.type)
            {
                float distance = Vector2.Distance(transform.position, collider.transform.position);
                if (distance < nearestDistance)
                {
                    nearestOpponent = otherCreature;
                    nearestDistance = distance;
                }
            }
        }
        
        // If we found an opposing creature, damage it
        if (nearestOpponent != null)
        {
            nearestOpponent.TakeDamage(attackDamage);
            return true;
        }
        
        return false;
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        
        // Visual feedback when taking damage
        StartCoroutine(FlashOnDamage());
    }

    private IEnumerator FlashOnDamage()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            Color originalColor = renderer.color;
            renderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            renderer.color = originalColor;
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if we collided with another creature
        Creature otherCreature = collision.gameObject.GetComponent<Creature>();
        if (otherCreature != null && otherCreature.type != type)
        {
            // Check if healths are approximately equal (within 0.1)
            if (Mathf.Abs(health - otherCreature.health) < 0.1f)
            {
                // Both creatures take half damage
                float damage = health / 2f;  // Use either health value since they're equal
                health = Mathf.Max(0, health - damage);
                otherCreature.health = Mathf.Max(0, otherCreature.health - damage);
                
                // If the damage killed either creature, destroy them
                if (health <= 0)
                {
                    Destroy(gameObject);
                }
                if (otherCreature.health <= 0)
                {
                    Destroy(otherCreature.gameObject);
                }
            }
            // Only handle the collision once (let the creature with higher health handle it)
            else if (health > otherCreature.health)
            {
                // Calculate damage as half of the killed creature's health
                float damage = otherCreature.health / 2f;
                
                // Apply damage to surviving creature
                health = Mathf.Max(0, health - damage);
                
                // Kill the creature with lower health
                Destroy(otherCreature.gameObject);
                
                // If the damage killed us too, destroy ourselves
                if (health <= 0)
                {
                    Destroy(gameObject);
                }
            }
        }
    }

    private void OnDestroy()
    {
        try
        {
            try
            {
                // Try to log, but catch any exceptions if LogManager is gone
                if (LogManager.Instance != null)
                {
                    LogManager.LogMessage($"Creature being destroyed - Type: {type}, Health: {health}, Generation: {generation}");
                }
            }
            catch (System.Exception)
            {
                // If LogManager is already cleaned up, just silently continue
            }
            
            // If we were someone's target mate, free them but handle exceptions
            if (targetMate != null && targetMate.gameObject != null && targetMate.gameObject.activeInHierarchy)
            {
                try
                {
                    // Break the circular reference first
                    var tempMate = targetMate;
                    targetMate = null;
                    
                    // Reset their flags directly instead of using FreeMate which may start coroutines
                    tempMate.isMovingToMate = false;
                    tempMate.isWaitingForMate = false;
                    tempMate.isReproducing = false;
                    tempMate.canStartReproducing = false;
                    tempMate.targetMate = null;
                    
                    // If they're still alive and active, they can start their own timer
                    if (tempMate.gameObject.activeInHierarchy)
                    {
                        try 
                        {
                            tempMate.StartCoroutine(tempMate.DelayedReproductionStart());
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"Error starting reproduction timer for mate: {e.Message}");
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error handling target mate in OnDestroy: {e.Message}");
                }
            }

            // Decrement counter when creature is destroyed
            totalCreatures--;
            
            try
            {
                // Try to log, but catch any exceptions if LogManager is gone
                if (LogManager.Instance != null)
                {
                    LogManager.LogMessage($"Creature destroyed. Total creatures: {totalCreatures}");
                }
            }
            catch (System.Exception)
            {
                // If LogManager is already cleaned up, just silently continue
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in Creature OnDestroy: {e.Message}\nStack trace: {e.StackTrace}");
        }
    }

    private void OnGUI()
    {
        try
        {
            // Safety check - if no camera, exit early
            if (Camera.main == null) return;

            // Only check for NEATTest reference once
            if (!hasCheckedNeatTest)
            {
                if (neatTest == null)
                {
                    neatTest = FindObjectOfType<NEATTest>();
                }
                hasCheckedNeatTest = true;
            }

            // If we still don't have a NEATTest reference or labels are disabled, return early
            if (neatTest == null || !neatTest.showCreatureLabels) return;

            // Get screen position for this creature
            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
            screenPos.y = Screen.height - screenPos.y; // GUI uses top-left origin

            // Only show if on screen
            if (screenPos.x >= 0 && screenPos.x <= Screen.width && 
                screenPos.y >= 0 && screenPos.y <= Screen.height)
            {
                // Set text color to black
                GUI.color = Color.black;
                
                // Format age display - handle large values
                string ageDisplay;
                if (lifetime >= 10000f)
                {
                    ageDisplay = "10,000+";
                }
                else
                {
                    ageDisplay = lifetime.ToString("F1");
                }

                // Show age and generation with increased width for larger numbers
                GUI.Label(new Rect(screenPos.x - 70, screenPos.y - 40, 140, 20), 
                         string.Format("Gen: {0}; Age: {1}", generation, ageDisplay));
                
                // Reset color back to white
                GUI.color = Color.white;
            }
        }
        catch (System.Exception e)
        {
            // Don't log errors in OnGUI as it can spam the console
            // Just silently fail
        }
    }

    private void OnDrawGizmos()
    {
        // Only draw if visualization is enabled and NEATTest reference exists
        if (neatTest != null)
        {
            if (neatTest.showDetectionRadius)
            {
                // Set color to be semi-transparent and match creature type
                Color gizmoColor = (type == CreatureType.Albert) ? new Color(1f, 0.5f, 0f, 0.1f) : new Color(0f, 0.5f, 1f, 0.1f);  // Orange for Albert, Blue for Kai
                Gizmos.color = gizmoColor;
                
                // Draw filled circle for better visibility
                Gizmos.DrawSphere(transform.position, CreatureObserver.DETECTION_RADIUS);
                
                // Draw wire frame with more opacity for better edge definition
                gizmoColor.a = 0.3f;
                Gizmos.color = gizmoColor;
                Gizmos.DrawWireSphere(transform.position, CreatureObserver.DETECTION_RADIUS);
            }
            
            // Draw chop range if enabled
            if (neatTest.showChopRange)
            {
                Gizmos.color = neatTest.chopRangeColor;
                Gizmos.DrawWireSphere(transform.position, chopRange);
            }
        }
    }
    
    private NEAT.NN.FeedForwardNetwork CreateChildNetwork(NEAT.NN.FeedForwardNetwork parent1, NEAT.NN.FeedForwardNetwork parent2)
    {
        // Get parent network details via reflection
        System.Reflection.FieldInfo nodesField = parent1.GetType().GetField("_nodes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        System.Reflection.FieldInfo connectionsField = parent1.GetType().GetField("_connections", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (nodesField == null || connectionsField == null)
        {
            Debug.LogError("Failed to access network fields via reflection");
            return null;
        }
        
        var parent1Nodes = nodesField.GetValue(parent1) as Dictionary<int, NEAT.Genes.NodeGene>;
        var parent1Connections = connectionsField.GetValue(parent1) as Dictionary<int, NEAT.Genes.ConnectionGene>;
        
        var parent2Nodes = nodesField.GetValue(parent2) as Dictionary<int, NEAT.Genes.NodeGene>;
        var parent2Connections = connectionsField.GetValue(parent2) as Dictionary<int, NEAT.Genes.ConnectionGene>;
        
        if (parent1Nodes == null || parent1Connections == null || parent2Nodes == null || parent2Connections == null)
        {
            Debug.LogError("Failed to extract network components");
            return null;
        }
        
        // Create new dictionaries for the child
        var childNodes = new Dictionary<int, NEAT.Genes.NodeGene>();
        var childConnections = new Dictionary<int, NEAT.Genes.ConnectionGene>();
        
        // Add all nodes (taking randomly from either parent for matching nodes)
        var allNodeKeys = new HashSet<int>(parent1Nodes.Keys.Concat(parent2Nodes.Keys));
        foreach (var key in allNodeKeys)
        {
            if (parent1Nodes.ContainsKey(key) && parent2Nodes.ContainsKey(key))
            {
                // Both parents have this node, randomly choose one
                childNodes[key] = Random.value < 0.5f ? 
                    (NEAT.Genes.NodeGene)parent1Nodes[key].Clone() : 
                    (NEAT.Genes.NodeGene)parent2Nodes[key].Clone();
            }
            else if (parent1Nodes.ContainsKey(key))
            {
                // Only parent1 has this node
                childNodes[key] = (NEAT.Genes.NodeGene)parent1Nodes[key].Clone();
            }
            else
            {
                // Only parent2 has this node
                childNodes[key] = (NEAT.Genes.NodeGene)parent2Nodes[key].Clone();
            }
        }
        
        // Add connections (taking randomly from either parent for matching connections)
        var allConnectionKeys = new HashSet<int>(parent1Connections.Keys.Concat(parent2Connections.Keys));
        foreach (var key in allConnectionKeys)
        {
            if (parent1Connections.ContainsKey(key) && parent2Connections.ContainsKey(key))
            {
                // Both parents have this connection, randomly choose one
                childConnections[key] = Random.value < 0.5f ? 
                    (NEAT.Genes.ConnectionGene)parent1Connections[key].Clone() : 
                    (NEAT.Genes.ConnectionGene)parent2Connections[key].Clone();
            }
            else if (parent1Connections.ContainsKey(key))
            {
                // Only parent1 has this connection
                childConnections[key] = (NEAT.Genes.ConnectionGene)parent1Connections[key].Clone();
            }
            else
            {
                // Only parent2 has this connection
                childConnections[key] = (NEAT.Genes.ConnectionGene)parent2Connections[key].Clone();
            }
            
            // Apply mutation to weight (occasionally)
            if (Random.value < 0.8f)
            {
                var conn = childConnections[key];
                conn.Weight += Random.Range(-0.5f, 0.5f);
                conn.Weight = Mathf.Clamp((float)conn.Weight, -1f, 1f);
            }
        }
        
        // Create a new network with the crossover results
        return new NEAT.NN.FeedForwardNetwork(childNodes, childConnections);
    }
}