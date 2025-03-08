using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class Creature : MonoBehaviour
{
    // Add static counter at the top of the class
    private static int totalCreatures = 0;
    private static readonly int maxCreatures = 20;
    private static NEATTest neatTest;  // Cache NEATTest reference

    [Header("Basic Stats")]
    public float health = 3f;
    public float reproduction = 0f;
    public float maxHealth = 3f;
    public float maxReproduction = 1f;
    
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
        
        // Increment counter when creature is created
        totalCreatures++;
        Debug.Log($"Creature created. Total creatures: {totalCreatures}");
    }

    private IEnumerator DelayedReproductionStart()
    {
        canStartReproducing = false;
        Debug.Log($"{gameObject.name}: Starting reproduction delay timer");
        
        // Wait for 2 seconds before allowing reproduction
        yield return new WaitForSeconds(2f);
        
        // Double check we're still alive before enabling reproduction
        if (this != null && gameObject != null)
        {
            canStartReproducing = true;
            Debug.Log($"{gameObject.name} can now start reproducing");
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
        
        // Configure Rigidbody2D for proper collisions
        rb.gravityScale = 0f;
        rb.drag = 2f;  // Increased drag for more controlled movement
        rb.angularDrag = 0.5f;
        rb.mass = 1f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;  // Always process physics
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        
        // Configure BoxCollider2D
        var collider = GetComponent<BoxCollider2D>();
        if (collider != null)
        {
            collider.isTrigger = true;  // Changed to true to disable physical collisions
            collider.offset = new Vector2(0f, GetComponent<SpriteRenderer>().bounds.extents.y);
            collider.size = new Vector2(0.6f, 0.6f);  // Slightly smaller for better separation
        }

        // Start the reproduction timer for new creatures
        StartCoroutine(DelayedReproductionStart());
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
        if (brain == null) return new float[] { 0f, 0f };
        
        float[] observations = observer.GetObservations(this);
        double[] doubleObservations = ConvertToDouble(observations);
        double[] doubleOutputs = brain.Activate(doubleObservations);
        float[] outputs = ConvertToFloat(doubleOutputs);
        
        // Ensure outputs are in range [-1, 1]
        outputs[0] = Mathf.Clamp(outputs[0], -1f, 1f);
        outputs[1] = Mathf.Clamp(outputs[1], -1f, 1f);
        
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
                Debug.LogWarning($"{gameObject.name}: Found inconsistent reproduction state, resetting...");
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
            Debug.LogWarning($"{gameObject.name}: Found stuck reproduction state, resetting...");
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
        
        // Check population limit first
        if (totalCreatures >= maxCreatures)
        {
            Debug.Log($"Max creatures ({maxCreatures}) reached, preventing reproduction");
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
            Debug.Log($"{gameObject.name}: No potential mates found, keeping reproduction ready");
            isReproducing = false;
            reproduction = maxReproduction; // Keep ready to reproduce
            yield break;
        }

        // Get the oldest mate
        Creature mate = potentialMates[0];
        
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

        if (shouldMove)
        {
            // We're the younger one, move to mate
            isMovingToMate = true;
            isReproducing = true;  // Set this flag for both creatures
            targetMate = mate;
            mate.isWaitingForMate = true;
            mate.isReproducing = true;
            mate.targetMate = this;
            Debug.Log($"{gameObject.name} (younger) moving to mate with {mate.gameObject.name}");
        }
        else
        {
            // We're the older one, wait for mate
            isWaitingForMate = true;
            isReproducing = true;  // Set this flag for both creatures
            targetMate = mate;
            mate.isMovingToMate = true;
            mate.isReproducing = true;
            mate.targetMate = this;
            Debug.Log($"{gameObject.name} (older) waiting for mate {mate.gameObject.name}");
        }
    }

    private void FreeMate()
    {
        Debug.Log($"{gameObject.name}: FreeMate called. Previous state - isReproducing: {isReproducing}, isMovingToMate: {isMovingToMate}, isWaitingForMate: {isWaitingForMate}, reproduction: {reproduction}");
        
        // Reset ALL reproduction-related flags
        isMovingToMate = false;
        isWaitingForMate = false;
        isReproducing = false;
        reproduction = 0f;
        canStartReproducing = false;  // Ensure this is false before starting the delay
        
        // If we have a target mate, log and clear it
        if (targetMate != null)
        {
            Debug.Log($"{gameObject.name} freeing mate {targetMate.gameObject.name}");
            
            // Store reference before nulling it
            var mate = targetMate;
            targetMate = null;
            
            // Also reset the target mate's flags (in case they haven't been reset)
            if (mate != null && mate.gameObject != null)
            {
                mate.isMovingToMate = false;
                mate.isWaitingForMate = false;
                mate.isReproducing = false;
                mate.reproduction = 0f;
                mate.canStartReproducing = false;  // Ensure this is false before starting the delay
                mate.targetMate = null;
                
                // Ensure the mate also starts their reproduction timer
                mate.StopCoroutine("DelayedReproductionStart");  // Stop any existing timers
                mate.StartCoroutine(mate.DelayedReproductionStart());
            }
        }
        else
        {
            targetMate = null;
        }
        
        // Stop any existing DelayedReproductionStart coroutines
        StopCoroutine("DelayedReproductionStart");
        
        // Start delayed reproduction timer again
        StartCoroutine(DelayedReproductionStart());
        
        Debug.Log($"{gameObject.name}: FreeMate completed. New state - canStartReproducing: false, reproduction: 0");
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
                if (node.Type == NEAT.Genes.NodeType.Hidden || node.Type == NEAT.Genes.NodeType.Bias)
                {
                    maxCurrentLayer = Mathf.Max(maxCurrentLayer, node.Layer);
                }
            }
            
            // Only proceed if we haven't reached the max hidden layers
            if (maxCurrentLayer < maxHiddenLayers + 1) // +1 because layer 0 is input
            {
                bool addBiasNode = Random.value < 0.9f; // 90% chance to add a bias node
                
                if (addBiasNode)
                {
                    // Pick a random hidden layer
                    int targetLayer = Random.Range(1, maxCurrentLayer + 2); // +2 because we might want to add a new layer
                    
                    // Check if this layer already has a bias node
                    bool hasBiasNode = false;
                    foreach (var node in genome.Nodes.Values)
                    {
                        if (node.Type == NEAT.Genes.NodeType.Bias && node.Layer == targetLayer)
                        {
                            hasBiasNode = true;
                            break;
                        }
                    }
                    
                    // If no bias node in this layer and it's not beyond max layers, add one
                    if (!hasBiasNode && targetLayer <= maxHiddenLayers)
                    {
                        // Create new bias node
                        int newNodeKey = genome.Nodes.Count;
                        var newNode = new NEAT.Genes.NodeGene(newNodeKey, NEAT.Genes.NodeType.Bias);
                        newNode.Layer = targetLayer;
                        genome.AddNode(newNode);
                        
                        // Add connections from this bias node to nodes in higher layers
                        foreach (var targetNode in genome.Nodes.Values)
                        {
                            if (targetNode.Layer > targetLayer)
                            {
                                var newConn = new NEAT.Genes.ConnectionGene(
                                    genome.Connections.Count,
                                    newNodeKey,
                                    targetNode.Key,
                                    Random.Range(-0.1f, 0.1f)); // Start with small random weights
                                genome.AddConnection(newConn);
                            }
                        }
                    }
                }
                else
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
                // - Cannot connect TO a bias node
                if (sourceNode.Layer >= targetNode.Layer ||
                    sourceNode.Type == NEAT.Genes.NodeType.Output ||
                    targetNode.Type == NEAT.Genes.NodeType.Input ||
                    targetNode.Type == NEAT.Genes.NodeType.Bias)
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
            
            // Don't delete connections from bias nodes as they're their only outputs
            var sourceNode = genome.Nodes[connToDelete.InputKey];
            if (sourceNode.Type != NEAT.Genes.NodeType.Bias)
            {
                genome.Connections.Remove(connToDelete.Key);
            }
        }
    }
    
    private void FixedUpdate()
    {
        // Update lifetime and apply aging damage
        lifetime += Time.fixedDeltaTime;
        if (lifetime > agingStartTime)
        {
            float agingTime = lifetime - agingStartTime;
            // Linear aging damage calculation
            float agingDamage = agingRate * agingTime * Time.fixedDeltaTime;
            health = Mathf.Max(0, health - agingDamage);
            
            // Die if health reaches 0
            if (health <= 0)
            {
                Destroy(gameObject);
                return;
            }
        }

        if (brain != null)
        {
            // If we're moving to mate, override normal movement
            if (isMovingToMate && targetMate != null)
            {
                // Calculate direction to mate
                Vector2 directionToMate = (targetMate.transform.position - transform.position).normalized;
                
                // Check if we're close enough to mate
                if (Vector2.Distance(transform.position, targetMate.transform.position) < 0.5f)
                {
                    // We've arrived at mate position
                    isMovingToMate = false;
                    
                    // Double check that targetMate is still valid before starting reproduction
                    if (targetMate != null && targetMate.gameObject != null && targetMate.isActiveAndEnabled)
                    {
                        StartCoroutine(BeginReproduction());
                    }
                    else
                    {
                        // If mate is no longer valid, reset our state
                        FreeMate();
                    }
                }
                else
                {
                    // Move towards mate
                    rb.velocity = directionToMate * moveSpeed;
                }
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
                float[] actions = GetActions();
                
                // Actions[0] is horizontal velocity, actions[1] is vertical velocity
                Vector2 desiredVelocity = new Vector2(actions[0], actions[1]) * moveSpeed;
                
                // Check if the desired position would be within bounds
                Vector2 currentPos = rb.position;
                Vector2 desiredPos = currentPos + desiredVelocity * Time.fixedDeltaTime;
                
                // Get the floor bounds
                PolygonCollider2D floorCollider = GameObject.FindGameObjectWithTag("Floor").GetComponent<PolygonCollider2D>();
                if (floorCollider != null)
                {
                    // Get the sprite's bottom center point
                    Vector2 bottomCenter = new Vector2(desiredPos.x, desiredPos.y - GetComponent<SpriteRenderer>().bounds.extents.y);
                    
                    // Check if the bottom center point would be inside the floor bounds
                    if (floorCollider.OverlapPoint(bottomCenter))
                    {
                        // Apply movement
                        rb.velocity = desiredVelocity;
                    }
                    else
                    {
                        // Stop at the current position
                        rb.velocity = Vector2.zero;
                    }
                }
                else
                {
                    // If no floor collider found, just apply movement
                    rb.velocity = desiredVelocity;
                }
            }
            
            // Update reproduction
            UpdateReproduction();
        }
    }
    
    private void OnTriggerEnter2D(Collider2D collider)
    {
        // Check if we collided with another creature
        Creature otherCreature = collider.GetComponent<Creature>();
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
        // If we were someone's target mate, free them
        if (targetMate != null)
        {
            targetMate.FreeMate();
        }

        // Decrement counter when creature is destroyed
        totalCreatures--;
        Debug.Log($"Creature destroyed. Total creatures: {totalCreatures}");
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
                     $"Age: {lifetime:F1}");
            
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
            Debug.LogError($"{gameObject.name}: Cannot begin reproduction - targetMate is null");
            FreeMate();
            yield break;
        }

        if (targetMate.gameObject == null)
        {
            Debug.LogError($"{gameObject.name}: Cannot begin reproduction - targetMate.gameObject is null");
            FreeMate();
            yield break;
        }

        Debug.Log($"{gameObject.name} beginning reproduction with {targetMate.gameObject.name}");

        // Get floor collider with null check
        var floorObj = GameObject.FindGameObjectWithTag("Floor");
        if (floorObj == null)
        {
            Debug.LogError($"{gameObject.name}: Cannot begin reproduction - Floor object not found");
            FreeMate();
            yield break;
        }

        var floorCollider = floorObj.GetComponent<PolygonCollider2D>();
        if (floorCollider == null)
        {
            Debug.LogError($"{gameObject.name}: Cannot begin reproduction - Floor collider not found");
            FreeMate();
            yield break;
        }

        // Get sprite renderer with null check
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            Debug.LogError($"{gameObject.name}: Cannot begin reproduction - SpriteRenderer or sprite is null");
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
            Debug.LogError($"{gameObject.name}: Failed to find valid spawn position");
            FreeMate();
            yield break;
        }

        if (targetMate == null || !targetMate.gameObject)
        {
            Debug.LogError($"{gameObject.name}: Target mate became invalid during spawn position search");
            FreeMate();
            yield break;
        }

        try
        {
            // Validate brains
            if (brain == null)
            {
                Debug.LogError($"{gameObject.name}: Brain is null");
                throw new System.Exception("Brain is null");
            }

            var parent2Brain = targetMate.GetBrain();
            if (parent2Brain == null)
            {
                Debug.LogError($"{gameObject.name}: Target mate's brain is null");
                throw new System.Exception("Target mate's brain is null");
            }

            // Get nodes and connections with null checks
            var parent1Nodes = brain.GetType().GetField("_nodes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(brain) as Dictionary<int, NEAT.Genes.NodeGene>;
            var parent1Connections = brain.GetType().GetField("_connections", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(brain) as Dictionary<int, NEAT.Genes.ConnectionGene>;
            var parent2Nodes = parent2Brain.GetType().GetField("_nodes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(parent2Brain) as Dictionary<int, NEAT.Genes.NodeGene>;
            var parent2Connections = parent2Brain.GetType().GetField("_connections", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(parent2Brain) as Dictionary<int, NEAT.Genes.ConnectionGene>;

            if (parent1Nodes == null || parent1Connections == null || parent2Nodes == null || parent2Connections == null)
            {
                Debug.LogError($"{gameObject.name}: Failed to get neural network data");
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
            
            Debug.Log($"{gameObject.name}: Successfully created offspring at position {validPosition}");
            
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
            Debug.LogError($"{gameObject.name}: Exception in BeginReproduction: {e}");
            FreeMate();
        }
    }
} 