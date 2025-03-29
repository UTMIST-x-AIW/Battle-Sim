using UnityEngine;

public class NEATTest : MonoBehaviour
{
    private static NEATTest instance;

    [Header("Creature Prefabs")]
    public GameObject albertCreaturePrefab;  // Assign in inspector
    public GameObject kaiCreaturePrefab;    // Assign in inspector
    
    [Header("Network Settings")]
    public int maxHiddenLayers = 10;  // Maximum number of hidden layers allowed

    [Header("Test Settings")]
    public bool runTests = true;
    public int currentTest = 1;

    [Header("Visualization Settings")]
    public bool showDetectionRadius = false;  // Toggle for detection radius visualization
    
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
                case 1:
                    SetupMatingMovementTest();
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
        
        // Initialize creatures with their starting values
        olderCreature.InitializeForTesting(20f, olderCreature.maxReproduction);
        youngerCreature.InitializeForTesting(10f, youngerCreature.maxReproduction);

        // Debug.Log("Test 1 Setup Complete:");
        // Debug.Log($"- Older creature at {olderPosition}, age: 20");
        // Debug.Log($"- Younger creature at {youngerPosition}, age: 10");
        // Debug.Log("Expected behavior: Younger creature should move toward older creature, older creature should stay still");
        // Debug.Log("Note: Reproduction will be enabled after a 2-second delay");
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
        
        // Add input nodes (17 inputs total): 
        // 0: health
        // 1: reproduction
        // 2: energy
        // 3,4: same type x,y
        // 5: same type absolute sum
        // 6,7: opposite type x,y
        // 8: opposite type absolute sum
        // 9,10: cherry x,y
        // 11: cherry absolute sum
        // 12,13: tree x,y
        // 14: tree absolute sum
        // 15,16: current direction x,y
        for (int i = 0; i < 17; i++)
        {
            var node = new NEAT.Genes.NodeGene(i, NEAT.Genes.NodeType.Input);
            node.Layer = 0;  // Input layer
            node.Bias = 0.0; // Explicitly set bias to 0 for input nodes
            genome.AddNode(node);
        }
        
        // Add output nodes (4 outputs now: x,y velocity, chop, attack)
        var outputNode1 = new NEAT.Genes.NodeGene(17, NEAT.Genes.NodeType.Output); // X velocity
        var outputNode2 = new NEAT.Genes.NodeGene(18, NEAT.Genes.NodeType.Output); // Y velocity
        var outputNode3 = new NEAT.Genes.NodeGene(19, NEAT.Genes.NodeType.Output); // Chop action
        var outputNode4 = new NEAT.Genes.NodeGene(20, NEAT.Genes.NodeType.Output); // Attack action
        
        outputNode1.Layer = 2;  // Output layer
        outputNode2.Layer = 2;  // Output layer
        outputNode3.Layer = 2;  // Output layer
        outputNode4.Layer = 2;  // Output layer
        
        // Explicitly set bias to 0 for output nodes to maintain previous behavior
        outputNode1.Bias = 0.0;
        outputNode2.Bias = 0.0;
        outputNode3.Bias = 0.0;
        outputNode4.Bias = 0.0;
        
        genome.AddNode(outputNode1);
        genome.AddNode(outputNode2);
        genome.AddNode(outputNode3);
        genome.AddNode(outputNode4);
        
        // Add connections with fixed weights
        // Health to horizontal velocity
        genome.AddConnection(new NEAT.Genes.ConnectionGene(0, 0, 17, -0.5f));
        // Reproduction to vertical velocity
        genome.AddConnection(new NEAT.Genes.ConnectionGene(1, 1, 18, -0.5f));
        // Same type x position to horizontal velocity
        genome.AddConnection(new NEAT.Genes.ConnectionGene(2, 3, 17, 0.5f));
        // Same type y position to vertical velocity
        genome.AddConnection(new NEAT.Genes.ConnectionGene(3, 4, 18, 0.5f));
        // Cherry x position to horizontal velocity
        genome.AddConnection(new NEAT.Genes.ConnectionGene(4, 9, 17, 0.5f));
        // Cherry y position to vertical velocity
        genome.AddConnection(new NEAT.Genes.ConnectionGene(5, 10, 18, 0.5f));
        // Current direction x to horizontal velocity
        genome.AddConnection(new NEAT.Genes.ConnectionGene(6, 15, 17, 0.3f));
        // Current direction y to vertical velocity
        genome.AddConnection(new NEAT.Genes.ConnectionGene(7, 16, 18, 0.3f));
        
