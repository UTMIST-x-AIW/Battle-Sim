using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class NEATTest : MonoBehaviour
{
    private static NEATTest instance;

    [Header("Creature Prefabs")]
    public GameObject albertCreaturePrefab;  // Assign in inspector
    public GameObject kaiCreaturePrefab;    // Assign in inspector
    [SerializeField]
    public static int num_alberts=20;
    
    [Header("Network Settings")]
    public int maxHiddenLayers = 10;  // Maximum number of hidden layers allowed
    public int maxCreatures = 20;     // Maximum number of creatures allowed in the simulation

    [Header("Test Settings")]
    public bool runTests = true;
    public int currentTest = 1;

    // Test scenarios
    private const int TEST_NORMAL_GAME = 0;
    private const int TEST_MATING_MOVEMENT = 1;
    private const int TEST_ALBERTS_ONLY = 2;  // New test case
    private const int TEST_REPRODUCTION = 3;  // Test for reproduction action

    [Header("Visualization Settings")]
    public bool showDetectionRadius = false;  // Toggle for detection radius visualization
    
    // NEAT instance for access by other classes
    [System.NonSerialized]
    public NEAT.Genome.Genome neat = new NEAT.Genome.Genome(0);

    public int creature_count = 50;

    private void Awake()
    {
        // Check if there's already an instance
        if (instance != null && instance != this)
        {
            Debug.LogError($"Found duplicate NEATTest on {gameObject.name}. There should only be one NEATTest component in the scene!");
            Destroy(this);
            return;
        }
        instance = this;
    }
    
    void Start()
    {
        // Only proceed if we're the main instance
        if (instance != this) return;

        // Debug.Log($"NEATTest starting on GameObject: {gameObject.name}");

        // Clear any existing creatures first
        var existingCreatures = GameObject.FindObjectsOfType<Creature>();
        // Debug.Log($"Found {existingCreatures.Length} existing creatures to clean up");
        foreach (var creature in existingCreatures)
        {
            Destroy(creature.gameObject);
        }

        if (runTests)
        {
            switch (currentTest)
            {
                case TEST_NORMAL_GAME:
                    SetupNormalGame();
                    break;
                case TEST_MATING_MOVEMENT:
                    SetupMatingMovementTest();
                    break;
                case TEST_ALBERTS_ONLY:
                    SetupAlbertsOnlyTest();
                    break;
                case TEST_REPRODUCTION:
                    SetupReproductionTest();
                    break;
                default:
                    SetupNormalGame();
                    break;
            }
        }
        else
        {
            SetupNormalGame();
        }
    }

    private void Update()
    {
        if (num_alberts < 20)
        {
            Debug.Log("NUM:" + num_alberts.ToString());
            // Spawn area in top left
            Vector2 spawnCenter = new Vector2(-5f, -0f);
            float spreadRadius = 10f;
            // Calculate a position with some randomness
            Vector2 offset = UnityEngine.Random.insideUnitCircle * spreadRadius;
            Vector3 position = new Vector3(
                spawnCenter.x + offset.x,
                spawnCenter.y + offset.y,
                0f
            );
            SpawnCreature(albertCreaturePrefab, position, Creature.CreatureType.Albert, false);
        }
    }

    private void SetupNormalGame()
    {
        // Debug.Log("Setting up normal game");
        
        // Spawn three Alberts in top left
        Vector3[] albertPositions = {
            new Vector3(-12f, 6f, 0f),
            new Vector3(-10f, 4f, 0f),
            new Vector3(-11f, 2f, 0f)
        };
        
        foreach (var basePos in albertPositions)
        {
            Vector3 position = basePos + Random.insideUnitSphere * 2f;
            position.z = 0f;
            SpawnCreature(albertCreaturePrefab, position, Creature.CreatureType.Albert, false);
        }
        
        // Spawn three Kais in bottom right
        Vector3[] kaiPositions = {
            new Vector3(12f, -6f, 0f),
            new Vector3(10f, -4f, 0f),
            new Vector3(11f, -2f, 0f)
        };
        
        foreach (var basePos in kaiPositions)
        {
            Vector3 position = basePos + Random.insideUnitSphere * 2f;
            position.z = 0f;
            SpawnCreature(kaiCreaturePrefab, position, Creature.CreatureType.Kai, true);
        }
    }

    private void SetupMatingMovementTest()
    {
        // Debug.Log("Starting Test 1: Basic Mating Movement");
        
        // Create two Alberts far from each other but still within detection radius
        Vector3 olderPosition = new Vector3(-2.5f, -2.5f, 0f);
        Vector3 youngerPosition = new Vector3(2.5f, 2.5f, 0f); // 8 units diagonal distance
        
        // Spawn older creature
        var olderCreature = SpawnCreature(albertCreaturePrefab, olderPosition, Creature.CreatureType.Albert, false);
        
        // Spawn younger creature
        var youngerCreature = SpawnCreature(albertCreaturePrefab, youngerPosition, Creature.CreatureType.Albert, false);
        

        // Debug.Log("Test 1 Setup Complete:");
        // Debug.Log($"- Older creature at {olderPosition}, age: 20");
        // Debug.Log($"- Younger creature at {youngerPosition}, age: 10");
        // Debug.Log("Expected behavior: Younger creature should move toward older creature, older creature should stay still");
        // Debug.Log("Note: Reproduction will be enabled after a 2-second delay");
    }

    private void SetupAlbertsOnlyTest()
    {
        Debug.Log("Starting Test: Alberts Only - 8 Alberts with random brains in top left");
        
        // Spawn area in top left
        Vector2 spawnCenter = new Vector2(-5f, -0f);
        float spreadRadius = 10f;
        
        // Spawn 8 Alberts in the top left corner
        for (int i = 0; i < num_alberts; i++)
        {
            // Calculate a position with some randomness
            Vector2 offset = Random.insideUnitCircle * spreadRadius;
            Vector3 position = new Vector3(
                spawnCenter.x + offset.x,
                spawnCenter.y + offset.y,
                0f
            );
            
            // Spawn the creature with a randomized brain
            var creature = SpawnCreatureWithRandomizedBrain(albertCreaturePrefab, position, Creature.CreatureType.Albert);
            
            // Initialize the creature with varied ages to encourage dynamic behavior
            float startingAge = Random.Range(5f, 15f);
            float startingReproduction = Random.Range(0f, creature.maxReproduction);
        }
        
        Debug.Log("Test setup complete: 8 Alberts with random brains spawned in top left");
    }
    
    private void SetupReproductionTest()
    {
        Debug.Log("Starting Test 3: Reproduction Action Test");
        
        // Create four creatures: two with high reproduction desire, two with low
        
        // Spawn creatures in a square pattern
        Vector2[] positions = {
            new Vector2(-5f, 5f),   // Top left - High reproduction creature
            new Vector2(5f, 5f),    // Top right - Low reproduction creature
            new Vector2(-5f, -5f),  // Bottom left - High reproduction creature
            new Vector2(5f, -5f)    // Bottom right - Low reproduction creature
        };
        
        // Spawn high reproduction desire creatures (Albert)
        var highRepro1 = SpawnCreatureWithReproductionBias(albertCreaturePrefab, positions[0], Creature.CreatureType.Albert, 0.9f);
        var highRepro2 = SpawnCreatureWithReproductionBias(albertCreaturePrefab, positions[2], Creature.CreatureType.Albert, 0.9f);
        
        // Spawn low reproduction desire creatures (Albert)
        var lowRepro1 = SpawnCreatureWithReproductionBias(albertCreaturePrefab, positions[1], Creature.CreatureType.Albert, 0.1f);
        var lowRepro2 = SpawnCreatureWithReproductionBias(albertCreaturePrefab, positions[3], Creature.CreatureType.Albert, 0.1f);
        
        // Set full energy for all creatures
        highRepro1.energy = highRepro1.maxEnergy;
        highRepro2.energy = highRepro2.maxEnergy;
        lowRepro1.energy = lowRepro1.maxEnergy;
        lowRepro2.energy = lowRepro2.maxEnergy;
        
        Debug.Log("Test 3 Setup Complete:");
        Debug.Log("- Top left & Bottom left: High reproduction desire (0.9)");
        Debug.Log("- Top right & Bottom right: Low reproduction desire (0.1)");
        Debug.Log("Expected behavior: High reproduction creatures should prioritize reproduction and create offspring");
        Debug.Log("Low reproduction creatures should prioritize other actions (like movement)");
    }
    
    private Creature SpawnCreature(GameObject prefab, Vector3 position, Creature.CreatureType type, bool isKai)
    {
        var creature = Instantiate(prefab, position, Quaternion.identity);
        var creatureComponent = creature.GetComponent<Creature>();
        creatureComponent.type = type;
        
        // Create initial neural network with appropriate genome
        var genome = isKai ? CreateInitialKaiGenome() : CreateInitialGenome();
        var network = NEAT.NN.FeedForwardNetwork.Create(genome);
        creatureComponent.InitializeNetwork(network);
        
        // Pass the max hidden layers setting to the creature
        creatureComponent.maxHiddenLayers = maxHiddenLayers;

        return creatureComponent;
    }
    
    NEAT.Genome.Genome CreateInitialGenome()
    {
        var genome = new NEAT.Genome.Genome(0);
        
        // Add input nodes (13 inputs total): 
        // 0: health
        // 1: reproduction
        // 2: energy
        // 3,4: same type x,y
        // 5,6: opposite type x,y
        // 7,8: cherry x,y
        // 9,10: tree x,y
        // 11,12: current position x,y
        for (int i = 0; i < 13; i++)
        {
            var node = new NEAT.Genes.NodeGene(i, NEAT.Genes.NodeType.Input);
            node.Layer = 0;  // Input layer
            node.Bias = 0.0; // Explicitly set bias to 0 for input nodes
            genome.AddNode(node);
        }
        
        // Add output nodes (5 outputs: x,y velocity, chop, attack, reproduce)
        var outputNode1 = new NEAT.Genes.NodeGene(17, NEAT.Genes.NodeType.Output); // X velocity
        var outputNode2 = new NEAT.Genes.NodeGene(18, NEAT.Genes.NodeType.Output); // Y velocity
        var outputNode3 = new NEAT.Genes.NodeGene(19, NEAT.Genes.NodeType.Output); // Chop action
        var outputNode4 = new NEAT.Genes.NodeGene(20, NEAT.Genes.NodeType.Output); // Attack action
        var outputNode5 = new NEAT.Genes.NodeGene(21, NEAT.Genes.NodeType.Output); // Reproduction action

        outputNode1.Layer = 2;  // Output layer
        outputNode2.Layer = 2;  // Output layer
        outputNode3.Layer = 2;  // Output layer
        outputNode4.Layer = 2;  // Output layer
        outputNode5.Layer = 2;  // Output layer

        // Explicitly set bias to 0 for output nodes to maintain previous behavior
        outputNode1.Bias = 0.0;
        outputNode2.Bias = 0.0;
        outputNode3.Bias = 0.0;
        outputNode4.Bias = 0.0;
        outputNode5.Bias = 0.0;

        genome.AddNode(outputNode1);
        genome.AddNode(outputNode2);
        genome.AddNode(outputNode3);
        genome.AddNode(outputNode4);
        genome.AddNode(outputNode5);

        for(int i = 0; i < 13; i++)
        {
            for (int j = 17; j <  22; j++)
            {
                genome.AddConnection(new NEAT.Genes.ConnectionGene((i*5 + j + 22),i, j, Random.Range(-1f, 1f)));
            }
        }
        
        return genome;
    }
    
    NEAT.Genome.Genome CreateInitialKaiGenome()
    {
        var genome = new NEAT.Genome.Genome(0);
        
        // Add input nodes (13 inputs now, not 17)
        for (int i = 0; i < 13; i++)
        {
            var node = new NEAT.Genes.NodeGene(i, NEAT.Genes.NodeType.Input);
            node.Layer = 0;
            node.Bias = 0.0; // Explicitly set bias to 0 for input nodes
            genome.AddNode(node);
        }
        
        // Add output nodes (5 outputs: x,y velocity, chop, attack, reproduce)
        var outputNode1 = new NEAT.Genes.NodeGene(17, NEAT.Genes.NodeType.Output); // X velocity
        var outputNode2 = new NEAT.Genes.NodeGene(18, NEAT.Genes.NodeType.Output); // Y velocity
        var outputNode3 = new NEAT.Genes.NodeGene(19, NEAT.Genes.NodeType.Output); // Chop action
        var outputNode4 = new NEAT.Genes.NodeGene(20, NEAT.Genes.NodeType.Output); // Attack action
        var outputNode5 = new NEAT.Genes.NodeGene(21, NEAT.Genes.NodeType.Output); // Reproduction action
        
        outputNode1.Layer = 2;
        outputNode2.Layer = 2;
        outputNode3.Layer = 2;
        outputNode4.Layer = 2;
        outputNode5.Layer = 2;
        
        // Explicitly set bias to 0 for output nodes to maintain previous behavior
        outputNode1.Bias = 0.0;
        outputNode2.Bias = 0.0;
        outputNode3.Bias = 0.0;
        outputNode4.Bias = 0.0;
        outputNode5.Bias = 0.0;
        
        genome.AddNode(outputNode1);
        genome.AddNode(outputNode2);
        genome.AddNode(outputNode3);
        genome.AddNode(outputNode4);
        genome.AddNode(outputNode5);
        
        // Add connections with different weights than Albert
        // Kais are more aggressive (stronger response to opposite type)
        // and less focused on reproduction
        
        // Health to horizontal velocity (more defensive)
        genome.AddConnection(new NEAT.Genes.ConnectionGene(0, 0, 17, -0.7f));
        // Reproduction to vertical velocity (less priority)
        genome.AddConnection(new NEAT.Genes.ConnectionGene(1, 1, 18, -0.3f));
        // Same type x,y position (slightly weaker attraction)
        genome.AddConnection(new NEAT.Genes.ConnectionGene(2, 3, 17, 0.4f));
        genome.AddConnection(new NEAT.Genes.ConnectionGene(3, 4, 18, 0.4f));
        // Opposite type x,y position (stronger reaction)
        genome.AddConnection(new NEAT.Genes.ConnectionGene(4, 6, 17, 0.8f));
        genome.AddConnection(new NEAT.Genes.ConnectionGene(5, 7, 18, 0.8f));
        // Cherry position (more food-focused)
        genome.AddConnection(new NEAT.Genes.ConnectionGene(6, 9, 17, 0.6f));
        genome.AddConnection(new NEAT.Genes.ConnectionGene(7, 10, 18, 0.6f));
        // Current direction (more momentum)
        genome.AddConnection(new NEAT.Genes.ConnectionGene(8, 11, 17, 0.4f));
        genome.AddConnection(new NEAT.Genes.ConnectionGene(9, 12, 18, 0.4f));
        
        // Add base connections for chop action - connect to tree observations
        // Kai is more aggressive about chopping
        genome.AddConnection(new NEAT.Genes.ConnectionGene(10, 9, 19, 0.8f)); // Tree x to chop
        genome.AddConnection(new NEAT.Genes.ConnectionGene(11, 10, 19, 0.8f)); // Tree y to chop
        
        // Add base connections for attack action - connect to opposite type observations
        // Kai is very aggressive about attacking
        genome.AddConnection(new NEAT.Genes.ConnectionGene(13, 5, 20, 0.9f)); // Opposite x to attack
        genome.AddConnection(new NEAT.Genes.ConnectionGene(14, 6, 20, 0.9f)); // Opposite y to attack
        
        // Energy level to actions - enables actions only when energy is high
        genome.AddConnection(new NEAT.Genes.ConnectionGene(16, 2, 19, 0.6f)); // Energy to chop
        genome.AddConnection(new NEAT.Genes.ConnectionGene(17, 2, 20, 0.7f)); // Energy to attack - higher weight makes Kai more likely to attack
        
        // Add connections for reproduction - Kai is less focused on reproduction than Albert
        genome.AddConnection(new NEAT.Genes.ConnectionGene(18, 1, 21, 0.5f));  // Reproduction readiness to reproduce action
        genome.AddConnection(new NEAT.Genes.ConnectionGene(19, 2, 21, 0.4f));  // Energy to reproduce - lower weight than attack/chop
        genome.AddConnection(new NEAT.Genes.ConnectionGene(20, 3, 21, 0.3f));  // Same type x position to reproduce
        genome.AddConnection(new NEAT.Genes.ConnectionGene(21, 4, 21, 0.3f));  // Same type y position to reproduce
        
        return genome;
    }

    // New method to spawn creatures with more randomized brains
    private Creature SpawnCreatureWithRandomizedBrain(GameObject prefab, Vector3 position, Creature.CreatureType type)
    {
        // Create the creature instance
        var creature = Instantiate(prefab, position, Quaternion.identity);
        var creatureComponent = creature.GetComponent<Creature>();
        creatureComponent.type = type;
        
        // Create a base genome
        var genome = type == Creature.CreatureType.Kai ? CreateInitialKaiGenome() : CreateInitialGenome();
        
        // Randomize the weights to create more diverse behaviors
        foreach (var connection in genome.Connections.Values)
        {
            // Assign a completely random weight to each connection
            connection.Weight = Random.Range(-1f, 1f);
        }
        
        // Add some random additional connections to create more diverse topologies
        int extraConnectionsCount = Random.Range(0, 5);
        for (int i = 0; i < extraConnectionsCount; i++)
        {
            creatureComponent.addConnectionRate = 1.0f; // Ensure connections are added
            ApplyRandomConnectionMutation(genome, creatureComponent.maxHiddenLayers);
        }
        
        // Create the neural network from the randomized genome
        var network = NEAT.NN.FeedForwardNetwork.Create(genome);
        creatureComponent.InitializeNetwork(network);
        
        // Pass the max hidden layers setting to the creature
        creatureComponent.maxHiddenLayers = maxHiddenLayers;
        
        return creatureComponent;
    }
    
    private void ApplyRandomConnectionMutation(NEAT.Genome.Genome genome, int maxHiddenLayers)
    {
        // Try a few times to find a valid connection
        for (int tries = 0; tries < 5; tries++)
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
    
    private Creature SpawnCreatureWithReproductionBias(GameObject prefab, Vector2 position, Creature.CreatureType type, float reproBias)
    {
        // Create the creature instance
        var creature = Instantiate(prefab, position, Quaternion.identity);
        var creatureComponent = creature.GetComponent<Creature>();
        creatureComponent.type = type;
        
        // Create a custom genome with clear reproduction bias
        var genome = new NEAT.Genome.Genome(0);
        
        // Add input nodes (13 inputs, not 17)
        for (int i = 0; i < 13; i++)
        {
            var node = new NEAT.Genes.NodeGene(i, NEAT.Genes.NodeType.Input);
            node.Layer = 0;
            node.Bias = 0.0;
            genome.AddNode(node);
        }
        
        // Add output nodes
        var outputNode1 = new NEAT.Genes.NodeGene(17, NEAT.Genes.NodeType.Output); // X velocity
        var outputNode2 = new NEAT.Genes.NodeGene(18, NEAT.Genes.NodeType.Output); // Y velocity
        var outputNode3 = new NEAT.Genes.NodeGene(19, NEAT.Genes.NodeType.Output); // Chop action
        var outputNode4 = new NEAT.Genes.NodeGene(20, NEAT.Genes.NodeType.Output); // Attack action
        var outputNode5 = new NEAT.Genes.NodeGene(21, NEAT.Genes.NodeType.Output); // Reproduction action
        
        outputNode1.Layer = 2;
        outputNode2.Layer = 2;
        outputNode3.Layer = 2;
        outputNode4.Layer = 2;
        outputNode5.Layer = 2;
        
        outputNode1.Bias = 0.0;
        outputNode2.Bias = 0.0;
        outputNode3.Bias = 0.0;
        outputNode4.Bias = 0.0;
        outputNode5.Bias = 0.0;
        
        genome.AddNode(outputNode1);
        genome.AddNode(outputNode2);
        genome.AddNode(outputNode3);
        genome.AddNode(outputNode4);
        genome.AddNode(outputNode5);
        
        // Add connections for basic movement (low weights)
        genome.AddConnection(new NEAT.Genes.ConnectionGene(0, 0, 17, 0.2f)); // Health to x movement
        genome.AddConnection(new NEAT.Genes.ConnectionGene(1, 0, 18, 0.2f)); // Health to y movement
        
        // Add connections for basic actions (medium weights)
        genome.AddConnection(new NEAT.Genes.ConnectionGene(2, 2, 19, 0.5f)); // Energy to chop
        genome.AddConnection(new NEAT.Genes.ConnectionGene(3, 2, 20, 0.5f)); // Energy to attack
        
        // Add connections for reproduction - with bias parameter
        genome.AddConnection(new NEAT.Genes.ConnectionGene(4, 1, 21, reproBias));  // Reproduction readiness to reproduction action
        genome.AddConnection(new NEAT.Genes.ConnectionGene(5, 2, 21, reproBias));  // Energy to reproduction action
        
        // Add connections to make creatures see each other
        genome.AddConnection(new NEAT.Genes.ConnectionGene(6, 3, 17, 0.4f));  // Same creature x to x movement
        genome.AddConnection(new NEAT.Genes.ConnectionGene(7, 4, 18, 0.4f));  // Same creature y to y movement
        
        // Create the neural network and initialize the creature
        var network = NEAT.NN.FeedForwardNetwork.Create(genome);
        creatureComponent.InitializeNetwork(network);
        
        // Pass the max hidden layers setting to the creature
        creatureComponent.maxHiddenLayers = maxHiddenLayers;
        
        return creatureComponent;
    }
} 