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
    
    // maxCreatures is now accessed from NEATTest
    
    [Header("Basic Stats")]
    public float health = 3f;
    public float reproduction = 0f;
    public float energy = 0f;
    public float maxHealth = 3f;
    public float maxReproduction = 1f;
    public float maxEnergy = 1f;
    public float energyRechargeRate = 0.333f; // Fill from 0 to 1 in 3 seconds
    
    [Header("Aging Settings")]
    public float agingStartTime = 10f;  // Time in seconds before aging starts
    public float agingRate = 0.03f;      // Base rate of aging damage (increased from 0.01 to compensate for linear aging)
    private float lifetime = 0f;        // How long the creature has lived
    
    [Header("Movement Settings")]
    public float moveSpeed = 5f;  // Maximum speed in any direction
    
    [Header("Reproduction Settings")]
    public float reproductionRate = 0.1f;  // Points gained per second
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
    private bool canStartReproducing = false;  // New flag to control reproduction start
    private Creature targetMate = null;

    // Add these private variables at the class level
    private bool hasLoggedObservations = false;
    private int debugFrameCounter = 0;

    // Animator reference
    private CreatureAnimator creatureAnimator;

    // Cache floor collider to avoid FindGameObjectWithTag every frame
    private static PolygonCollider2D cachedFloorCollider;
    private static Bounds floorBounds;

    private void Awake()
    {
        // Cache NEATTest reference if not already cached
        if (neatTest == null)
        {
            neatTest = FindObjectOfType<NEATTest>();
        }

        // Initialize stats
        health = maxHealth;
        reproduction = 0f;
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

    private IEnumerator DelayedReproductionStart()
    {
        canStartReproducing = false;
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

    // Add method to initialize creature for testing
    public void InitializeForTesting(float startingAge, float startingReproduction)
    {
        lifetime = startingAge;
        if (startingReproduction > 0)
        {
            reproduction = startingReproduction;
            StartCoroutine(DelayedReproductionStart());
        }
    }
    
    private void Start()
    {
        observer = gameObject.AddComponent<CreatureObserver>();
        
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
        rb.constraints = RigidbodyConstraints2D.None;
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
            return new float[] { 0f, 0f, 0f, 0f };  // Update to include chop and attack actions
        }
        
        float[] observations = observer.GetObservations(this);
        double[] doubleObservations = ConvertToDouble(observations);
        
        // Debug the observations once
        //if (!hasLoggedObservations)
        //{
        //    string obsStr = "Observations: ";
        //    for (int i = 0; i < observations.Length; i++)
        //    {
        //        obsStr += string.Format("[{0}]={1}, ", i, observations[i]);
        //    }
        //    Debug.Log(string.Format("{0}: {1}", gameObject.name, obsStr));
        //    hasLoggedObservations = true;
        //}
        
        double[] doubleOutputs = brain.Activate(doubleObservations);
        float[] outputs = ConvertToFloat(doubleOutputs);
        
        // Debug the outputs periodically
        //debugFrameCounter++;
        //if (debugFrameCounter >= 100)
        //{
        //    Debug.Log(string.Format("{0}: Network output x={1}, y={2}, chop={3}, attack={4}", 
        //        gameObject.name, outputs[0], outputs[1], outputs[2], outputs[3]));
        //    debugFrameCounter = 0;
        //}
        
        // Ensure outputs are in range [-1, 1]
        for (int i = 0; i < outputs.Length; i++)
        {
            outputs[i] = Mathf.Clamp(outputs[i], -1f, 1f);
        }
        
        return outputs;
    }
    
    private void UpdateReproduction()
    {
        // Only accumulate reproduction points and try to reproduce if allowed
        if (!isReproducing && canStartReproducing)
        {
            // If we somehow got stuck with isReproducing false but other flags true, reset
            if (isMovingToMate || isWaitingForMate)
            {
                // Debug.LogWarning(string.Format("{0}: Found inconsistent reproduction state, resetting...", gameObject.name));
                FreeMate();
                return;
            }

            // Only accumulate if we're not at max
            if (reproduction < maxReproduction)
            {
                reproduction += reproductionRate * Time.fixedDeltaTime;
                reproduction = Mathf.Min(reproduction, maxReproduction);
            }
            
            // Check if ready to reproduce
            if (reproduction >= maxReproduction && !isReproducing)
            {
                StartCoroutine(TryReproduce());
            }
        }
        else if (isReproducing && !isMovingToMate && !isWaitingForMate && targetMate == null)
        {
            // We're in an inconsistent state - isReproducing is true but we're not actually in any reproduction process
            // Debug.LogWarning(string.Format("{0}: Found stuck reproduction state, resetting...", gameObject.name));
            FreeMate();
        }
    }
    
    private List<Creature> FindPotentialMates()
    {
        List<Creature> potentialMates = new List<Creature>();
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, CreatureObserver.DETECTION_RADIUS);
        
        foreach (var collider in nearbyColliders)
        {
            if (collider.gameObject == gameObject) continue;
            
            Creature otherCreature = collider.GetComponent<Creature>();
            if (otherCreature != null && 
                otherCreature.type == type && 
                otherCreature.reproduction >= maxReproduction &&
                !otherCreature.isReproducing &&
                otherCreature.canStartReproducing)
            {
                potentialMates.Add(otherCreature);
            }
        }
        
        // Sort by lifetime (oldest first)
        potentialMates.Sort((a, b) => b.lifetime.CompareTo(a.lifetime));
        return potentialMates;
    }

    private IEnumerator TryReproduce()
    {
        // Set flag to prevent multiple reproduction attempts
        isReproducing = true;
        
        // Make sure NEATTest reference exists
        if (neatTest == null)
        {
            neatTest = FindObjectOfType<NEATTest>();
            
            // If still null, use a default value and cancel reproduction
            if (neatTest == null)
            {
                Debug.LogWarning("Could not find NEATTest component - using default maxCreatures value of 20");
                if (totalCreatures >= 20)
                {
                    FreeMate();
                    yield break;
                }
            }
        }
        
        // Check population limit first
        if (neatTest != null && totalCreatures >= neatTest.maxCreatures)
        {
            Debug.Log($"Max creatures ({neatTest.maxCreatures}) reached, preventing reproduction");
            FreeMate();
            yield break;
        }

        if (brain == null)
        {
            FreeMate();
            yield break;
        }

        // Find potential mates
        var potentialMates = FindPotentialMates();
        
        // If no potential mates found, reset reproduction and exit
        if (potentialMates.Count == 0)
        {
            // Debug.Log(string.Format("{0}: No potential mates found, keeping reproduction ready", gameObject.name));
            isReproducing = false;
            reproduction = maxReproduction; // Keep ready to reproduce
            yield break;
        }

        // Get the oldest mate
        Creature mate = potentialMates[0];
        
        // First check that the potential mate is still valid and ready
        if (mate == null || !mate.gameObject.activeInHierarchy || mate.isReproducing)
        {
            // Cancel reproduction if mate isn't valid anymore
            isReproducing = false;
            reproduction = maxReproduction; // Keep ready to reproduce
            yield break;
        }
        
        // Decide who moves to whom
        bool shouldMove = true;
        if (lifetime == mate.lifetime)
        {
            // If same age, randomly decide
            shouldMove = Random.value < 0.5f;
        }
        else if (lifetime > mate.lifetime)
        {
            // If we're older, we wait
            shouldMove = false;
        }

        // Lock the mate's reproduction state immediately to prevent race conditions
        mate.isReproducing = true;
        
        if (shouldMove)
        {
            // We're the younger one, move to mate
            isMovingToMate = true;
            targetMate = mate;
            mate.isWaitingForMate = true;
            mate.targetMate = this;
            // Debug.Log($"{gameObject.name} (younger) moving to mate with {mate.gameObject.name}. My position: {transform.position}, Target: {mate.transform.position}");
        }
        else
        {
            // We're the older one, wait for mate
            isWaitingForMate = true;
            targetMate = mate;
            mate.isMovingToMate = true;
            mate.targetMate = this;
            // Debug.Log($"{gameObject.name} (older) waiting for mate {mate.gameObject.name}. My position: {transform.position}, Partner starting at: {mate.transform.position}");
        }
    }

    private void FreeMate()
    {
        // Debug.Log(string.Format("{0}: FreeMate called. Previous state - isReproducing: {1}, isMovingToMate: {2}, isWaitingForMate: {3}, reproduction: {4}", gameObject.name, isReproducing, isMovingToMate, isWaitingForMate, reproduction));
        
        // Reset ALL reproduction-related flags
        isMovingToMate = false;
        isWaitingForMate = false;
        isReproducing = false;
        reproduction = 0f;
        canStartReproducing = false;  // Ensure this is false before starting the delay
        
        // If we have a target mate, log and clear it
        if (targetMate != null)
        {
            // Debug.Log(string.Format("{0} freeing mate {1}", gameObject.name, targetMate.gameObject.name));
            
            // Store reference before nulling it
            var mate = targetMate;
            targetMate = null;
            
            // Also reset the target mate's flags (in case they haven't been reset)
            if (mate != null && mate.gameObject != null && mate.gameObject.activeInHierarchy)
            {
                mate.isMovingToMate = false;
                mate.isWaitingForMate = false;
                mate.isReproducing = false;
                mate.reproduction = 0f;
                mate.canStartReproducing = false;  // Ensure this is false before starting the delay
                mate.targetMate = null;
                
                // Ensure the mate also starts their reproduction timer
                try
                {
                    mate.StopAllCoroutines();  // Stop any existing timers
                    mate.StartCoroutine(mate.DelayedReproductionStart());
                }
                catch (System.Exception) 
                {
                    // Ignore any exceptions if coroutines can't be started
                }
            }
        }
        else
        {
            targetMate = null;
        }
        
        // Only try to manage coroutines if the gameObject is active
        if (gameObject.activeInHierarchy)
        {
            try
            {
                // Stop any existing DelayedReproductionStart coroutines
                StopAllCoroutines();
                
                // Start delayed reproduction timer again
                StartCoroutine(DelayedReproductionStart());
            }
            catch (System.Exception)
            {
                // Ignore any exceptions if coroutines can't be started
            }
        }
        
        // Debug.Log(string.Format("{0}: FreeMate completed. New state - canStartReproducing: false, reproduction: 0", gameObject.name));
    }

    private void ApplyMutations(NEAT.Genome.Genome genome)
    {
        // 1. Weight mutations (configurable chance for each connection)
        foreach (var conn in genome.Connections.Values)
        {
            if (Random.value < weightMutationRate)
            {
                if (Random.value < 0.9f)
                {
                    // Perturb weight
                    conn.Weight += Random.Range(-mutationRange, mutationRange);
                    conn.Weight = Mathf.Clamp((float)conn.Weight, -1f, 1f);
                }
                else
                {
                    // Assign new random weight
                    conn.Weight = Random.Range(-1f, 1f);
                }
            }
        }

        // 2. Add node mutation (configurable chance)
        if (Random.value < addNodeRate && genome.Connections.Count > 0)
        {
            // Check if we've reached the maximum number of hidden layers
            int maxCurrentLayer = 0;
            foreach (var node in genome.Nodes.Values)
            {
                if (node.Type == NEAT.Genes.NodeType.Hidden)
                {
                    maxCurrentLayer = Mathf.Max(maxCurrentLayer, node.Layer);
                }
            }
            
            // Only proceed if we haven't reached the max hidden layers
            if (maxCurrentLayer < maxHiddenLayers + 1) // +1 because layer 0 is input
            {
                // Original connection-splitting logic
                var connList = new List<NEAT.Genes.ConnectionGene>(genome.Connections.Values);
                var connToSplit = connList[Random.Range(0, connList.Count)];
                connToSplit.Enabled = false;

                // Create new node
                int newNodeKey = genome.Nodes.Count;
                var newNode = new NEAT.Genes.NodeGene(newNodeKey, NEAT.Genes.NodeType.Hidden);
                
                // Set layer between input and output nodes
                var inputNode = genome.Nodes[connToSplit.InputKey];
                var outputNode = genome.Nodes[connToSplit.OutputKey];
                newNode.Layer = (inputNode.Layer + outputNode.Layer) / 2;
                
                // If the layer would be the same as the input layer, increment it
                if (newNode.Layer <= inputNode.Layer)
                {
                    // Check if incrementing would exceed max layers
                    if (inputNode.Layer + 1 >= maxHiddenLayers + 1)
                    {
                        // Skip this mutation if it would exceed max layers
                        return;
                    }
                    newNode.Layer = inputNode.Layer + 1;
                }
                
                genome.AddNode(newNode);

                // Add two new connections
                var conn1 = new NEAT.Genes.ConnectionGene(
                    genome.Connections.Count,
                    connToSplit.InputKey,
                    newNodeKey,
                    1.0);

                var conn2 = new NEAT.Genes.ConnectionGene(
                    genome.Connections.Count + 1,
                    newNodeKey,
                    connToSplit.OutputKey,
                    connToSplit.Weight);

                genome.AddConnection(conn1);
                genome.AddConnection(conn2);
            }
        }

        // 3. Add connection mutation (configurable chance)
        if (Random.value < addConnectionRate)
        {
            for (int tries = 0; tries < 5; tries++) // Try 5 times to find valid connection
            {
                var nodeList = new List<NEAT.Genes.NodeGene>(genome.Nodes.Values);
                var sourceNode = nodeList[Random.Range(0, nodeList.Count)];
                var targetNode = nodeList[Random.Range(0, nodeList.Count)];

                // Skip invalid connections:
                // - Must be from a lower layer to a higher layer
                // - Cannot connect input to input or output to output
                // - Cannot connect to input or from output
                if (sourceNode.Layer >= targetNode.Layer ||
                    sourceNode.Type == NEAT.Genes.NodeType.Output ||
                    targetNode.Type == NEAT.Genes.NodeType.Input)
                {
                    continue;
                }

                // Check if connection already exists
                bool exists = genome.Connections.Values.Any(c =>
                    c.InputKey == sourceNode.Key && c.OutputKey == targetNode.Key);

                if (!exists)
                {
                    var newConn = new NEAT.Genes.ConnectionGene(
                        genome.Connections.Count,
                        sourceNode.Key,
                        targetNode.Key,
                        Random.Range(-1f, 1f));

                    genome.AddConnection(newConn);
                    break;
                }
            }
        }

        // 4. Delete connection mutation (configurable chance)
        if (Random.value < deleteConnectionRate && genome.Connections.Count > 1)
        {
            var connList = new List<NEAT.Genes.ConnectionGene>(genome.Connections.Values);
            var connToDelete = connList[Random.Range(0, connList.Count)];
            genome.Connections.Remove(connToDelete.Key);
        }
    }
    
    private void FixedUpdate()
    {
        // Apply aging (linear damage based on lifetime after a delay)
        lifetime += Time.fixedDeltaTime;
        if (lifetime > agingStartTime)
        {
            health -= agingRate * Time.fixedDeltaTime;
        }
        
        // Replenish energy over time
        energy = Mathf.Min(energy + energyRechargeRate * Time.fixedDeltaTime, maxEnergy);
        
        if (brain != null)
        {
            // Get actions from neural network - moved out of the conditional blocks
            float[] actions = GetActions();
            
            // If we're moving to mate, override normal movement
            if (isMovingToMate && targetMate != null)
            {
                MoveTowardsMate();
            }
            // If waiting for mate, don't move
            else if (isWaitingForMate)
            {
                rb.velocity = Vector2.zero;
                
                // Check if we should cancel waiting (if mate is gone)
                if (targetMate == null || !targetMate.gameObject || !targetMate.isActiveAndEnabled)
                {
                    FreeMate();
                }
            }
            // Normal movement
            else if (!isReproducing)
            {
                // Only apply movement if not waiting for mate
                if (!isWaitingForMate)
                {
                    // Apply movement based on neural network output
                    Vector2 moveDirection = Vector2.zero;
                    moveDirection.x = actions[0];  // Left/right movement
                    moveDirection.y = actions[1];  // Up/down movement
                    
                    // Normalize to ensure diagonal movement isn't faster
                    if (moveDirection.magnitude > 1f)
                    {
                        moveDirection.Normalize();
                    }
                    
                    // Apply move speed with bounds check
                    Vector2 desiredVelocity = moveDirection * moveSpeed;
                    ApplyMovementWithBoundsCheck(desiredVelocity);
                }
            }
            
            // Process action commands (chop and attack)
            ProcessActionCommands(actions);
            
            // Update reproduction
            UpdateReproduction();
        }
        
        // Check if we should die
        if (health <= 0f)
        {
            Destroy(gameObject);
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
        // We need at least 4 actions: move x, move y, chop, attack
        if (actions.Length < 4) return;
        
        // Only execute actions if we have enough energy
        if (energy >= actionEnergyCost)
        {
            float chopDesire = actions[2];
            float attackDesire = actions[3];
            
            // Both values must be positive to be considered
            if (chopDesire > 0 || attackDesire > 0)
            {
                // Choose the action with the highest positive value
                if (chopDesire > attackDesire && chopDesire > 0)
                {
                    if (TryChopTree())
                    {
                        // Reset energy after successful action
                        energy -= actionEnergyCost;
                    }
                }
                else if (attackDesire > 0)
                {
                    if (TryAttackCreature())
                    {
                        // Reset energy after successful action
                        energy -= actionEnergyCost;
                    }
                }
            }
        }
    }

    private bool TryChopTree()
    {
        // Find the nearest tree within detection radius
        TreeHealth nearestTree = null;
        float nearestDistance = float.MaxValue;
        
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, CreatureObserver.DETECTION_RADIUS);
        
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
        // If we were someone's target mate, free them but handle exceptions
        if (targetMate != null && targetMate.gameObject != null && targetMate.gameObject.activeInHierarchy)
        {
            // Break the circular reference first
            var tempMate = targetMate;
            targetMate = null;
            
            // Reset their flags directly instead of using FreeMate which may start coroutines
            tempMate.isMovingToMate = false;
            tempMate.isWaitingForMate = false;
            tempMate.isReproducing = false;
            tempMate.reproduction = 0f;
            tempMate.canStartReproducing = false;
            tempMate.targetMate = null;
            
            // If they're still alive and active, they can start their own timer
            if (tempMate.gameObject.activeInHierarchy)
            {
                try 
                {
                    tempMate.StartCoroutine(tempMate.DelayedReproductionStart());
                }
                catch (System.Exception)
                {
                    // Ignore any exceptions if coroutines can't be started
                }
            }
        }

        // Decrement counter when creature is destroyed
        totalCreatures--;
        // Debug.Log(string.Format("Creature destroyed. Total creatures: {0}", totalCreatures));
    }

    private void OnGUI()
    {
        // Get screen position for this creature
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        screenPos.y = Screen.height - screenPos.y; // GUI uses top-left origin

        // Only show if on screen
        if (screenPos.x >= 0 && screenPos.x <= Screen.width && 
            screenPos.y >= 0 && screenPos.y <= Screen.height)
        {
            string status = "";
            if (isMovingToMate)
                status = "Moving to mate";
            else if (isWaitingForMate)
                status = "Waiting for mate";
            else if (isReproducing)
                status = "Reproducing";

            // Show age and status
            GUI.Label(new Rect(screenPos.x - 50, screenPos.y - 40, 100, 20), 
                     string.Format("Age: {0:F1}", lifetime));
            
            if (status != "")
            {
                GUI.Label(new Rect(screenPos.x - 50, screenPos.y - 60, 100, 20), 
                         status);
            }
        }
    }

    private void OnDrawGizmos()
    {
        // Only draw if visualization is enabled and NEATTest reference exists
        if (neatTest != null && neatTest.showDetectionRadius)
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
    }

    private IEnumerator BeginReproduction()
    {
        // Validate all required components before proceeding
        if (targetMate == null)
        {
            // Debug.LogError(string.Format("{0}: Cannot begin reproduction - targetMate is null", gameObject.name));
            FreeMate();
            yield break;
        }

        if (targetMate.gameObject == null)
        {
            // Debug.LogError(string.Format("{0}: Cannot begin reproduction - targetMate.gameObject is null", gameObject.name));
            FreeMate();
            yield break;
        }

        // Debug.Log(string.Format("{0} beginning reproduction with {1}", gameObject.name, targetMate.gameObject.name));

        // Get floor collider with null check
        var floorObj = GameObject.FindGameObjectWithTag("Floor");
        if (floorObj == null)
        {
            // Debug.LogError(string.Format("{0}: Cannot begin reproduction - Floor object not found", gameObject.name));
            FreeMate();
            yield break;
        }

        var floorCollider = floorObj.GetComponent<PolygonCollider2D>();
        if (floorCollider == null)
        {
            // Debug.LogError(string.Format("{0}: Cannot begin reproduction - Floor collider not found", gameObject.name));
            FreeMate();
            yield break;
        }

        // Get sprite renderer with null check
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            // Debug.LogError(string.Format("{0}: Cannot begin reproduction - SpriteRenderer or sprite is null", gameObject.name));
            FreeMate();
            yield break;
        }

        WaitForSeconds spawnDelay = new WaitForSeconds(0.3f);
        bool spawnSuccessful = false;
        Vector2 validPosition = Vector2.zero;
        
        // Try to find a valid spawn position first
        while (!spawnSuccessful && health > 0 && targetMate != null && targetMate.health > 0)
        {
            Vector2 spawnOffset = Random.insideUnitCircle * 2f;
            Vector2 potentialPosition = (Vector2)transform.position + spawnOffset;
            
            // Calculate where the bottom center would be using our existing sprite
            Vector2 bottomCenter = new Vector2(
                potentialPosition.x,
                potentialPosition.y - spriteRenderer.bounds.extents.y
            );
            
            if (floorCollider.OverlapPoint(bottomCenter))
            {
                validPosition = potentialPosition;
                spawnSuccessful = true;
            }
            else
            {
                yield return spawnDelay;
            }
        }

        // Final validation before creating offspring
        if (!spawnSuccessful)
        {
            // Debug.LogError(string.Format("{0}: Failed to find valid spawn position", gameObject.name));
            FreeMate();
            yield break;
        }

        if (targetMate == null || !targetMate.gameObject)
        {
            // Debug.LogError(string.Format("{0}: Target mate became invalid during spawn position search", gameObject.name));
            FreeMate();
            yield break;
        }

        try
        {
            // Validate brains
            if (brain == null)
            {
                // Debug.LogError(string.Format("{0}: Brain is null", gameObject.name));
                throw new System.Exception("Brain is null");
            }

            var parent2Brain = targetMate.GetBrain();
            if (parent2Brain == null)
            {
                // Debug.LogError(string.Format("{0}: Target mate's brain is null", gameObject.name));
                throw new System.Exception("Target mate's brain is null");
            }

            // Get nodes and connections with null checks
            var parent1Nodes = brain.GetType().GetField("_nodes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(brain) as Dictionary<int, NEAT.Genes.NodeGene>;
            var parent1Connections = brain.GetType().GetField("_connections", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(brain) as Dictionary<int, NEAT.Genes.ConnectionGene>;
            var parent2Nodes = parent2Brain.GetType().GetField("_nodes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(parent2Brain) as Dictionary<int, NEAT.Genes.NodeGene>;
            var parent2Connections = parent2Brain.GetType().GetField("_connections", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(parent2Brain) as Dictionary<int, NEAT.Genes.ConnectionGene>;

            if (parent1Nodes == null || parent1Connections == null || parent2Nodes == null || parent2Connections == null)
            {
                // Debug.LogError(string.Format("{0}: Failed to get neural network data", gameObject.name));
                throw new System.Exception("Failed to get neural network data");
            }

            // Create offspring
            var childGenome = new NEAT.Genome.Genome((int)(Time.time * 1000) % 1000000);

            // Perform crossover
            var allNodeKeys = new HashSet<int>(parent1Nodes.Keys.Concat(parent2Nodes.Keys));
            foreach (var nodeKey in allNodeKeys)
            {
                NEAT.Genes.NodeGene nodeToAdd = null;
                if (parent1Nodes.ContainsKey(nodeKey) && parent2Nodes.ContainsKey(nodeKey))
                {
                    nodeToAdd = (NEAT.Genes.NodeGene)(Random.value < 0.5f ? parent1Nodes[nodeKey].Clone() : parent2Nodes[nodeKey].Clone());
                }
                else if (parent1Nodes.ContainsKey(nodeKey))
                {
                    nodeToAdd = (NEAT.Genes.NodeGene)parent1Nodes[nodeKey].Clone();
                }
                else
                {
                    nodeToAdd = (NEAT.Genes.NodeGene)parent2Nodes[nodeKey].Clone();
                }
                childGenome.AddNode(nodeToAdd);
            }

            var allConnectionKeys = new HashSet<int>(parent1Connections.Keys.Concat(parent2Connections.Keys));
            foreach (var connKey in allConnectionKeys)
            {
                NEAT.Genes.ConnectionGene connToAdd = null;
                if (parent1Connections.ContainsKey(connKey) && parent2Connections.ContainsKey(connKey))
                {
                    connToAdd = (NEAT.Genes.ConnectionGene)(Random.value < 0.5f ? parent1Connections[connKey].Clone() : parent2Connections[connKey].Clone());
                }
                else if (parent1Connections.ContainsKey(connKey))
                {
                    connToAdd = (NEAT.Genes.ConnectionGene)parent1Connections[connKey].Clone();
                }
                else
                {
                    connToAdd = (NEAT.Genes.ConnectionGene)parent2Connections[connKey].Clone();
                }
                childGenome.AddConnection(connToAdd);
            }

            ApplyMutations(childGenome);

            // Create and initialize offspring
            GameObject offspring = Instantiate(gameObject, validPosition, Quaternion.identity);
            if (offspring == null)
            {
                throw new System.Exception("Failed to instantiate offspring");
            }

            Creature offspringCreature = offspring.GetComponent<Creature>();
            if (offspringCreature == null)
            {
                Destroy(offspring);
                throw new System.Exception("Offspring missing Creature component");
            }

            var network = NEAT.NN.FeedForwardNetwork.Create(childGenome);
            if (network == null)
            {
                Destroy(offspring);
                throw new System.Exception("Failed to create neural network for offspring");
            }

            offspringCreature.InitializeNetwork(network);
            offspringCreature.type = type;
            
            // Initialize offspring's reproduction state
            offspringCreature.reproduction = 0f;
            offspringCreature.canStartReproducing = false;
            offspringCreature.isReproducing = false;
            offspringCreature.isMovingToMate = false;
            offspringCreature.isWaitingForMate = false;
            offspringCreature.targetMate = null;
            offspringCreature.StartCoroutine(offspringCreature.DelayedReproductionStart());
            
            // Debug.Log(string.Format("{0}: Successfully created offspring at position {1}", gameObject.name, validPosition));
            
            // Reset both parents
            var tempMate = targetMate; // Store reference in case it becomes null
            FreeMate();
            if (tempMate != null && tempMate.gameObject != null)
            {
                tempMate.FreeMate();
            }
        }
        catch (System.Exception e)
        {
            // Debug.LogError(string.Format("{0}: Exception in BeginReproduction: {1}", gameObject.name, e));
            FreeMate();
        }
    }

    private void MoveTowardsMate()
    {
        if (targetMate == null || !targetMate.gameObject.activeInHierarchy)
        {
            FreeMate();
            return;
        }
    
        // Calculate direction and distance to mate
        Vector2 directionToMate = (targetMate.transform.position - transform.position);
        float distanceToMate = directionToMate.magnitude;
        
        // Check if we've arrived
        if (distanceToMate < 0.5f)
        {
            // We've arrived at mate position
            isMovingToMate = false;
            rb.velocity = Vector2.zero; // Stop moving
            
            // Start reproduction process
            StartCoroutine(BeginReproduction());
            return;
        }
        
        // Calculate movement direction and speed
        // Handle the case where creatures are aligned on one axis
        float xDiff = Mathf.Abs(directionToMate.x);
        float yDiff = Mathf.Abs(directionToMate.y);
        Vector2 normalizedDirection;
        
        // If the x difference is very small and the y difference is significant
        if (xDiff < 0.1f && yDiff > 0.5f)
        {
            // Force a purely vertical movement
            normalizedDirection = new Vector2(0, Mathf.Sign(directionToMate.y));
        }
        // If the y difference is very small and the x difference is significant
        else if (yDiff < 0.1f && xDiff > 0.5f)
        {
            // Force a purely horizontal movement
            normalizedDirection = new Vector2(Mathf.Sign(directionToMate.x), 0);
        }
        else
        {
            // Normal case - normalize the direction
            normalizedDirection = directionToMate.normalized;
        }
        
        float speed = moveSpeed;
        
        // Check floor bounds before moving
        if (cachedFloorCollider == null)
        {
            GameObject floorObj = GameObject.FindGameObjectWithTag("Floor");
            if (floorObj != null)
            {
                cachedFloorCollider = floorObj.GetComponent<PolygonCollider2D>();
                floorBounds = cachedFloorCollider.bounds;
            }
        }
        
        // Apply movement with a simple bounds check
        Vector2 newPosition = rb.position + normalizedDirection * speed * Time.fixedDeltaTime;
        
        // If we'd go out of bounds, adjust the direction to move along the boundary
        if (cachedFloorCollider != null && !cachedFloorCollider.OverlapPoint(newPosition))
        {
            // Try moving just horizontally or just vertically toward the target
            Vector2 horizontalDir = new Vector2(normalizedDirection.x, 0).normalized;
            Vector2 verticalDir = new Vector2(0, normalizedDirection.y).normalized;
            
            Vector2 horizontalPos = rb.position + horizontalDir * speed * Time.fixedDeltaTime;
            Vector2 verticalPos = rb.position + verticalDir * speed * Time.fixedDeltaTime;
            
            // Check which direction is valid and closest to the target
            bool horizontalValid = cachedFloorCollider.OverlapPoint(horizontalPos);
            bool verticalValid = cachedFloorCollider.OverlapPoint(verticalPos);
            
            if (horizontalValid && verticalValid)
            {
                // Choose the direction that gets us closer to the target
                float horizontalDist = Vector2.Distance(horizontalPos, targetMate.transform.position);
                float verticalDist = Vector2.Distance(verticalPos, targetMate.transform.position);
                
                rb.velocity = (horizontalDist < verticalDist) ? horizontalDir * speed : verticalDir * speed;
            }
            else if (horizontalValid)
            {
                rb.velocity = horizontalDir * speed;
            }
            else if (verticalValid)
            {
                rb.velocity = verticalDir * speed;
            }
            else
            {
                // If both are invalid, try to move toward the center of the floor
                Vector2 centerPos = new Vector2(floorBounds.center.x, floorBounds.center.y);
                Vector2 centerDir = (centerPos - rb.position).normalized;
                rb.velocity = centerDir * speed * 0.5f;
            }
        }
        else
        {
            // Move directly toward the mate
            rb.velocity = normalizedDirection * speed;
            
            // Add debug visualization to see the movement path
            Debug.DrawLine(transform.position, transform.position + (Vector3)rb.velocity.normalized * 1f, Color.yellow, 0.1f);
        }
    }
} 