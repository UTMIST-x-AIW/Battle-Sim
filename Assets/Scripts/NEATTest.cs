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

        Debug.Log($"NEATTest starting on GameObject: {gameObject.name}");

        // Clear any existing creatures first
        var existingCreatures = GameObject.FindObjectsOfType<Creature>();
        Debug.Log($"Found {existingCreatures.Length} existing creatures to clean up");
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
        Debug.Log("Setting up normal game");
        
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
        Debug.Log("Starting Test 1: Basic Mating Movement");
        
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

        Debug.Log("Test 1 Setup Complete:");
        Debug.Log($"- Older creature at {olderPosition}, age: 20");
        Debug.Log($"- Younger creature at {youngerPosition}, age: 10");
        Debug.Log("Expected behavior: Younger creature should move toward older creature, older creature should stay still");
        Debug.Log("Note: Reproduction will be enabled after a 2-second delay");
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
        
        // Add input nodes (13 inputs):
        // 0: health
        // 1: reproduction
        // 2,3: same type x,y
        // 4: same type absolute sum
        // 5,6: opposite type x,y
        // 7: opposite type absolute sum
        // 8,9: cherry x,y
        // 10: cherry absolute sum
        // 11,12: current direction x,y
        for (int i = 0; i < 13; i++)
        {
            var node = new NEAT.Genes.NodeGene(i, NEAT.Genes.NodeType.Input);
            node.Layer = 0;  // Input layer
            genome.AddNode(node);
        }
        
        // Add output nodes (x,y velocity)
        var outputNode1 = new NEAT.Genes.NodeGene(13, NEAT.Genes.NodeType.Output);
        var outputNode2 = new NEAT.Genes.NodeGene(14, NEAT.Genes.NodeType.Output);
        outputNode1.Layer = 2;  // Output layer
        outputNode2.Layer = 2;  // Output layer
        genome.AddNode(outputNode1);
        genome.AddNode(outputNode2);
        
        // Add some basic connections with fixed weights
        // Only connect from input layer (0) to output layer (2)
        // Health to horizontal velocity
        genome.AddConnection(new NEAT.Genes.ConnectionGene(0, 0, 13, -0.5f));
        // Reproduction to vertical velocity
        genome.AddConnection(new NEAT.Genes.ConnectionGene(1, 1, 14, -0.5f));
        // Same type x position to horizontal velocity
        genome.AddConnection(new NEAT.Genes.ConnectionGene(2, 2, 13, 0.5f));
        // Same type y position to vertical velocity
        genome.AddConnection(new NEAT.Genes.ConnectionGene(3, 3, 14, 0.5f));
        // Cherry x position to horizontal velocity
        genome.AddConnection(new NEAT.Genes.ConnectionGene(4, 8, 13, 0.5f));
        // Cherry y position to vertical velocity
        genome.AddConnection(new NEAT.Genes.ConnectionGene(5, 9, 14, 0.5f));
        // Current direction x to horizontal velocity
        genome.AddConnection(new NEAT.Genes.ConnectionGene(6, 11, 13, 0.3f));
        // Current direction y to vertical velocity
        genome.AddConnection(new NEAT.Genes.ConnectionGene(7, 12, 14, 0.3f));
        
        return genome;
    }
    
    NEAT.Genome.Genome CreateInitialKaiGenome()
    {
        var genome = new NEAT.Genome.Genome(0);
        
        // Add input nodes (same as Albert)
        for (int i = 0; i < 13; i++)
        {
            var node = new NEAT.Genes.NodeGene(i, NEAT.Genes.NodeType.Input);
            node.Layer = 0;
            genome.AddNode(node);
        }
        
        // Add output nodes
        var outputNode1 = new NEAT.Genes.NodeGene(13, NEAT.Genes.NodeType.Output);
        var outputNode2 = new NEAT.Genes.NodeGene(14, NEAT.Genes.NodeType.Output);
        outputNode1.Layer = 2;
        outputNode2.Layer = 2;
        genome.AddNode(outputNode1);
        genome.AddNode(outputNode2);
        
        // Add connections with different weights than Albert
        // Kais are more aggressive (stronger response to opposite type)
        // and less focused on reproduction
        
        // Health to horizontal velocity (more defensive)
        genome.AddConnection(new NEAT.Genes.ConnectionGene(0, 0, 13, -0.7f));
        // Reproduction to vertical velocity (less priority)
        genome.AddConnection(new NEAT.Genes.ConnectionGene(1, 1, 14, -0.3f));
        // Same type x,y position (slightly weaker attraction)
        genome.AddConnection(new NEAT.Genes.ConnectionGene(2, 2, 13, 0.4f));
        genome.AddConnection(new NEAT.Genes.ConnectionGene(3, 3, 14, 0.4f));
        // Opposite type x,y position (stronger reaction)
        genome.AddConnection(new NEAT.Genes.ConnectionGene(4, 5, 13, 0.8f));
        genome.AddConnection(new NEAT.Genes.ConnectionGene(5, 6, 14, 0.8f));
        // Cherry position (more food-focused)
        genome.AddConnection(new NEAT.Genes.ConnectionGene(6, 8, 13, 0.6f));
        genome.AddConnection(new NEAT.Genes.ConnectionGene(7, 9, 14, 0.6f));
        // Current direction (more momentum)
        genome.AddConnection(new NEAT.Genes.ConnectionGene(8, 11, 13, 0.4f));
        genome.AddConnection(new NEAT.Genes.ConnectionGene(9, 12, 14, 0.4f));
        
        return genome;
    }
} 