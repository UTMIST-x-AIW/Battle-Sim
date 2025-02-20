using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class Creature : MonoBehaviour
{
    // Add static counter at the top of the class
    private static int totalCreatures = 0;
    private static readonly int maxCreatures = 20;

    [Header("Basic Stats")]
    public float health = 3f;
    public float reproduction = 0f;
    public float maxHealth = 3f;
    public float maxReproduction = 1f;
    
    [Header("Aging Settings")]
    public float agingStartTime = 10f;  // Time in seconds before aging starts
    public float agingRate = 0.1f;      // Base rate of aging damage
    public float agingExponent = 1.5f;  // How quickly aging accelerates
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
    
    private void Awake()
    {
        // Initialize stats
        health = maxHealth;
        reproduction = 0f;
        lifetime = 0f;
        
        // Increment counter when creature is created
        totalCreatures++;
        Debug.Log($"Creature created. Total creatures: {totalCreatures}");
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
        reproduction += reproductionRate * Time.fixedDeltaTime;
        
        // Check if ready to reproduce
        if (reproduction >= maxReproduction)
        {
            StartCoroutine(TryReproduce());
        }
    }
    
    private IEnumerator TryReproduce()
    {
        // Check population limit first
        if (totalCreatures >= maxCreatures)
        {
            Debug.Log($"Max creatures ({maxCreatures}) reached, preventing reproduction");
            reproduction = 0f; // Reset reproduction progress
            yield break;
        }

        if (brain == null) yield break;

        // Create a new genome with a unique key based on timestamp
        int newKey = (int)(Time.time * 1000) % 1000000;
        var genome = new NEAT.Genome.Genome(newKey);

        // Get nodes and connections from the brain's network
        var nodes = brain.GetType().GetField("_nodes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(brain) as Dictionary<int, NEAT.Genes.NodeGene>;
        var connections = brain.GetType().GetField("_connections", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(brain) as Dictionary<int, NEAT.Genes.ConnectionGene>;

        // Clone nodes and connections
        foreach (var node in nodes.Values)
        {
            genome.AddNode((NEAT.Genes.NodeGene)node.Clone());
        }
        foreach (var conn in connections.Values)
        {
            genome.AddConnection((NEAT.Genes.ConnectionGene)conn.Clone());
        }

        // Apply mutations
        ApplyMutations(genome);

        // Keep trying to spawn until successful or dead
        bool spawnSuccessful = false;
        PolygonCollider2D floorCollider = GameObject.FindGameObjectWithTag("Floor").GetComponent<PolygonCollider2D>();
        
        WaitForSeconds spawnDelay = new WaitForSeconds(0.3f);  // Create delay object once
        
        while (!spawnSuccessful && health > 0)
        {
            // Create offspring
            GameObject offspring = Instantiate(gameObject, transform.position, transform.rotation);
            Creature offspringCreature = offspring.GetComponent<Creature>();
            
            // Initialize offspring with mutated brain
            var network = NEAT.NN.FeedForwardNetwork.Create(genome);
            offspringCreature.InitializeNetwork(network);
            offspringCreature.type = type;
            
            if (floorCollider != null)
            {
                // Get random offset (slightly larger range for more spread)
                Vector2 spawnOffset = Random.insideUnitCircle * 2f;
                Vector2 potentialPosition = (Vector2)transform.position + spawnOffset;
                
                // Check if the bottom center of the sprite would be inside the floor
                Vector2 bottomCenter = new Vector2(
                    potentialPosition.x,
                    potentialPosition.y - offspring.GetComponent<SpriteRenderer>().bounds.extents.y
                );
                
                if (floorCollider.OverlapPoint(bottomCenter))
                {
                    offspring.transform.position = potentialPosition;
                    spawnSuccessful = true;
                    reproduction = 0f;  // Reset reproduction points on success
                }
                else
                {
                    // Failed spawn attempt, destroy offspring and wait before trying again
                    Destroy(offspring);
                    yield return spawnDelay;  // Wait 0.3 seconds before next attempt
                }
            }
            else
            {
                // If no floor collider found, just offset slightly
                Vector2 offset = Random.insideUnitCircle.normalized;
                offspring.transform.position += (Vector3)offset;
                spawnSuccessful = true;
                reproduction = 0f;
            }
        }
        
        // If we died trying, reset reproduction progress
        if (!spawnSuccessful)
        {
            reproduction = 0f;
        }
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
            float agingDamage = agingRate * Mathf.Pow(agingTime, agingExponent) * Time.fixedDeltaTime;
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
            
            // Update reproduction
            UpdateReproduction();
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
        // Decrement counter when creature is destroyed
        totalCreatures--;
        Debug.Log($"Creature destroyed. Total creatures: {totalCreatures}");
    }
} 