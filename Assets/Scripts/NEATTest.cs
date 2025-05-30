//TODO: bring back the object pool manager, i didnt even realize it was gone
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.IO;
using System;
// Explicitly use UnityEngine.Random to avoid ambiguity with System.Random
using Random = UnityEngine.Random;

public class NEATTest : MonoBehaviour
{
    private static NEATTest instance; //IMPROVEMENT: make this public instead, better for performance and readability

    [Header("Creature Prefabs")]
    public GameObject albertCreaturePrefab;  // Assign in inspector
    public GameObject kaiCreaturePrefab;    // Assign in inspector

    
    [Header("Albert Population Settings")]
    public int MIN_ALBERTS = 20;  // Minimum number of Alberts to maintain
    public int MAX_ALBERTS = 100; // Maximum number of Alberts allowed
    public int INITIAL_ALBERTS = 10; // Number of Alberts to spawn initially
    public float MIN_STARTING_AGE_ALBERT = 0f; // Minimum starting age for initial Alberts
    public float MAX_STARTING_AGE_ALBERT = 5f; // Maximum starting age for initial Alberts
    
    [Header("Kai Population Settings")]
    public int MIN_KAIS = 20;  // Minimum number of Kais to maintain
    public int MAX_KAIS = 100; // Maximum number of Kais allowed
    public int INITIAL_KAIS = 10; // Number of Kais to spawn initially
    public float MIN_STARTING_AGE_KAI = 0f; // Minimum starting age for initial Kais
    public float MAX_STARTING_AGE_KAI = 5f; // Maximum starting age for initial Kais
    
    // Property to display current creature counts in Inspector
    [SerializeField]
    private int _current_alberts = 0;
    public int CurrentAlberts
    {
        get { return _current_alberts; }
        private set { _current_alberts = value; }
    }
    
    [SerializeField]
    private int _current_kais = 0;
    public int CurrentKais
    {
        get { return _current_kais; }
        private set { _current_kais = value; }
    }

    [Header("Network Settings")]
    public int maxHiddenLayers = 10;  // Maximum number of hidden layers allowed
    public const int OBSERVATION_COUNT = 15;
    public const int ACTION_COUNT = 5;


    [Header("Test Settings")]
    public bool runTests = true;

    public enum CurrentTest
    {
        NormalGame,
        MatingMovement,
        AlbertsOnly,
        Reproduction,
        LoadCreature,
        AlbertsVsKais,
        LoadCreaturesBattle
    }
    public CurrentTest currentTest;

    [Header("Visualization Settings")]
    public bool showTreeVisionRange = false;  // Toggle for tree vision range visualization
    public bool showTeammateVisionRange = false;  // Toggle for alberts vision range visualization
    public bool showOpponentVisionRange = false;  // Toggle for kais vision range visualization
    public bool showGroundVisionRange = false;  // Toggle for ground vision range visualization
    // public bool showCherryVisionRange = false;  // Toggle for detection radius visualization
    public bool showCloseRange = false;  // Toggle for close range visualization
    public bool showBowRange = false;  // Toggle for bow range visualization
    public bool showGizmos = false;     // Master toggle for all gizmos
    public Color closeRangeColor = new Color(0.5f, 0, 0.5f, 0.2f);  // Semi-transparent purple
    public Color bowRangeColor = new Color(0, 0.5f, 0.5f, 0.2f);  // Semi-transparent blue
    public bool showCreatureLabels = true;  // Toggle for creature labels
    public bool showSpawnArea = false;  // Toggle for spawn area visualization
    public Color spawnAreaColor = new Color(0.2f, 0.8f, 0.2f, 0.2f);  // Semi-transparent green
    
    [Header("Spawn Area Settings")]
    public Vector2 spawnCenter = new Vector2(-25f, -0f);  // Center of the spawn area
    public float spawnSpreadRadius = 2f;  // Radius of the spawn area
    
    // Additional spawn area for Kais in the dual-species test
    public Vector2 rightSpawnCenter = new Vector2(25f, 0f);  // Center of the Kai spawn area (right side)
    public float rightSpawnSpreadRadius = 2f;  // Radius of the right spawn area
    
    // NEAT instance for access by other classes
    [System.NonSerialized]
    public NEAT.Genome.Genome neat = new NEAT.Genome.Genome(0);

    [Header("Population Settings")]
    private float lastPopulationCheck = 0f;
    private float populationCheckInterval = 0.1f;  // Check population every 0.1 seconds
    private float lastSpawnTime = 0f;
    private float spawnCooldown = 0.01f;  // Minimum time between spawns
    private bool isSpawning = false;  // Flag to prevent multiple spawn coroutines

    private float countTimer = 0f;

    [Header("Creature Loading Settings")]
    public string savedCreaturePath = "";  // Path to the saved creature JSON file
    
    [Header("Creatures Battle Loading Settings")]
    public string albertsFolderPath = "";  // Folder containing JSONs for Alberts
    public string kaisFolderPath = "";     // Folder containing JSONs for Kais
    [Tooltip("If checked, the system will resample from the folder until the target population is reached")]
    public bool resampleCreatures = false;  // Whether to resample from the folder to reach the target count
    public bool respectTypeInFiles = true;  // Whether to respect the creature type in the files (or override with folder type)

    [Header("Creature Saving Settings")]
    public bool saveCreatures = false;  // Option to save creatures at generation milestones
    public int genSavingFrequency = 5;  // Save creatures every X generations (5 = gen 5, 10, 15, etc.)

    // Generation tracking for auto-saving
    private string currentRunSaveFolder = "";
    private HashSet<int> savedGenerations = new HashSet<int>();

    // Add new variables for Kai spawning
    private float lastKaiSpawnTime = 0f;
    private bool isSpawningKai = false;