        // Add base connections for chop action - connect to tree observations
        genome.AddConnection(new NEAT.Genes.ConnectionGene(8, 12, 19, 0.7f)); // Tree x to chop
        genome.AddConnection(new NEAT.Genes.ConnectionGene(9, 13, 19, 0.7f)); // Tree y to chop
        genome.AddConnection(new NEAT.Genes.ConnectionGene(10, 14, 19, -0.5f)); // Tree distance to chop (negative weight - closer trees more likely to chop)
        
        // Add base connections for attack action - connect to opposite type observations
        genome.AddConnection(new NEAT.Genes.ConnectionGene(11, 6, 20, 0.7f)); // Opposite x to attack
        genome.AddConnection(new NEAT.Genes.ConnectionGene(12, 7, 20, 0.7f)); // Opposite y to attack
        genome.AddConnection(new NEAT.Genes.ConnectionGene(13, 8, 20, -0.5f)); // Opposite distance to attack (negative weight - closer creatures more likely to attack)
        
        // Energy level to actions - enables actions only when energy is high
        genome.AddConnection(new NEAT.Genes.ConnectionGene(14, 2, 19, 0.5f)); // Energy to chop
        genome.AddConnection(new NEAT.Genes.ConnectionGene(15, 2, 20, 0.5f)); // Energy to attack
        
        return genome;
    }
    
    NEAT.Genome.Genome CreateInitialKaiGenome()
    {
        var genome = new NEAT.Genome.Genome(0);
        
        // Add input nodes (same as Albert, 17 inputs now)
        for (int i = 0; i < 17; i++)
        {
            var node = new NEAT.Genes.NodeGene(i, NEAT.Genes.NodeType.Input);
            node.Layer = 0;
            node.Bias = 0.0; // Explicitly set bias to 0 for input nodes
            genome.AddNode(node);
        }
        
        // Add output nodes (4 outputs now: x,y velocity, chop, attack)
        var outputNode1 = new NEAT.Genes.NodeGene(17, NEAT.Genes.NodeType.Output); // X velocity
        var outputNode2 = new NEAT.Genes.NodeGene(18, NEAT.Genes.NodeType.Output); // Y velocity
        var outputNode3 = new NEAT.Genes.NodeGene(19, NEAT.Genes.NodeType.Output); // Chop action
        var outputNode4 = new NEAT.Genes.NodeGene(20, NEAT.Genes.NodeType.Output); // Attack action
        
        outputNode1.Layer = 2;
        outputNode2.Layer = 2;
        outputNode3.Layer = 2;
        outputNode4.Layer = 2;
        
        // Explicitly set bias to 0 for output nodes to maintain previous behavior
        outputNode1.Bias = 0.0;
        outputNode2.Bias = 0.0;
        outputNode3.Bias = 0.0;
        outputNode4.Bias = 0.0;
        
        genome.AddNode(outputNode1);
        genome.AddNode(outputNode2);
        genome.AddNode(outputNode3);
        genome.AddNode(outputNode4);
        
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
        genome.AddConnection(new NEAT.Genes.ConnectionGene(8, 15, 17, 0.4f));
        genome.AddConnection(new NEAT.Genes.ConnectionGene(9, 16, 18, 0.4f));
        
        // Add base connections for chop action - connect to tree observations
        // Kai is more aggressive about chopping
        genome.AddConnection(new NEAT.Genes.ConnectionGene(10, 12, 19, 0.8f)); // Tree x to chop
        genome.AddConnection(new NEAT.Genes.ConnectionGene(11, 13, 19, 0.8f)); // Tree y to chop
        genome.AddConnection(new NEAT.Genes.ConnectionGene(12, 14, 19, -0.6f)); // Tree distance to chop (negative weight - closer trees more likely to chop)
        
        // Add base connections for attack action - connect to opposite type observations
        // Kai is very aggressive about attacking
        genome.AddConnection(new NEAT.Genes.ConnectionGene(13, 6, 20, 0.9f)); // Opposite x to attack
        genome.AddConnection(new NEAT.Genes.ConnectionGene(14, 7, 20, 0.9f)); // Opposite y to attack
        genome.AddConnection(new NEAT.Genes.ConnectionGene(15, 8, 20, -0.7f)); // Opposite distance to attack (negative weight - closer creatures more likely to attack)
        
        // Energy level to actions - enables actions only when energy is high
        genome.AddConnection(new NEAT.Genes.ConnectionGene(16, 2, 19, 0.6f)); // Energy to chop
        genome.AddConnection(new NEAT.Genes.ConnectionGene(17, 2, 20, 0.7f)); // Energy to attack - higher weight makes Kai more likely to attack
        
        return genome;
    }
} 