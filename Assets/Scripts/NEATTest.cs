using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class NEATTest : MonoBehaviour
{
    private static NEATTest instance;

    [Header("Creature Prefabs")]
    public GameObject albertCreaturePrefab;  // Assign in inspector
    public GameObject kaiCreaturePrefab;    // Assign in inspector
    
    [Header("Population Settings")]
    public int MIN_ALBERTS = 20;  // Minimum number of Alberts to maintain
    public int MAX_ALBERTS = 100; // Maximum number of Alberts allowed
    
    // Property to display current Albert count in Inspector
    [SerializeField]
    private int _current_alberts = 0;
    public int CurrentAlberts
    {
        get { return _current_alberts; }
        private set { _current_alberts = value; }
    }

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
    public bool showChopRange = false;  // Toggle for chop range visualization
    public bool showGizmos = false;     // Master toggle for all gizmos
    public Color chopRangeColor = new Color(0.5f, 0, 0.5f, 0.2f);  // Semi-transparent purple
    public bool showCreatureLabels = true;  // Toggle for creature labels
    public bool showSpawnArea = false;  // Toggle for spawn area visualization
    public Color spawnAreaColor = new Color(0.2f, 0.8f, 0.2f, 0.2f);  // Semi-transparent green
    
    [Header("Spawn Area Settings")]
    public Vector2 spawnCenter = new Vector2(-10f, -0f);  // Center of the spawn area
    public float spawnSpreadRadius = 2f;  // Radius of the spawn area
    
    // NEAT instance for access by other classes
    [System.NonSerialized]
    public NEAT.Genome.Genome neat = new NEAT.Genome.Genome(0);

    [Header("Population Settings")]
    private float lastPopulationCheck = 0f;
    private float populationCheckInterval = 0.1f;  // Check population every 0.1 seconds
    private float lastSpawnTime = 0f;
    private float spawnCooldown = 1.0f;  // Minimum time between spawns
    private bool isSpawning = false;  // Flag to prevent multiple spawn coroutines

    private float countTimer = 0f;

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
        try
        {
            // Check for population management (only for Tests 1 and 4 to not interfere with other tests)
            if (currentTest == TEST_MATING_MOVEMENT || currentTest == TEST_ALBERTS_ONLY)
            {
                // Check current Albert count every second
                countTimer += Time.deltaTime;
                if (countTimer >= 0.4f)
                {
                    CountAlberts();
                    countTimer = 0f;
                }
                
                // Automatic population management
                ManagePopulation();
            }
            
            // Testing controls
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                currentTest = TEST_MATING_MOVEMENT;
                RestartTest();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                currentTest = TEST_ALBERTS_ONLY;
                RestartTest();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                currentTest = TEST_REPRODUCTION;
                RestartTest();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                currentTest = TEST_NORMAL_GAME;
                RestartTest();
            }
            
            // Toggle creature labels display
            if (Input.GetKeyDown(KeyCode.L))
            {
                LogManager.LogMessage($"Toggle labels: {!showCreatureLabels} -> {showCreatureLabels}");
                showCreatureLabels = !showCreatureLabels;
            }
            
            // Toggle gizmos display
            if (Input.GetKeyDown(KeyCode.G))
            {
                LogManager.LogMessage($"Toggle gizmos: {!showGizmos} -> {showGizmos}");
                showGizmos = !showGizmos;
            }

            // Add this to the Update method, after the other key checks
            if (Input.GetKeyDown(KeyCode.D))
            {
                LogManager.LogMessage("Running neural network diagnostics...");
                RunNeuralNetworkDiagnostics();
            }
        }
        catch (System.Exception e)
        {
            if (LogManager.Instance != null)
            {
                LogManager.LogError($"CRITICAL ERROR in NEATTest.Update: {e.Message}\nStack trace: {e.StackTrace}");
            }
            else
            {
                Debug.LogError($"CRITICAL ERROR in NEATTest.Update: {e.Message}\nStack trace: {e.StackTrace}");
            }
        }
    }

    private void ManagePopulation()
    {
        try
        {
            // Count current Alberts
            int currentAlberts = CountAlberts();
            
            // Update the inspector-visible count
            CurrentAlberts = currentAlberts;
            
            // Log population count
            LogManager.LogMessage($"Current Albert population: {currentAlberts}");
            
            // Check if we need to spawn more Alberts and if enough time has passed since last spawn
            if (currentAlberts < MIN_ALBERTS && Time.time - lastSpawnTime >= spawnCooldown && !isSpawning)
            {
                LogManager.LogMessage($"Population below minimum ({MIN_ALBERTS}). Spawning new Albert with staggered timing.");
                
                // Start a coroutine to spawn a new Albert with a random delay
                StartCoroutine(SpawnNewAlbertStaggered());
            }
        }
        catch (System.Exception e)
        {
            LogManager.LogError($"Error in ManagePopulation: {e.Message}\nStack trace: {e.StackTrace}");
        }
    }

    private IEnumerator SpawnNewAlbertStaggered()
    {
        isSpawning = true;
        lastSpawnTime = Time.time;
        
        // Add a random delay between 0.5 and 1.5 seconds before spawning
        yield return new WaitForSeconds(Random.Range(0.5f, 1.5f));
        
        try
        {
            // Calculate a position with some randomness within the spawn area
            Vector2 offset = Random.insideUnitCircle * spawnSpreadRadius;
            Vector3 position = new Vector3(
                spawnCenter.x + offset.x,
                spawnCenter.y + offset.y,
                0f
            );
            
            LogManager.LogMessage($"Spawning new Albert at position: {position}");
            
            // Spawn the creature with a randomized brain
            var creature = SpawnCreatureWithRandomizedBrain(albertCreaturePrefab, position, Creature.CreatureType.Albert);
            
            if (creature == null)
            {
                LogManager.LogError("Failed to spawn new Albert - SpawnCreatureWithRandomizedBrain returned null");
                isSpawning = false;
                yield break;
            }
            
            // Initialize with random age and reproduction
            float startingAge = Random.Range(0f, 10f);  // Reduced max age to 10 seconds
            creature.Lifetime = startingAge;  // Set the lifetime using the public property
            
            // If the creature starts with an age past the aging threshold, give it appropriate health
            if (startingAge > creature.agingStartTime)
            {
                float ageBeyondThreshold = startingAge - creature.agingStartTime;
                float healthLost = ageBeyondThreshold * creature.agingRate;
                creature.health = Mathf.Max(0.1f, creature.maxHealth - healthLost);  // Ensure at least 0.1 health
            }
            
            // Set starting reproduction meter to a random value between 0 and 1
            float startingReproductionMeter = Random.Range(0f, 1f);
            creature.reproductionMeter = startingReproductionMeter;
            
            // Set generation to 0 for initially spawned Alberts
            creature.generation = 0;
            
            LogManager.LogMessage($"Successfully spawned new Albert with age: {startingAge}, reproduction meter: {startingReproductionMeter}");
        }
        catch (System.Exception e)
        {
            LogManager.LogError($"Error in SpawnNewAlbertStaggered: {e.Message}\nStack trace: {e.StackTrace}");
        }
        finally
        {
            isSpawning = false;
        }
    }

    // Modify Reproduction class to check MAX_ALBERTS before allowing reproduction
    public bool CanReproduce()
    {
        return CurrentAlberts < MAX_ALBERTS;
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
        Debug.Log("Starting Test: Alberts Only - Population will be managed automatically");
        
        // No need to spawn initial creatures - the population management system will handle it
        // The system will detect that we're below MIN_ALBERTS and start spawning creatures
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
        highRepro1.energyMeter = highRepro1.maxEnergy;
        highRepro2.energyMeter = highRepro2.maxEnergy;
        lowRepro1.energyMeter = lowRepro1.maxEnergy;
        lowRepro2.energyMeter = lowRepro2.maxEnergy;
        
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
        
        // Add input nodes (11 inputs total): 
        // 0: health
        // 1: energyMeter
        // 2: reproductionMeter
        // 3,4: same type x,y
        // 5,6: opposite type x,y
        // 7,8: cherry x,y
        // 9,10: tree x,y
        for (int i = 0; i < 11; i++)
        {
            var node = new NEAT.Genes.NodeGene(i, NEAT.Genes.NodeType.Input);
            node.Layer = 0;  // Input layer
            node.Bias = 0.0; // Explicitly set bias to 0 for input nodes
            genome.AddNode(node);
        }
        
        // Add output nodes (4 outputs: x,y velocity, chop, attack)
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

        for(int i = 0; i < 11; i++)
        {
            for (int j = 17; j < 21; j++)
            {
                genome.AddConnection(new NEAT.Genes.ConnectionGene((i*4 + j + 22),i, j, Random.Range(-1f, 1f)));
            }
        }
        
        return genome;
    }
    
    NEAT.Genome.Genome CreateInitialKaiGenome()
    {
        var genome = new NEAT.Genome.Genome(0);
        
        // Add input nodes (11 inputs total): 
        // 0: health
        // 1: energyMeter
        // 2: reproductionMeter
        // 3,4: same type x,y
        // 5,6: opposite type x,y
        // 7,8: cherry x,y
        // 9,10: tree x,y
        for (int i = 0; i < 11; i++)
        {
            var node = new NEAT.Genes.NodeGene(i, NEAT.Genes.NodeType.Input);
            node.Layer = 0;
            node.Bias = 0.0; // Explicitly set bias to 0 for input nodes
            genome.AddNode(node);
        }
        
        // Add output nodes (4 outputs: x,y velocity, chop, attack)
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
        
        // Health to horizontal velocity (more defensive)
        genome.AddConnection(new NEAT.Genes.ConnectionGene(0, 0, 17, -0.7f));
        // ReproductionMeter to vertical velocity (less priority)
        genome.AddConnection(new NEAT.Genes.ConnectionGene(1, 2, 18, -0.3f));
        // Same type x,y position (slightly weaker attraction)
        genome.AddConnection(new NEAT.Genes.ConnectionGene(2, 3, 17, 0.4f));
        genome.AddConnection(new NEAT.Genes.ConnectionGene(3, 4, 18, 0.4f));
        // Opposite type x,y position (stronger reaction)
        genome.AddConnection(new NEAT.Genes.ConnectionGene(4, 5, 17, 0.8f));
        genome.AddConnection(new NEAT.Genes.ConnectionGene(5, 6, 18, 0.8f));
        // Cherry position (more food-focused)
        genome.AddConnection(new NEAT.Genes.ConnectionGene(6, 7, 17, 0.6f));
        genome.AddConnection(new NEAT.Genes.ConnectionGene(7, 8, 18, 0.6f));
        
        // Add base connections for chop action - connect to tree observations
        // Kai is more aggressive about chopping
        genome.AddConnection(new NEAT.Genes.ConnectionGene(10, 9, 19, 0.8f)); // Tree x to chop
        genome.AddConnection(new NEAT.Genes.ConnectionGene(11, 10, 19, 0.8f)); // Tree y to chop
        
        // Add base connections for attack action - connect to opposite type observations
        // Kai is very aggressive about attacking
        genome.AddConnection(new NEAT.Genes.ConnectionGene(13, 5, 20, 0.9f)); // Opposite x to attack
        genome.AddConnection(new NEAT.Genes.ConnectionGene(14, 6, 20, 0.9f)); // Opposite y to attack
        
        // EnergyMeter level to actions - enables actions only when energy is high
        genome.AddConnection(new NEAT.Genes.ConnectionGene(16, 1, 19, 0.6f)); // EnergyMeter to chop
        genome.AddConnection(new NEAT.Genes.ConnectionGene(17, 1, 20, 0.7f)); // EnergyMeter to attack - higher weight makes Kai more likely to attack
        
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
        
        // Add input nodes (11 inputs total)
        for (int i = 0; i < 11; i++)
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
        
        outputNode1.Layer = 2;
        outputNode2.Layer = 2;
        outputNode3.Layer = 2;
        outputNode4.Layer = 2;
        
        outputNode1.Bias = 0.0;
        outputNode2.Bias = 0.0;
        outputNode3.Bias = 0.0;
        outputNode4.Bias = 0.0;
        
        genome.AddNode(outputNode1);
        genome.AddNode(outputNode2);
        genome.AddNode(outputNode3);
        genome.AddNode(outputNode4);
        
        // Add connections for basic movement (low weights)
        genome.AddConnection(new NEAT.Genes.ConnectionGene(0, 0, 17, 0.2f)); // Health to x movement
        genome.AddConnection(new NEAT.Genes.ConnectionGene(1, 0, 18, 0.2f)); // Health to y movement
        
        // Add connections for basic actions (medium weights)
        genome.AddConnection(new NEAT.Genes.ConnectionGene(2, 1, 19, 0.5f)); // EnergyMeter to chop
        genome.AddConnection(new NEAT.Genes.ConnectionGene(3, 1, 20, 0.5f)); // EnergyMeter to attack
        
        // Add influence from reproduction meter to movement (bias towards finding mates)
        genome.AddConnection(new NEAT.Genes.ConnectionGene(4, 2, 17, reproBias));  // ReproductionMeter to x movement
        genome.AddConnection(new NEAT.Genes.ConnectionGene(5, 2, 18, reproBias));  // ReproductionMeter to y movement
        
        // Add connections to make creatures see each other
        genome.AddConnection(new NEAT.Genes.ConnectionGene(6, 3, 17, 0.4f * reproBias));  // Same creature x to x movement (weighted by repro bias)
        genome.AddConnection(new NEAT.Genes.ConnectionGene(7, 4, 18, 0.4f * reproBias));  // Same creature y to y movement (weighted by repro bias)
        
        // Create the neural network and initialize the creature
        var network = NEAT.NN.FeedForwardNetwork.Create(genome);
        creatureComponent.InitializeNetwork(network);
        
        // Pass the max hidden layers setting to the creature
        creatureComponent.maxHiddenLayers = maxHiddenLayers;
        
        return creatureComponent;
    }

    private void OnDrawGizmos()
    {
        // Draw spawn area if enabled
        if (showSpawnArea)
        {
            // Set color for spawn area
            Gizmos.color = spawnAreaColor;
            
            // Draw the spawn area as a circle
            Gizmos.DrawSphere(new Vector3(spawnCenter.x, spawnCenter.y, 0), spawnSpreadRadius);
            
            // Draw a wire frame for better visibility
            Gizmos.color = new Color(spawnAreaColor.r, spawnAreaColor.g, spawnAreaColor.b, 0.5f);
            Gizmos.DrawWireSphere(new Vector3(spawnCenter.x, spawnCenter.y, 0), spawnSpreadRadius);
        }
    }

    private int CountAlberts()
    {
        try
        {
            // Find all creatures in the scene
            var creatures = GameObject.FindObjectsOfType<Creature>();
            
            // Count only Alberts
            int count = creatures.Count(c => c.type == Creature.CreatureType.Albert);
            
            LogManager.LogMessage($"Counted {count} Albert creatures in the scene");
            return count;
        }
        catch (System.Exception e)
        {
            LogManager.LogError($"Error in CountAlberts: {e.Message}\nStack trace: {e.StackTrace}");
            return 0;  // Return 0 if there's an error
        }
    }

    private void OnDestroy()
    {
        try
        {
            Debug.Log($"NEATTest OnDestroy called on {gameObject.name}");
            
            // Only clear the static instance if this is the instance being destroyed
            if (instance == this)
            {
                Debug.Log("NEATTest: Main instance being destroyed");
                
                try
                {
                    // Clear static references in Creature class
                    Creature.ClearStaticReferences();
                    Debug.Log("NEATTest: Successfully cleared Creature static references");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"NEATTest: Error clearing Creature static references: {e.Message}");
                }
                
                // Clear the instance reference
                instance = null;
                
                // Call LogManager cleanup to prevent the GameObject from lingering
                // Use direct reference to LogManager methods without creating new instances
                if (LogManager.Instance != null)
                {
                    try
                    {
                        LogManager.LogMessage("NEATTest instance has been destroyed and static references cleared.");
                        Debug.Log("NEATTest: Successfully logged final message");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"NEATTest: Error logging final message: {e.Message}");
                    }
                    
                    try
                    {
                        // Call cleanup separately
                        LogManager.Cleanup();
                        Debug.Log("NEATTest: LogManager cleanup completed");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"NEATTest: Error during LogManager cleanup: {e.Message}");
                    }
                }
                else
                {
                    Debug.Log("NEATTest: LogManager instance is null, skipping cleanup");
                }
            }
            else
            {
                Debug.Log("NEATTest: Not the main instance, skipping cleanup");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"NEATTest: Unhandled error in OnDestroy: {e.Message}\n{e.StackTrace}");
        }
    }

    private void RunNeuralNetworkDiagnostics()
    {
        try
        {
            var creatures = GameObject.FindObjectsOfType<Creature>();
            int total = creatures.Length;
            int withValidNetwork = 0;
            int withNoNetwork = 0;
            List<string> anomalies = new List<string>();
            
            LogManager.LogMessage($"Checking {total} creatures for neural network integrity...");
            
            foreach (var creature in creatures)
            {
                if (creature.brain == null)
                {
                    withNoNetwork++;
                    continue;
                }
                
                // Get and analyze neural network output
                try
                {
                    float[] observations = creature.observer.GetObservations(creature);
                    double[] doubleObservations = new double[observations.Length];
                    for (int i = 0; i < observations.Length; i++)
                    {
                        doubleObservations[i] = (double)observations[i];
                    }
                    
                    double[] doubleOutputs = creature.brain.Activate(doubleObservations);
                    
                    // Analyze outputs
                    if (doubleOutputs.Length != 4)
                    {
                        anomalies.Add($"Creature {creature.gameObject.name} (Gen {creature.generation}, Type {creature.type}): " +
                                     $"Neural network returned {doubleOutputs.Length} outputs instead of 4");
                    }
                    else
                    {
                        withValidNetwork++;
                    }
                }
                catch (System.Exception e)
                {
                    anomalies.Add($"Error processing creature {creature.gameObject.name}: {e.Message}");
                }
            }
            
            // Log summary
            LogManager.LogMessage($"Neural Network Diagnostics Complete");
            LogManager.LogMessage($"Total creatures: {total}");
            LogManager.LogMessage($"With valid networks (4 outputs): {withValidNetwork}");
            LogManager.LogMessage($"With no network: {withNoNetwork}");
            
            // Log anomalies
            if (anomalies.Count > 0)
            {
                LogManager.LogMessage($"Found {anomalies.Count} anomalies:");
                foreach (var anomaly in anomalies)
                {
                    LogManager.LogError(anomaly);
                }
            }
            else
            {
                LogManager.LogMessage("No anomalies found! All networks are outputting 4 values.");
            }
        }
        catch (System.Exception e)
        {
            LogManager.LogError($"Error running neural network diagnostics: {e.Message}\nStack trace: {e.StackTrace}");
        }
    }

    private void RestartTest()
    {
        try
        {
            LogManager.LogMessage($"Restarting test {currentTest}...");
            
            // Clear any existing creatures
            var existingCreatures = GameObject.FindObjectsOfType<Creature>();
            LogManager.LogMessage($"Destroying {existingCreatures.Length} existing creatures");
            
            foreach (var creature in existingCreatures)
            {
                Destroy(creature.gameObject);
            }
            
            // Wait a frame to ensure cleanup completes
            StartCoroutine(RestartAfterCleanup());
        }
        catch (System.Exception e)
        {
            LogManager.LogError($"Error in RestartTest: {e.Message}\nStack trace: {e.StackTrace}");
        }
    }

    private IEnumerator RestartAfterCleanup()
    {
        yield return null; // Wait one frame
        
        try
        {
            // Re-initialize based on current test
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
                
                LogManager.LogMessage($"Test {currentTest} restarted successfully");
            }
            else
            {
                SetupNormalGame();
                LogManager.LogMessage("Normal game restarted successfully");
            }
        }
        catch (System.Exception e)
        {
            LogManager.LogError($"Error in RestartAfterCleanup: {e.Message}\nStack trace: {e.StackTrace}");
        }
    }
} 