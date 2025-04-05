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
    [Tooltip("Minimum number of Alberts that should exist in the simulation")]
    [Range(5, 50)]
    public int MIN_ALBERTS = 20;  // Minimum number of Alberts to maintain

    [Tooltip("Maximum number of Alberts allowed in the simulation")]
    [Range(20, 200)]
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
    [SerializeField] private int maxHiddenLayers = 10;  // Maximum hidden layers for neural networks
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
    public static bool showCreatureLabels = true;  // Toggle for creature labels
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
    [Tooltip("Adjust simulation speed (1.0 = normal, 2.0 = 2x speed)")]
    [Range(0.1f, 3.0f)]
    [SerializeField] private float timeScale = 1.0f;
    [Tooltip("Maximum allowed time scale for safety")]
    [SerializeField] private float maxTimeScale = 3.0f; // Reduced from 5.0f to 3.0f for better stability
    [Tooltip("How often to check population (in seconds)")]
    [SerializeField] private float checkInterval = 0.1f;
    private float lastCheckTime = 0f;
    private float lastSpawnTime = 0f;
    private float spawnCooldown = 1.0f;
    private bool isSpawning = false;

    // Debugging properties to track in inspector
    [Header("Debug Information")]
    [SerializeField] private int creaturesSpawned = 0;
    [SerializeField] private int creaturesRemoved = 0;
    [SerializeField] private float timeSinceLastSpawn = 0f;
    [SerializeField] private float currentAverageAge = 0f;
    [SerializeField] private float currentAverageHealth = 0f;

    [Header("Creature Settings")]
    [SerializeField] private float initialAgingRate = 0.005f;  // Rate at which creatures age and lose health
    [Range(0.0001f, 0.005f)]
    [Tooltip("How quickly creatures age and lose health per second after aging starts")]
    public float creatureAgingRate = 0.005f;  // Default value same as before

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

    void Update()
    {
        // Apply time scale with safety limits
        timeScale = Mathf.Clamp(timeScale, 0.1f, maxTimeScale);
        Time.timeScale = timeScale;

        // Update debug info
        timeSinceLastSpawn = Time.time - lastSpawnTime;
        UpdateDebugStatistics();

        // Scale the check interval with time scale to maintain consistent checks
        float scaledCheckInterval = checkInterval / timeScale;
        
        if (Time.time - lastCheckTime >= scaledCheckInterval)
        {
            lastCheckTime = Time.time;
            ManagePopulation();
        }
    }

    private void UpdateDebugStatistics()
    {
        try
        {
            var creatures = GameObject.FindObjectsOfType<Creature>();
            var alberts = creatures.Where(c => c.type == Creature.CreatureType.Albert).ToArray();
            
            if (alberts.Length > 0)
            {
                currentAverageAge = alberts.Average(c => c.Lifetime);
                currentAverageHealth = alberts.Average(c => c.health);
                
                // Log detailed information periodically (every 3 seconds)
                if (Time.frameCount % 180 == 0)
                {
                    int lowHealthCount = alberts.Count(c => c.health < 0.3f);
                    int agingCount = alberts.Count(c => c.Lifetime > c.agingStartTime);
                    float oldestAge = alberts.Max(c => c.Lifetime);
                    
                    LogManager.LogMessage($"DEBUG: Population={alberts.Length}, " +
                        $"AvgAge={currentAverageAge:F1}s, OldestAge={oldestAge:F1}s, " +
                        $"AvgHealth={currentAverageHealth:F2}, LowHealth={lowHealthCount}, " +
                        $"Aging={agingCount}, TimeScale={timeScale:F1}x");
                }
            }
        }
        catch (System.Exception e)
        {
            LogManager.LogError($"Error in UpdateDebugStatistics: {e.Message}");
        }
    }

    private void ManagePopulation()
    {
        try
        {
            int currentCount = CountAlberts();
            CurrentAlberts = currentCount;
            
            // Only log every few checks to reduce spam
            if (Time.frameCount % 30 == 0)
            {
                LogManager.LogMessage($"Current Albert population: {currentCount}, Spawned: {creaturesSpawned}, Removed: {creaturesRemoved}, TimeScale: {timeScale:F1}x");
            }

            // Scale the spawn cooldown with time scale
            float scaledSpawnCooldown = spawnCooldown / timeScale;
            
            if (currentCount < MIN_ALBERTS && !isSpawning && 
                Time.time - lastSpawnTime >= scaledSpawnCooldown)
            {
                lastSpawnTime = Time.time;
                StartCoroutine(SpawnNewAlbertStaggered());
            }
        }
        catch (System.Exception e)
        {
            LogManager.LogMessage($"Error in ManagePopulation: {e.Message}");
        }
    }

    private Vector3 GetRandomSpawnPosition()
    {
        // Calculate a position with some randomness within the spawn area
        Vector2 offset = Random.insideUnitCircle * spawnSpreadRadius;
        return new Vector3(
            spawnCenter.x + offset.x,
            spawnCenter.y + offset.y,
            0f
        );
    }

    private IEnumerator SpawnNewAlbertStaggered()
    {
        if (isSpawning) yield break;
        
        isSpawning = true;
        
        // Scale the delay with time scale, but with a minimum delay to prevent too rapid spawning
        float scaledDelay = Mathf.Max(0.2f, Random.Range(0.5f, 1.5f) / timeScale);
        LogManager.LogMessage($"Preparing to spawn with delay: {scaledDelay:F2}s at time scale {timeScale:F1}x");
        yield return new WaitForSeconds(scaledDelay);

        // Safety check to ensure prefab exists
        if (albertCreaturePrefab == null)
        {
            LogManager.LogError("Albert creature prefab is missing! Cannot spawn creatures.");
            isSpawning = false;
            yield break;
        }
        
        try
        {
            Vector3 spawnPos = GetRandomSpawnPosition();
            LogManager.LogMessage($"Spawning new Albert at position: {spawnPos}");
            
            // Spawn the creature with a randomized brain
            var creature = SpawnCreatureWithRandomizedBrain(albertCreaturePrefab, spawnPos, Creature.CreatureType.Albert);
            
            if (creature != null)
            {
                creaturesSpawned++;
                
                // Reduce initial age range further to give creatures more time to live
                float randomAge = Random.Range(0f, 3f); // Reduced from 5f to 3f
                float randomReproduction = Random.Range(0f, 1f);
                
                // Set the lifetime using the public property
                creature.Lifetime = randomAge;
                
                // Track when creatures are destroyed
                var creatureGameObject = creature.gameObject;
                StartCoroutine(TrackCreatureDestruction(creatureGameObject));
                
                // IMPORTANT: Don't adjust health for aging if age is less than aging start time
                // This fixes the premature death bug
                if (randomAge > creature.agingStartTime)
                {
                    float ageBeyondThreshold = randomAge - creature.agingStartTime;
                    // CRITICAL FIX: Ensure we're using the creature's actual aging rate
                    // and not applying any additional multipliers
                    float healthLost = ageBeyondThreshold * creature.agingRate;
                    creature.health = Mathf.Max(0.2f, creature.maxHealth - healthLost);
                    LogManager.LogMessage($"Applied aging health adjustment: -{healthLost:F4} (age {randomAge:F1}s > threshold {creature.agingStartTime:F1}s)");
                }
                else
                {
                    // Ensure full health if not past aging threshold
                    creature.health = creature.maxHealth;
                }
                
                // IMPORTANT: Make sure the aging rate is correct
                LogManager.LogMessage($"Creature aging rate: {creature.agingRate:F6} per second");
                
                creature.reproduction = randomReproduction;
                
                // Set generation to 0 for initially spawned Alberts
                creature.generation = 0;
                
                LogManager.LogMessage($"Successfully spawned new Albert with age: {randomAge}, reproduction: {randomReproduction}, health: {creature.health:F2}");
            }
            else
            {
                LogManager.LogError("Failed to spawn creature - returned null");
            }
        }
        catch (System.Exception e)
        {
            LogManager.LogError($"Error in SpawnNewAlbertStaggered: {e.Message}\n{e.StackTrace}");
        }
        finally
        {
            isSpawning = false;
        }
    }

    private IEnumerator TrackCreatureDestruction(GameObject creatureObj)
    {
        // Wait until the creature no longer exists
        yield return new WaitUntil(() => creatureObj == null);
        
        // If we get here, the creature was destroyed
        creaturesRemoved++;
        LogManager.LogMessage($"Creature destroyed. Total spawned: {creaturesSpawned}, Total removed: {creaturesRemoved}");
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
        
        // CRITICAL FIX: Override aging rate from prefab with the configurable value
        // The prefab has incorrect values (0.26 instead of 0.005)
        creatureComponent.agingRate = creatureAgingRate;
        
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
        
        // CRITICAL FIX: Override aging rate from prefab with the configurable value
        // The prefab has incorrect values (0.26 instead of 0.005)
        creatureComponent.agingRate = creatureAgingRate;
        
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
        
        // CRITICAL FIX: Override aging rate from prefab with the configurable value
        // The prefab has incorrect values (0.26 instead of 0.005)
        creatureComponent.agingRate = creatureAgingRate;
        
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
} 