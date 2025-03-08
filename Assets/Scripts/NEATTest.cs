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
        
        // Original game setup
        // Spawn one Albert near (-5, 5)
        Vector3 albertPosition = new Vector3(-12, 6, 0) + Random.insideUnitSphere * 2f;
        albertPosition.z = 0;  // Ensure z is 0
        SpawnCreature(albertCreaturePrefab, albertPosition, Creature.CreatureType.Albert);
        
        // Spawn one Kai near (5, -5)
        Vector3 kaiPosition = new Vector3(12, -6, 0) + Random.insideUnitSphere * 2f;
        kaiPosition.z = 0;  // Ensure z is 0
        SpawnCreature(kaiCreaturePrefab, kaiPosition, Creature.CreatureType.Kai);
    }

    private void SetupMatingMovementTest()
    {
        Debug.Log("Starting Test 1: Basic Mating Movement");
        
        // Create two Alberts close to each other but not too close
        Vector3 olderPosition = new Vector3(0, 0, 0);
        Vector3 youngerPosition = new Vector3(3, 0, 0); // 3 units away, within detection radius
        
        // Spawn older creature
        var olderCreature = SpawnCreature(albertCreaturePrefab, olderPosition, Creature.CreatureType.Albert);
        
        // Spawn younger creature
        var youngerCreature = SpawnCreature(albertCreaturePrefab, youngerPosition, Creature.CreatureType.Albert);
        
        // Set their reproduction meters to maximum
        olderCreature.reproduction = olderCreature.maxReproduction;
        youngerCreature.reproduction = youngerCreature.maxReproduction;
        
        // Set different ages (using lifetime)
        // We'll use reflection since lifetime is private
        var lifetimeField = typeof(Creature).GetField("lifetime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (lifetimeField != null)
        {
            lifetimeField.SetValue(olderCreature, 20f); // 20 seconds old
            lifetimeField.SetValue(youngerCreature, 10f); // 10 seconds old
        }

        Debug.Log("Test 1 Setup Complete:");
        Debug.Log($"- Older creature at {olderPosition}, age: 20");
        Debug.Log($"- Younger creature at {youngerPosition}, age: 10");
        Debug.Log("Expected behavior: Younger creature should move toward older creature, older creature should stay still");
    }
    
    private Creature SpawnCreature(GameObject prefab, Vector3 position, Creature.CreatureType type)
    {
        var creature = Instantiate(prefab, position, Quaternion.identity);
        var creatureComponent = creature.GetComponent<Creature>();
        creatureComponent.type = type;
        
        // Create initial neural network
        var genome = CreateInitialGenome();
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
} 