    private void Awake()
    {
        // Check if there's already an instance
        if (instance != null && instance != this) //IMPROVEMENT: i think we have a lot of null checks in the codebase that might be unnecessary like this one, can look into removing them if it doesn't cause issues
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

        // Initialize the ArrowsManager if it doesn't exist
        if (ArrowsManager.Instance == null)
        {
            GameObject arrowsManagerObj = new GameObject("ArrowsManager");
            arrowsManagerObj.AddComponent<ArrowsManager>();
        }

        // Create save folder for this run if auto-saving is enabled
        if (saveCreatures)
        {
            CreateRunSaveFolder();
        }

        // Debug.Log($"NEATTest starting on GameObject: {gameObject.name}");

        // Clear any existing creatures first
        var existingCreatures = GameObject.FindObjectsOfType<Creature>();
        // Debug.Log($"Found {existingCreatures.Length} existing creatures to clean up");
        foreach (var creature in existingCreatures)
        {
            ObjectPoolManager.ReturnObjectToPool(creature.gameObject);
        }

        if (runTests)
        {
            switch (currentTest)
            {
                case CurrentTest.NormalGame:
                    SetupNormalGame();
                    break;
                case CurrentTest.MatingMovement:
                    SetupMatingMovementTest();
                    break;
                case CurrentTest.AlbertsOnly:
                    SetupAlbertsOnlyTest();
                    break;
                case CurrentTest.Reproduction:
                    SetupReproductionTest();
                    break;
                case CurrentTest.LoadCreature:
                    SetupLoadCreatureTest();
                    break;
                case CurrentTest.AlbertsVsKais:
                    SetupAlbertsVsKaisTest();
                    break;
                case CurrentTest.LoadCreaturesBattle:
                    SetupLoadCreaturesBattleTest();
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
            // Check for population management (only for specific tests)
            if (currentTest == CurrentTest.MatingMovement || 
                currentTest == CurrentTest.AlbertsOnly || 
                currentTest == CurrentTest.AlbertsVsKais ||
                currentTest == CurrentTest.LoadCreaturesBattle)  // Add LoadCreaturesBattle test
            {
                // Check current creature counts periodically
                countTimer += Time.deltaTime;
                if (countTimer >= 0.4f)
                {
                    CountAlberts();
                    if (currentTest == CurrentTest.AlbertsVsKais || 
                        currentTest == CurrentTest.LoadCreaturesBattle)  // Also count Kais for LoadCreaturesBattle
                    {
                        CountKais();
                    }
                    countTimer = 0f;
                }
                
                // Automatic population management
                ManagePopulation();
            }
            
            // Testing controls
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                currentTest = CurrentTest.MatingMovement;
                RestartTest();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                currentTest = CurrentTest.AlbertsOnly;
                RestartTest();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                currentTest = CurrentTest.Reproduction;
                RestartTest();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                currentTest = CurrentTest.NormalGame;
                RestartTest();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                currentTest = CurrentTest.LoadCreature;
                RestartTest();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                currentTest = CurrentTest.AlbertsVsKais;
                RestartTest();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha7))
            {
                currentTest = CurrentTest.LoadCreaturesBattle;
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
                LogManager.LogMessage($"Albert population below minimum ({MIN_ALBERTS}). Spawning new Albert with staggered timing.");
                
                // Start a coroutine to spawn a new Albert with a random delay
                StartCoroutine(SpawnNewAlbertStaggered());
            }
            
            // If this is an Alberts vs Kais test, also manage Kai population
            if (currentTest == CurrentTest.AlbertsVsKais)
            {
                // Count current Kais
                int currentKais = CountKais();
                
                // Update the inspector-visible count
                CurrentKais = currentKais;
                
                // Log population count
                LogManager.LogMessage($"Current Kai population: {currentKais}");
                
                // Check if we need to spawn more Kais
                if (currentKais < MIN_KAIS && Time.time - lastKaiSpawnTime >= spawnCooldown && !isSpawningKai)
                {
                    LogManager.LogMessage($"Kai population below minimum ({MIN_KAIS}). Spawning new Kai with staggered timing.");
                    
                    // Start a coroutine to spawn a new Kai with a random delay
                    StartCoroutine(SpawnNewKaiStaggered());
                }
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
        yield return new WaitForSeconds(Random.Range(0.01f, 0.3f));
        
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
            // Debug.Log(ObjectPoolManager.ObjectPools);
            if (creature == null)
            {
                LogManager.LogError("Failed to spawn new Albert - SpawnCreatureWithRandomizedBrain returned null");
                isSpawning = false;
                yield break;
            }
            
            // Initialize with random age
            float startingAge = Random.Range(MIN_STARTING_AGE_ALBERT, MAX_STARTING_AGE_ALBERT);
            creature.Lifetime = startingAge;
            
            // If the creature starts with an age past the aging threshold, give it appropriate health
            if (startingAge > creature.agingStartTime)
            {
                float ageBeyondThreshold = startingAge - creature.agingStartTime;
                float healthLost = ageBeyondThreshold * creature.agingRate;
                creature.health = Mathf.Max(0.1f, creature.maxHealth - healthLost);
            }
            
            // Set starting reproduction meter to a random value between 0 and 1
            float startingReproductionMeter = Random.Range(0f, 1f);
            creature.reproductionMeter = startingReproductionMeter;
            
            // Set generation to 0 for initially spawned Alberts
            creature.generation = 0;
            
            // Check if this creature should be saved (generation milestone)
            CheckCreatureForSaving(creature);
            
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

    private IEnumerator SpawnNewKaiStaggered()
    {
        isSpawningKai = true;
        lastKaiSpawnTime = Time.time;
        
        // Add a random delay between 0.5 and 1.5 seconds before spawning
        yield return new WaitForSeconds(Random.Range(0.01f, 0.3f));
        
        try
        {
            // Calculate a position with some randomness within the spawn area
            Vector2 offset = Random.insideUnitCircle * rightSpawnSpreadRadius;
            Vector3 position = new Vector3(
                rightSpawnCenter.x + offset.x,
                rightSpawnCenter.y + offset.y,
                0f
            );
            
            LogManager.LogMessage($"Spawning new Kai at position: {position}");
            
            // Spawn the creature with a randomized brain
            var creature = SpawnCreatureWithRandomizedBrain(kaiCreaturePrefab, position, Creature.CreatureType.Kai);
            
            if (creature == null)
            {
                LogManager.LogError("Failed to spawn new Kai - SpawnCreatureWithRandomizedBrain returned null");
                isSpawningKai = false;
                yield break;
            }
            
            // Initialize with random age
            float startingAge = Random.Range(MIN_STARTING_AGE_KAI, MAX_STARTING_AGE_KAI);
            creature.Lifetime = startingAge;
            
            // If the creature starts with an age past the aging threshold, give it appropriate health
            if (startingAge > creature.agingStartTime)
            {
                float ageBeyondThreshold = startingAge - creature.agingStartTime;
                float healthLost = ageBeyondThreshold * creature.agingRate;
                creature.health = Mathf.Max(0.1f, creature.maxHealth - healthLost);
            }
            
            // Set starting reproduction meter to a random value between 0 and 1
            float startingReproductionMeter = Random.Range(0f, 1f);
            creature.reproductionMeter = startingReproductionMeter;
            
            // Set generation to 0 for initially spawned Kais
            creature.generation = 0;
            
            // Check if this creature should be saved (generation milestone)
            CheckCreatureForSaving(creature);
            
            LogManager.LogMessage($"Successfully spawned new Kai with age: {startingAge}, reproduction meter: {startingReproductionMeter}");
        }
        catch (System.Exception e)
        {
            LogManager.LogError($"Error in SpawnNewKaiStaggered: {e.Message}\nStack trace: {e.StackTrace}");
        }
        finally
        {
            isSpawningKai = false;
        }
    }

    // Modify the CanReproduce method to check species-specific limits
    public bool CanReproduce(Creature.CreatureType type)
    {
        if (type == Creature.CreatureType.Albert)
        {
            return CurrentAlberts < MAX_ALBERTS;
        }
        else if (type == Creature.CreatureType.Kai)
        {
            return CurrentKais < MAX_KAIS;
        }
        return true; // Allow other types to reproduce without limit
    }
    
    // Keep the old method for backward compatibility
    public bool CanReproduce()
    {
        return CurrentAlberts < MAX_ALBERTS;
    }

    private int CountKais()
    {
        try
        {
            // Find all creatures in the scene
            var creatures = GameObject.FindObjectsOfType<Creature>();
            
            // Count only Kais
            int count = creatures.Count(c => c.type == Creature.CreatureType.Kai);
            
            LogManager.LogMessage($"Counted {count} Kai creatures in the scene");
            return count;
        }
        catch (System.Exception e)
        {
            LogManager.LogError($"Error in CountKais: {e.Message}\nStack trace: {e.StackTrace}");
            return 0;  // Return 0 if there's an error
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
        Debug.Log("Starting Test: Alberts Only - Spawning initial Alberts");
        
        // Spawn initial Alberts as specified by INITIAL_ALBERTS parameter
        for (int i = 0; i < INITIAL_ALBERTS; i++)
        {
            // Calculate a position with some randomness within the spawn area
            Vector2 offset = Random.insideUnitCircle * spawnSpreadRadius;
            Vector3 position = new Vector3(
                spawnCenter.x + offset.x,
                spawnCenter.y + offset.y,
                0f
            );
        
            // Spawn the creature with a randomized brain
            var creature = SpawnCreatureWithRandomizedBrain(albertCreaturePrefab, position, Creature.CreatureType.Albert);
            
            if (creature != null)
            {
                // Initialize with random age based on parameters
                float startingAge = Random.Range(MIN_STARTING_AGE_ALBERT, MAX_STARTING_AGE_ALBERT);
                creature.Lifetime = startingAge;
                
                // If the creature starts with an age past the aging threshold, adjust health
                if (startingAge > creature.agingStartTime)
                {
                    float ageBeyondThreshold = startingAge - creature.agingStartTime;
                    float healthLost = ageBeyondThreshold * creature.agingRate;
                    creature.health = Mathf.Max(0.5f, creature.maxHealth - healthLost);
                }
                
                // Set starting reproduction meter to a random value
                creature.reproductionMeter = Random.Range(0f, 1f);
                
                // Set generation to 0 for initial Alberts
                creature.generation = 0;
                
                LogManager.LogMessage($"Spawned initial Albert {i+1}/{INITIAL_ALBERTS} at {position}, age: {startingAge:F1}");
            }
        }
        
        // Report on the setup
        LogManager.LogMessage($"Initial setup complete: {INITIAL_ALBERTS} Alberts spawned");
        LogManager.LogMessage($"Age range: {MIN_STARTING_AGE_ALBERT}-{MAX_STARTING_AGE_ALBERT}");
        LogManager.LogMessage($"Population management: MIN_ALBERTS={MIN_ALBERTS}, MAX_ALBERTS={MAX_ALBERTS}");
        
        // Check current count
        CountAlberts();
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
        var creature = ObjectPoolManager.SpawnObject(prefab, position, Quaternion.identity);
        ParenthoodManager.AssignParent(creature);
        AnimatingDoTweenUtilities.PlayGrow(creature);
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
        
        // Add input nodes
        for (int i = 0; i < OBSERVATION_COUNT; i++)
        {
            var node = new NEAT.Genes.NodeGene(i, NEAT.Genes.NodeType.Input);
            node.Layer = 0;  // Input layer
            node.Bias = 0.0; // Explicitly set bias to 0 for input nodes
            genome.AddNode(node);
        }
        
       // Add output nodes
       for (int i = OBSERVATION_COUNT; i < OBSERVATION_COUNT + ACTION_COUNT; i++)
       {
           var node = new NEAT.Genes.NodeGene(i, NEAT.Genes.NodeType.Output);
           node.Layer = 2;
           node.Bias = 0.0;
           genome.AddNode(node);
       }


        for(int i = 0; i < OBSERVATION_COUNT; i++)
        {
            for (int j = OBSERVATION_COUNT; j < OBSERVATION_COUNT + ACTION_COUNT; j++)
            {
                genome.AddConnection(new NEAT.Genes.ConnectionGene(genome.Connections.Count, i, j, Random.Range(-1f, 1f)));
            }
        }
        
        return genome;
    }
    
    NEAT.Genome.Genome CreateInitialKaiGenome() // TODO: remove this
    {
        var genome = new NEAT.Genome.Genome(0);
        
        // Add input nodes
        for (int i = 0; i < OBSERVATION_COUNT; i++)
        {
            var node = new NEAT.Genes.NodeGene(i, NEAT.Genes.NodeType.Input);
            node.Layer = 0;
            node.Bias = 0.0; // Explicitly set bias to 0 for input nodes
            genome.AddNode(node);
        }
        
        // Add output nodes (4 outputs: x,y velocity, chop, sword)
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
        
        // Ground avoidance/attraction connections
        genome.AddConnection(new NEAT.Genes.ConnectionGene(18, 11, 17, 0.5f)); // Ground x to horizontal velocity
        genome.AddConnection(new NEAT.Genes.ConnectionGene(19, 12, 18, 0.5f)); // Ground y to vertical velocity
        
        return genome;
    }

    // New method to spawn creatures with more randomized brains
    private Creature SpawnCreatureWithRandomizedBrain(GameObject prefab, Vector3 position, Creature.CreatureType type)
    {
        // Create the creature instance
        GameObject creature = ObjectPoolManager.SpawnObject(prefab, position, Quaternion.identity);
        ParenthoodManager.AssignParent(creature);
        AnimatingDoTweenUtilities.PlayGrow(creature);
        Creature creatureComponent = creature.GetComponent<Creature>();
        creatureComponent.type = type;
        
        // Create a base genome
        var genome = CreateInitialGenome();
        
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
        var creature = ObjectPoolManager.SpawnObject(prefab, position, Quaternion.identity);
        var creatureComponent = creature.GetComponent<Creature>();
        creatureComponent.type = type;
        
        // Create a custom genome with clear reproduction bias
        var genome = new NEAT.Genome.Genome(0);
        
        // Add input nodes
   
        for (int i = 0; i < 14; i++)
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
        
        // Add connections for ground detection
        genome.AddConnection(new NEAT.Genes.ConnectionGene(8, 11, 17, 0.3f));  // Ground x to x movement
        genome.AddConnection(new NEAT.Genes.ConnectionGene(9, 12, 18, 0.3f));  // Ground y to y movement
        
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
            
            // Draw the right spawn area as well (for Alberts vs Kais test)
            if (currentTest == CurrentTest.AlbertsVsKais)
            {
                // Use a different color tint for the right spawn area (more blue)
                Color rightSpawnColor = new Color(spawnAreaColor.r * 0.7f, spawnAreaColor.g, spawnAreaColor.b * 1.3f, spawnAreaColor.a);
                Gizmos.color = rightSpawnColor;
                
                // Draw the right spawn area
                Gizmos.DrawSphere(new Vector3(rightSpawnCenter.x, rightSpawnCenter.y, 0), rightSpawnSpreadRadius);
                
                // Draw wire frame
                Gizmos.color = new Color(rightSpawnColor.r, rightSpawnColor.g, rightSpawnColor.b, 0.5f);
                Gizmos.DrawWireSphere(new Vector3(rightSpawnCenter.x, rightSpawnCenter.y, 0), rightSpawnSpreadRadius);
            }
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
                ObjectPoolManager.ReturnObjectToPool(creature.gameObject);
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
                    case CurrentTest.NormalGame:
                        SetupNormalGame();
                        break;
                    case CurrentTest.MatingMovement:
                        SetupMatingMovementTest();
                        break;
                    case CurrentTest.AlbertsOnly:
                        SetupAlbertsOnlyTest();
                        break;
                    case CurrentTest.Reproduction:
                        SetupReproductionTest();
                        break;
                    case CurrentTest.LoadCreature:
                        SetupLoadCreatureTest();
                        break;
                    case CurrentTest.AlbertsVsKais:
                        SetupAlbertsVsKaisTest();
                        break;
                    case CurrentTest.LoadCreaturesBattle:
                        SetupLoadCreaturesBattleTest();
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

    // Add this method in the NEATTest class, ideally near other genome-related methods
    private void ValidateGenome(NEAT.Genome.Genome genome)
    {
        if (genome == null)
        {
            if (LogManager.Instance != null)
            {
                LogManager.LogMessage("Error: Attempting to validate a null genome");
            }
            else
            {
                Debug.LogError("Attempting to validate a null genome");
            }
            return;
        }

        // 1. Ensure at least one connection exists between input and output layers
        bool hasIOConnection = false;
        foreach (var conn in genome.Connections.Values)
        {
            if (!conn.Enabled) continue;
            
            if (genome.Nodes.ContainsKey(conn.InputKey) && 
                genome.Nodes.ContainsKey(conn.OutputKey) &&
                genome.Nodes[conn.InputKey].Type == NEAT.Genes.NodeType.Input &&
                genome.Nodes[conn.OutputKey].Type == NEAT.Genes.NodeType.Output)
            {
                hasIOConnection = true;
                break;
            }
        }
        
        // If no direct I/O connections, add some
        if (!hasIOConnection)
        {
            var inputNodes = genome.Nodes.Values.Where(n => n.Type == NEAT.Genes.NodeType.Input).ToList();
            var outputNodes = genome.Nodes.Values.Where(n => n.Type == NEAT.Genes.NodeType.Output).ToList();
            
            if (inputNodes.Count > 0 && outputNodes.Count > 0)
            {
                // Add at least one connection from each input to a random output
                foreach (var input in inputNodes)
                {
                    var output = outputNodes[Random.Range(0, outputNodes.Count)];
                    
                    var newConn = new NEAT.Genes.ConnectionGene(
                        genome.Connections.Count,
                        input.Key,
                        output.Key,
                        Random.Range(-0.5f, 0.5f));
                        
                    genome.AddConnection(newConn);
                    
                    if (LogManager.Instance != null)
                    {
                        LogManager.LogMessage($"Added connection from input {input.Key} to output {output.Key} to repair genome");
                    }
                    else
                    {
                        Debug.Log($"Added connection from input {input.Key} to output {output.Key} to repair genome");
                    }
                }
            }
        }
        
        // 2. Ensure proper node layers
        foreach (var node in genome.Nodes.Values)
        {
            // Input nodes should be layer 0
            if (node.Type == NEAT.Genes.NodeType.Input && node.Layer != 0)
            {
                node.Layer = 0;
            }
            
            // Output nodes should be at least layer 1
            if (node.Type == NEAT.Genes.NodeType.Output && node.Layer <= 0)
            {
                node.Layer = 1;
            }
        }
        
        // 3. Verify hidden nodes have proper layers (between input and output)
        var inputLayer = 0;
        var minOutputLayer = genome.Nodes.Values
            .Where(n => n.Type == NEAT.Genes.NodeType.Output)
            .Min(n => n.Layer);
        
        foreach (var node in genome.Nodes.Values)
        {
            if (node.Type == NEAT.Genes.NodeType.Hidden)
            {
                // Ensure hidden nodes are between input and output layers
                if (node.Layer <= inputLayer || node.Layer >= minOutputLayer)
                {
                    node.Layer = inputLayer + 1;
                }
            }
        }
        
        // 4. Verify connection directions follow layer hierarchy
        foreach (var conn in genome.Connections.Values)
        {
            if (!conn.Enabled) continue;
            
            if (genome.Nodes.ContainsKey(conn.InputKey) && 
                genome.Nodes.ContainsKey(conn.OutputKey))
            {
                var inputNode = genome.Nodes[conn.InputKey];
                var outputNode = genome.Nodes[conn.OutputKey];
                
                // If connection goes backward (from higher to lower layer), disable it
                if (inputNode.Layer >= outputNode.Layer)
                {
                    if (LogManager.Instance != null)
                    {
                        LogManager.LogMessage($"Disabling backward connection from node {conn.InputKey} (layer {inputNode.Layer}) to {conn.OutputKey} (layer {outputNode.Layer})");
                    }
                    else
                    {
                        Debug.LogWarning($"Disabling backward connection from node {conn.InputKey} (layer {inputNode.Layer}) to {conn.OutputKey} (layer {outputNode.Layer})");
                    }
                    conn.Enabled = false;
                }
            }
        }
    }

    // Add this call in the SpawnCreature method, right before the genome is used
    private void SpawnCreature(Vector3 pos, bool isInitial = false, NEAT.Genome.Genome genome = null, Creature parent1 = null, Creature parent2 = null)
    {
        // ... existing code ...
        
        // Validate and fix genome right before it's used to create a creature
        if (genome != null)
        {
            ValidateGenome(genome);
        }
        
        // Create creature
        // ... existing code ...
    }

    private void SetupLoadCreatureTest()
    {
        if (string.IsNullOrEmpty(savedCreaturePath))
        {
            Debug.LogError("No saved creature path specified. Please set the path in the inspector.");
            return;
        }

        Debug.Log($"Starting Test: Load Creature - Loading from {savedCreaturePath}");

        // Calculate spawn position
        Vector2 offset = Random.insideUnitCircle * spawnSpreadRadius;
        Vector3 position = new Vector3(
            spawnCenter.x + offset.x,
            spawnCenter.y + offset.y,
            0f
        );

        // Load the creature
        var creature = CreatureLoader.LoadCreature(albertCreaturePrefab, position, savedCreaturePath);
        
        if (creature != null)
        {
            Debug.Log($"Successfully loaded creature at position {position}");
            Debug.Log($"Creature properties: Type={creature.type}, Generation={creature.generation}, Age={creature.Lifetime:F1}");
        }
        else
        {
            Debug.LogError("Failed to load creature");
        }
    }

    private void CreateRunSaveFolder()
    {
        try
        {
            // Format the timestamp: yyyymmdd__[am/pm]_hh_mm_ss
            DateTime now = DateTime.Now;
            string ampm = now.Hour < 12 ? "am" : "pm";
            // Use 00 for 12 AM to maintain chronological order
            int hourDisplay = now.Hour % 12;
            if (hourDisplay == 0) hourDisplay = 12;
            if (now.Hour == 0) hourDisplay = 0; // Special case for midnight (00am)
            
            string timestamp = string.Format("{0:yyyyMMdd}__{1}_{2:D2}_{3:D2}_{4:D2}", 
                now, ampm, hourDisplay, now.Minute, now.Second);
            
            // Get the base save directory
            string baseDir = Path.Combine(Application.persistentDataPath, "SavedCreatures");
            
            // Create the run-specific directory
            currentRunSaveFolder = Path.Combine(baseDir, timestamp);
            
            if (!Directory.Exists(baseDir))
            {
                Directory.CreateDirectory(baseDir);
            }
            
            if (!Directory.Exists(currentRunSaveFolder))
            {
                Directory.CreateDirectory(currentRunSaveFolder);
                Debug.Log($"Created run save folder: {currentRunSaveFolder}");
            }
            
            // Initialize tracking set
            savedGenerations.Clear();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create run save folder: {e.Message}");
            currentRunSaveFolder = ""; // Reset so we don't try to save
        }
    }

    // Check if a creature should be saved based on its generation
    public void CheckCreatureForSaving(Creature creature)
    {
        // Only proceed if saving is enabled and we have a valid save folder
        if (!saveCreatures || string.IsNullOrEmpty(currentRunSaveFolder)) return;
        
        // Make sure creature is valid
        if (creature == null) return;
        
        // Only save at generation milestones (gen intervals based on genSavingFrequency)
        if (creature.generation % genSavingFrequency == 0 && creature.generation >= genSavingFrequency)
        {
            int generation = creature.generation;
            
            // Save every creature that reaches the milestone
            SaveCreatureAtMilestone(creature, generation);
            
            // Keep track of generation milestones we've seen (for logging/stats purposes)
            if (!savedGenerations.Contains(generation))
            {
                savedGenerations.Add(generation);
                Debug.Log($"First creature reached generation milestone {generation}");
            }
            else
            {
                Debug.Log($"Another creature reached generation milestone {generation}");
            }
        }
    }
    
    private void SaveCreatureAtMilestone(Creature creature, int milestone)
    {
        try
        {
            // Create the milestone folder if it doesn't exist
            string milestoneFolder = Path.Combine(currentRunSaveFolder, $"gen{milestone}");
            if (!Directory.Exists(milestoneFolder))
            {
                Directory.CreateDirectory(milestoneFolder);
                Debug.Log($"Created milestone folder: {milestoneFolder}");
            }
            
            // Generate a unique filename
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string filename = $"{creature.type}_Gen{creature.generation}_{timestamp}.json";
            string savePath = Path.Combine(milestoneFolder, filename);
            
            // Save the creature
            SaveCreatureToPath(creature, savePath);
            
            Debug.Log($"Saved milestone creature (gen {milestone}) to: {savePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save creature at milestone: {e.Message}");
        }
    }
    
    private void SaveCreatureToPath(Creature creature, string path)
    {
        try
        {
            // This calls our existing CreatureSaver functionality but with a custom path
            var savedCreature = new SavedCreature
            {
                type = creature.type,
                health = creature.health,
                maxHealth = creature.maxHealth,
                energyMeter = creature.energyMeter,
                maxEnergy = creature.maxEnergy,
                lifetime = creature.Lifetime,
                generation = creature.generation,
                moveSpeed = creature.moveSpeed,
                pushForce = creature.pushForce,
                closeRange = creature.closeRange,
                bowRange = creature.bowRange,
                actionEnergyCost = creature.actionEnergyCost,
                chopDamage = creature.chopDamage,
                swordDamage = creature.swordDamage,
                weightMutationRate = creature.weightMutationRate,
                mutationRange = creature.mutationRange,
                addNodeRate = creature.addNodeRate,
                addConnectionRate = creature.addConnectionRate,
                deleteConnectionRate = creature.deleteConnectionRate,
                maxHiddenLayers = creature.maxHiddenLayers,
                brain = SerializeBrain(creature.GetBrain())
            };
            
            // Convert to JSON and save
            string json = JsonUtility.ToJson(savedCreature, true);
            File.WriteAllText(path, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save creature to path: {e.Message}");
        }
    }
    
    private SerializedBrain SerializeBrain(NEAT.NN.FeedForwardNetwork brain)
    {
        if (brain == null) return null;
        
        var nodesField = brain.GetType().GetField("_nodes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var connectionsField = brain.GetType().GetField("_connections", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (nodesField == null || connectionsField == null) return null;
        
        var nodes = nodesField.GetValue(brain) as Dictionary<int, NEAT.Genes.NodeGene>;
        var connections = connectionsField.GetValue(brain) as Dictionary<int, NEAT.Genes.ConnectionGene>;
        
        if (nodes == null || connections == null) return null;
        
        var serializedBrain = new SerializedBrain
        {
            nodes = nodes.Values.Select(n => new SerializedNode
            {
                key = n.Key,
                type = (int)n.Type,
                layer = n.Layer,
                bias = n.Bias
            }).ToArray(),
            
            connections = connections.Values.Select(c => new SerializedConnection
            {
                key = c.Key,
                inputKey = c.InputKey,
                outputKey = c.OutputKey,
                weight = c.Weight,
                enabled = c.Enabled
            }).ToArray()
        };
        
        return serializedBrain;
    }

    private void SetupAlbertsVsKaisTest()
    {
        Debug.Log("Starting Test: Alberts vs Kais - Battle Simulation");
        
        // Spawn Alberts 
        for (int i = 0; i < INITIAL_ALBERTS; i++)
        {
            // Calculate a position with randomness within the left spawn area
            Vector2 offset = Random.insideUnitCircle * spawnSpreadRadius;
            Vector3 position = new Vector3(
                spawnCenter.x + offset.x,
                spawnCenter.y + offset.y,
                0f
            );
            
            // Spawn Albert with a randomized brain
            var albert = SpawnCreatureWithRandomizedBrain(albertCreaturePrefab, position, Creature.CreatureType.Albert);
            
            if (albert != null)
            {
                // Initialize with random age
                float startingAge = Random.Range(MIN_STARTING_AGE_ALBERT, MAX_STARTING_AGE_ALBERT);
                albert.Lifetime = startingAge;
                
                // Set starting reproduction meter to a random value
                albert.reproductionMeter = Random.Range(0f, 0.2f);
                
                // Set generation to 0 for initial Alberts
                albert.generation = 0;
                
                LogManager.LogMessage($"Spawned Albert {i+1}/{INITIAL_ALBERTS} at {position}, age: {startingAge:F1}");
            }
        }
        
        // Spawn Kais
        for (int i = 0; i < INITIAL_KAIS; i++)
        {
            // Calculate a position with randomness within the right spawn area
            Vector2 offset = Random.insideUnitCircle * rightSpawnSpreadRadius;
            Vector3 position = new Vector3(
                rightSpawnCenter.x + offset.x,
                rightSpawnCenter.y + offset.y,
                0f
            );
            
            // Spawn Kai with a randomized brain
            var kai = SpawnCreatureWithRandomizedBrain(kaiCreaturePrefab, position, Creature.CreatureType.Kai);
            
            if (kai != null)
            {
                // Initialize with random age
                float startingAge = Random.Range(MIN_STARTING_AGE_KAI, MAX_STARTING_AGE_KAI);
                kai.Lifetime = startingAge;
                
                // Set starting reproduction meter to a random value
                kai.reproductionMeter = Random.Range(0f, 0.2f);
                
                // Set generation to 0 for initial Kais
                kai.generation = 0;
                
                LogManager.LogMessage($"Spawned Kai {i+1}/{INITIAL_KAIS} at {position}, age: {startingAge:F1}");
            }
        }
        
        // Update counts
        CountAlberts();
        CountKais();
        
        LogManager.LogMessage("Alberts vs Kais battle simulation setup complete!");
        LogManager.LogMessage($"Left side (Alberts): {INITIAL_ALBERTS} creatures near {spawnCenter}");
        LogManager.LogMessage($"Right side (Kais): {INITIAL_KAIS} creatures near {rightSpawnCenter}");
        LogManager.LogMessage($"Population management: MIN_ALBERTS={MIN_ALBERTS}, MAX_ALBERTS={MAX_ALBERTS}");
        LogManager.LogMessage($"Population management: MIN_KAIS={MIN_KAIS}, MAX_KAIS={MAX_KAIS}");
    }

    private void SetupLoadCreaturesBattleTest()
    {
        Debug.Log("Starting Test: Load Creatures Battle");
        
        if (string.IsNullOrEmpty(albertsFolderPath) || string.IsNullOrEmpty(kaisFolderPath))
        {
            Debug.LogError("Please specify folder paths for both Alberts and Kais in the inspector!");
            return;
        }
        
        int albertTarget = INITIAL_ALBERTS;
        int kaisTarget = INITIAL_KAIS;
        
        // Load and spawn Alberts from folder
        int albertsLoaded = LoadAndSpawnCreaturesFromFolder(
            albertsFolderPath, 
            spawnCenter, 
            spawnSpreadRadius, 
            albertCreaturePrefab, 
            Creature.CreatureType.Albert,
            albertTarget,
            resampleCreatures,
            respectTypeInFiles
        );
        
        // Load and spawn Kais from folder
        int kaisLoaded = LoadAndSpawnCreaturesFromFolder(
            kaisFolderPath, 
            rightSpawnCenter, 
            rightSpawnSpreadRadius, 
            kaiCreaturePrefab, 
            Creature.CreatureType.Kai,
            kaisTarget,
            resampleCreatures,
            respectTypeInFiles
        );
        
        // Update creature counts
        CurrentAlberts = albertsLoaded;
        CurrentKais = kaisLoaded;
        
        Debug.Log($"Creatures Battle Setup Complete: Loaded {albertsLoaded} Alberts and {kaisLoaded} Kais");
        
        if (albertsLoaded == 0 && kaisLoaded == 0)
        {
            Debug.LogWarning("No creatures were loaded from either folder. Please check the folder paths.");
        }
    }
    
    private int LoadAndSpawnCreaturesFromFolder(
        string folderPath, 
        Vector2 spawnCenter, 
        float spreadRadius, 
        GameObject prefab, 
        Creature.CreatureType type,
        int targetCount,
        bool resample,
        bool respectTypeInFile)
    {
        try
        {
            // Check if directory exists
            if (!System.IO.Directory.Exists(folderPath))
            {
                Debug.LogError($"Directory not found: {folderPath}");
                return 0;
            }
            
            // Get all JSON files in the folder
            string[] jsonFiles = System.IO.Directory.GetFiles(folderPath, "*.json");
            
            if (jsonFiles.Length == 0)
            {
                Debug.LogWarning($"No JSON files found in {folderPath}");
                return 0;
            }
            
            Debug.Log($"Found {jsonFiles.Length} JSON files in {folderPath}");
            
            // If resampling is enabled, we'll use the target count
            // Otherwise, limit to minimum of files found or target
            int filesToProcess = resample ? 
                targetCount : 
                Mathf.Min(jsonFiles.Length, targetCount);
            
            int creaturesLoaded = 0;
            
            // If resampling and not enough files, we'll need to reuse some files
            if (resample && jsonFiles.Length < targetCount)
            {
                Debug.LogWarning($"Only {jsonFiles.Length} files available for {type}s, but target is {targetCount}. Will sample with replacement.");
            }
            
            // Process each JSON file
            for (int i = 0; i < filesToProcess; i++)
            {
                try
                {
                    // Calculate spawn position with random offset
                    Vector2 offset = UnityEngine.Random.insideUnitCircle * spreadRadius;
                    Vector3 position = new Vector3(
                        spawnCenter.x + offset.x,
                        spawnCenter.y + offset.y,
                        0f
                    );
                    
                    // If resampling, pick a random file; otherwise use sequential
                    string jsonFile = resample ? 
                        jsonFiles[UnityEngine.Random.Range(0, jsonFiles.Length)] : 
                        jsonFiles[i % jsonFiles.Length];
                    
                    // Load the creature, overriding type if needed
                    var creature = LoadCreatureFromFile(jsonFile, prefab, position, respectTypeInFile ? null : type);
                    
                    if (creature != null)
                    {
                        // Initialize with creature's values but adjust age if needed
                        float startingAge = type == Creature.CreatureType.Albert ? 
                            Random.Range(MIN_STARTING_AGE_ALBERT, MAX_STARTING_AGE_ALBERT) :
                            Random.Range(MIN_STARTING_AGE_KAI, MAX_STARTING_AGE_KAI);
                        
                        // Override age if desired (comment out to keep saved ages)
                        creature.Lifetime = startingAge;
                        
                        creaturesLoaded++;
                        Debug.Log($"Successfully loaded {type} #{creaturesLoaded} from {System.IO.Path.GetFileName(jsonFile)}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error loading creature #{i}: {e.Message}");
                }
            }
            
            return creaturesLoaded;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading creatures from folder {folderPath}: {e.Message}");
            return 0;
        }
    }
    
    private Creature LoadCreatureFromFile(string filePath, GameObject prefab, Vector3 position, Creature.CreatureType? overrideType)
    {
        try
        {
            // Read the JSON file
            if (!System.IO.File.Exists(filePath))
            {
                Debug.LogError($"File not found: {filePath}");
                return null;
            }
            
            string json = System.IO.File.ReadAllText(filePath);
            SavedCreature savedCreature = JsonUtility.FromJson<SavedCreature>(json);
            
            if (savedCreature == null)
            {
                Debug.LogError($"Failed to parse JSON from {filePath}");
                return null;
            }
            
            // Override the type if requested
            if (overrideType.HasValue)
            {
                savedCreature.type = overrideType.Value;
            }
            
            // Instantiate the creature prefab at the specified position
            GameObject creatureObj = ObjectPoolManager.SpawnObject(prefab, position, Quaternion.identity);
            Creature creatureComponent = creatureObj.GetComponent<Creature>();
            
            if (creatureComponent == null)
            {
                Debug.LogError("Prefab does not have a Creature component");
                ObjectPoolManager.ReturnObjectToPool(creatureObj);
                return null;
            }
            
            // Set the type from our override or the file
            creatureComponent.type = savedCreature.type;
            
            // Copy properties from saved creature
            creatureComponent.health = savedCreature.health;
            creatureComponent.maxHealth = savedCreature.maxHealth;
            creatureComponent.energyMeter = savedCreature.energyMeter;
            creatureComponent.maxEnergy = savedCreature.maxEnergy;
            creatureComponent.Lifetime = savedCreature.lifetime;
            creatureComponent.generation = savedCreature.generation;
            creatureComponent.moveSpeed = savedCreature.moveSpeed;
            creatureComponent.pushForce = savedCreature.pushForce;
            creatureComponent.closeRange = savedCreature.closeRange;
            creatureComponent.bowRange = savedCreature.bowRange;
            creatureComponent.actionEnergyCost = savedCreature.actionEnergyCost;
            creatureComponent.chopDamage = savedCreature.chopDamage;
            creatureComponent.swordDamage = savedCreature.swordDamage;
            
            // Copy neural network parameters
            creatureComponent.weightMutationRate = savedCreature.weightMutationRate;
            creatureComponent.mutationRange = savedCreature.mutationRange;
            creatureComponent.addNodeRate = savedCreature.addNodeRate;
            creatureComponent.addConnectionRate = savedCreature.addConnectionRate;
            creatureComponent.deleteConnectionRate = savedCreature.deleteConnectionRate;
            creatureComponent.maxHiddenLayers = savedCreature.maxHiddenLayers;
            
            // Set up the neural network
            if (savedCreature.brain != null)
            {
                try
                {
                    // Reconstruct the neural network from the saved brain data
                    var nodes = new Dictionary<int, NEAT.Genes.NodeGene>();
                    var connections = new Dictionary<int, NEAT.Genes.ConnectionGene>();
                    
                    // Create nodes
                    foreach (var nodeData in savedCreature.brain.nodes)
                    {
                        var nodeType = (NEAT.Genes.NodeType)nodeData.type;
                        var node = new NEAT.Genes.NodeGene(nodeData.key, nodeType);
                        node.Layer = nodeData.layer;
                        node.Bias = nodeData.bias;
                        nodes.Add(nodeData.key, node);
                    }
                    
                    // Create connections
                    foreach (var connData in savedCreature.brain.connections)
                    {
                        var conn = new NEAT.Genes.ConnectionGene(
                            connData.key,
                            connData.inputKey,
                            connData.outputKey,
                            connData.weight
                        );
                        conn.Enabled = connData.enabled;
                        connections.Add(connData.key, conn);
                    }
                    
                    // Create the network
                    var network = new NEAT.NN.FeedForwardNetwork(nodes, connections);
                    creatureComponent.InitializeNetwork(network);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error reconstructing neural network: {e.Message}");
                    
                    // Fall back to a randomized brain
                    var genome = CreateInitialGenome();
                    var network = NEAT.NN.FeedForwardNetwork.Create(genome);
                    creatureComponent.InitializeNetwork(network);
                }
            }
            else
            {
                // If no brain data, create a randomized brain
                Debug.LogWarning("No brain data found in saved creature, creating a randomized brain");
                var genome = CreateInitialGenome();
                var network = NEAT.NN.FeedForwardNetwork.Create(genome);
                creatureComponent.InitializeNetwork(network);
            }
            
            return creatureComponent;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading creature from {filePath}: {e.Message}");
            return null;
        }
    }
} 