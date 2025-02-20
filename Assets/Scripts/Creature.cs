using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Creature : MonoBehaviour
{
    [Header("Basic Stats")]
    public float health = 3f;
    public float energy = 5f;
    public float reproduction = 0f;
    public float maxEnergy = 5f;
    // public float maxReproduction = 5f;
    public float maxReproduction = 1f;
    
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotateSpeed = 180f;
    
    [Header("Energy Settings")]
    public float movementEnergyCost = 0.2f;
    public float rotationEnergyCost = 0.1f;
    public float energyRegenRate = 0.5f;  // Energy regenerated per second when stationary
    public float energyRegenDelay = 0.5f; // Time to wait before regenerating energy after movement
    
    [Header("Reproduction Settings")]
    public float reproductionRate = 0.1f;  // Points gained per second
    public float weightMutationRate = 0.8f;  // Chance of mutating each connection weight
    public float mutationRange = 0.5f;       // Maximum weight change during mutation
    public float addNodeRate = 0.2f;         // Chance of adding a new node
    public float addConnectionRate = 0.5f;    // Chance of adding a new connection
    public float deleteConnectionRate = 0.2f; // Chance of deleting a connection
    
    // Type
    public enum CreatureType { Albert, Kai }
    public CreatureType type;
    
    // Neural Network
    private NEAT.NN.FeedForwardNetwork brain;
    private CreatureObserver observer;
    private Rigidbody2D rb;
    private float lastMovementTime;
    
    // Add method to get brain
    public NEAT.NN.FeedForwardNetwork GetBrain()
    {
        return brain;
    }
    
    private void Awake()
    {
        // Initialize stats
        health = 3f;
        energy = maxEnergy;
        reproduction = 0f;
        lastMovementTime = -energyRegenDelay; // Allow immediate regen at start
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
    
    private void RegenerateEnergy()
    {
        if (Time.time - lastMovementTime >= energyRegenDelay)
        {
            energy = Mathf.Min(maxEnergy, energy + energyRegenRate * Time.fixedDeltaTime);
        }
    }
    
    private void UpdateReproduction()
    {
        // Only accumulate reproduction points if we have some energy
        if (energy > 0)
        {
            reproduction += reproductionRate * Time.fixedDeltaTime;
            
            // Check if ready to reproduce
            if (reproduction >= maxReproduction)
            {
                Reproduce();
            }
        }
    }
    
    private void Reproduce()
    {
        if (brain == null) return;

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
            // Choose a random connection to split
            var connList = new List<NEAT.Genes.ConnectionGene>(genome.Connections.Values);
            var connToSplit = connList[Random.Range(0, connList.Count)];
            connToSplit.Enabled = false;

            // Create new node
            int newNodeKey = genome.Nodes.Count;
            var newNode = new NEAT.Genes.NodeGene(newNodeKey, NEAT.Genes.NodeType.Hidden);
            
            // Set layer between input and output
            var inputNode = genome.Nodes[connToSplit.InputKey];
            var outputNode = genome.Nodes[connToSplit.OutputKey];
            newNode.Layer = (inputNode.Layer + outputNode.Layer) / 2;
            if (newNode.Layer == inputNode.Layer) newNode.Layer++;
            
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

        // 3. Add connection mutation (configurable chance)
        if (Random.value < addConnectionRate)
        {
            for (int tries = 0; tries < 5; tries++) // Try 5 times to find valid connection
            {
                var nodeList = new List<NEAT.Genes.NodeGene>(genome.Nodes.Values);
                var sourceNode = nodeList[Random.Range(0, nodeList.Count)];
                var targetNode = nodeList[Random.Range(0, nodeList.Count)];

                // Skip invalid connections
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

        // Create offspring
        GameObject offspring = Instantiate(gameObject, transform.position, transform.rotation);
        Creature offspringCreature = offspring.GetComponent<Creature>();
        
        // Initialize offspring with mutated brain
        var network = NEAT.NN.FeedForwardNetwork.Create(genome);
        offspringCreature.InitializeNetwork(network);
        offspringCreature.type = type;
        
        // Reset reproduction points
        reproduction = 0f;
        
        // Offset offspring slightly to avoid overlap
        Vector2 offset = Random.insideUnitCircle.normalized;
        offspring.transform.position += (Vector3)offset;
    }
    
    private void FixedUpdate()
    {
        if (brain != null)
        {
            float[] actions = GetActions();
            
            // Calculate movement
            float forwardSpeed = actions[0] * moveSpeed;
            float rotationSpeed = actions[1] * rotateSpeed;
            
            // Apply movement if we have enough energy
            if (energy > 0)
            {
                // Calculate energy costs
                float moveCost = Mathf.Abs(forwardSpeed) * movementEnergyCost * Time.fixedDeltaTime;
                float rotateCost = Mathf.Abs(rotationSpeed) * rotationEnergyCost * Time.fixedDeltaTime;
                float totalEnergyCost = moveCost + rotateCost;
                
                if (totalEnergyCost <= energy)
                {
                    // Apply forward movement
                    Vector2 movement = (Vector2)(transform.right * forwardSpeed);
                    rb.velocity = movement;
                    
                    // Apply rotation
                    rb.angularVelocity = rotationSpeed;
                    
                    // Deduct energy
                    energy = Mathf.Max(0, energy - totalEnergyCost);
                    
                    // Update last movement time
                    if (Mathf.Abs(forwardSpeed) > 0.01f || Mathf.Abs(rotationSpeed) > 0.01f)
                    {
                        lastMovementTime = Time.time;
                    }
                }
                else
                {
                    // Not enough energy for full movement, use what we have
                    float energyRatio = energy / totalEnergyCost;
                    rb.velocity = (Vector2)(transform.right * forwardSpeed * energyRatio);
                    rb.angularVelocity = rotationSpeed * energyRatio;
                    energy = 0;
                    lastMovementTime = Time.time;
                }
            }
            else
            {
                // No energy, stop movement
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
            
            // Regenerate energy when not moving
            if (rb.velocity.magnitude < 0.01f && Mathf.Abs(rb.angularVelocity) < 0.01f)
            {
                RegenerateEnergy();
            }
            
            // Update reproduction
            UpdateReproduction();
        }
    }
    
    private bool isMainAlbert
    {
        get { return type == CreatureType.Albert && transform.position == Vector3.zero; }
    }
} 