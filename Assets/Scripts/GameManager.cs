//TODO: bring back the object pool manager, i didnt even realize it was gone
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.IO;
using System;
// Explicitly use UnityEngine.Random to avoid ambiguity with System.Random
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    private static GameManager instance; //IMPROVEMENT: make this public instead, better for performance and readability

    [Header("Creature Prefabs")]
    public GameObject albertCreaturePrefab;  // Assign in inspector
    public GameObject kaiCreaturePrefab;    // Assign in inspector


    [Header("Albert Population Settings")]
    public int MinAlberts = 20;  // Minimum number of Alberts to maintain
    public int MaxAlberts = 100; // Maximum number of Alberts allowed
    public int InitialAlberts = 10; // Number of Alberts to spawn initially
    public float MinStartingAgeAlbert = 0f; // Minimum starting age for initial Alberts
    public float MaxStartingAgeAlbert = 5f; // Maximum starting age for initial Alberts

    [Header("Kai Population Settings")]
    public int MinKais = 20;  // Minimum number of Kais to maintain
    public int MaxKais = 100; // Maximum number of Kais allowed
    public int InitialKais = 10; // Number of Kais to spawn initially
    public float MinStartingAgeKai = 0f; // Minimum starting age for initial Kais
    public float MaxStartingAgeKai = 5f; // Maximum starting age for initial Kais

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
    public const int k_ObservationCount = 18;
    public const int k_ActionCount = 5;


    public enum CurrentRun
    {
        [InspectorName("New Run (From Scratch)")] NewRunFromScratch,
        [InspectorName("New Run (With Saved Creatures)")] NewRunWithSavedCreatures
    }

    [Header("Run Settings")]
    public CurrentRun currentRun;

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

    // Additional spawn area for Kais in the dual-species run
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
    private Queue<int> savedGenerationQueue = new Queue<int>();
    private const int maxSavedGenerationEntries = 100;

    // Add new variables for Kai spawning
    private float lastKaiSpawnTime = 0f;
    private bool isSpawningKai = false;

    private void Awake()
    {
        // Check if there's already an instance
        if (instance != null && instance != this) //IMPROVEMENT: i think we have a lot of null checks in the codebase that might be unnecessary like this one, can look into removing them if it doesn't cause issues
        {
            Debug.LogError($"Found duplicate GameManager on {gameObject.name}. There should only be one GameManager component in the scene!");
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

        // Debug.Log($"GameManager starting on GameObject: {gameObject.name}");

        // Clear any existing creatures first
        var existingCreatures = GameObject.FindObjectsOfType<Creature>();
        // Debug.Log($"Found {existingCreatures.Length} existing creatures to clean up");
        foreach (var creature in existingCreatures)
        {
            ObjectPoolManager.ReturnObjectToPool(creature.gameObject); //TODO-OBJECTPOOL: return to this after implementing reset
            // Destroy(creature.gameObject);
        }


        switch (currentRun)
        {
            case CurrentRun.NewRunFromScratch:
                SetupNewRunFromScratch();
                break;
            case CurrentRun.NewRunWithSavedCreatures:
                SetupNewRunWithSavedCreatures();
                break;
            default:
                SetupNewRunFromScratch();
                break;
        }

    }

    private void Update()
    {
        try
        {
            // Check for population management (only for specific runs)


            // Check current creature counts periodically
            countTimer += Time.deltaTime;
            if (countTimer >= 0.4f)
            {
                CountAlberts();
                CountKais();
                countTimer = 0f;
            }

            // Automatic population management
            ManagePopulation();


            // Testing controls
            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                currentRun = CurrentRun.NewRunFromScratch;
                RestartTest();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                currentRun = CurrentRun.NewRunWithSavedCreatures;
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
                LogManager.LogError($"CRITICAL ERROR in GameManager.Update: {e.Message}\nStack trace: {e.StackTrace}");
            }
            else
            {
                Debug.LogError($"CRITICAL ERROR in GameManager.Update: {e.Message}\nStack trace: {e.StackTrace}");
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
            if (currentAlberts < MinAlberts && Time.time - lastSpawnTime >= spawnCooldown && !isSpawning)
            {
                LogManager.LogMessage($"Albert population below minimum ({MinAlberts}). Spawning new Albert with staggered timing.");

                // Start a coroutine to spawn a new Albert with a random delay
                StartCoroutine(SpawnNewAlbertStaggered());
            }

            // If this is an Alberts vs Kais test, also manage Kai population
            if (currentRun == CurrentRun.NewRunFromScratch)
            {
                // Count current Kais
                int currentKais = CountKais();

                // Update the inspector-visible count
                CurrentKais = currentKais;

                // Log population count
                LogManager.LogMessage($"Current Kai population: {currentKais}");

                // Check if we need to spawn more Kais
                if (currentKais < MinKais && Time.time - lastKaiSpawnTime >= spawnCooldown && !isSpawningKai)
                {
                    LogManager.LogMessage($"Kai population below minimum ({MinKais}). Spawning new Kai with staggered timing.");

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
            float startingAge = Random.Range(MinStartingAgeAlbert, MaxStartingAgeAlbert);
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
            float startingAge = Random.Range(MinStartingAgeKai, MaxStartingAgeKai);
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
            return CurrentAlberts < MaxAlberts;
        }
        else if (type == Creature.CreatureType.Kai)
        {
            return CurrentKais < MaxKais;
        }
        return true; // Allow other types to reproduce without limit
    }

    // Keep the old method for backward compatibility
    public bool CanReproduce()
    {
        return CurrentAlberts < MaxAlberts;
    }

    private int CountKais()
    {
        return ObjectPoolManager.GetActiveChildCount(kaiCreaturePrefab);
    }

    NEAT.Genome.Genome CreateInitialGenome()
    {
        var genome = new NEAT.Genome.Genome(0);

        // Add input nodes
        for (int i = 0; i < k_ObservationCount; i++)
        {
            var node = new NEAT.Genes.NodeGene(i, NEAT.Genes.NodeType.Input);
            node.Layer = 0;  // Input layer
            node.Bias = 0.0; // Explicitly set bias to 0 for input nodes
            genome.AddNode(node);
        }

        // Add output nodes
        for (int i = k_ObservationCount; i < k_ObservationCount + k_ActionCount; i++)
        {
            var node = new NEAT.Genes.NodeGene(i, NEAT.Genes.NodeType.Output);
            node.Layer = 2;
            node.Bias = 0.0;
            genome.AddNode(node);
        }


        for (int i = 0; i < k_ObservationCount; i++)
        {
            for (int j = k_ObservationCount; j < k_ObservationCount + k_ActionCount; j++)
            {
                genome.AddConnection(new NEAT.Genes.ConnectionGene(genome.Connections.Count, i, j, Random.Range(-1f, 1f)));
            }
        }

        return genome;
    }

    // New method to spawn creatures with more randomized brains
    private Creature SpawnCreatureWithRandomizedBrain(GameObject prefab, Vector3 position, Creature.CreatureType type)
    {
        // Create the creature instance
        GameObject creature = ObjectPoolManager.SpawnObject(prefab, position, Quaternion.identity); //TODO-OBJECTPOOL: return to this after implementing reset
        // GameObject creature = Instantiate(prefab, position, Quaternion.identity);
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
            if (currentRun == CurrentRun.NewRunFromScratch)
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
        return ObjectPoolManager.GetActiveChildCount(albertCreaturePrefab);
    }

    private void OnDestroy()
    {
        try
        {
            Debug.Log($"GameManager OnDestroy called on {gameObject.name}");

            // Only clear the static instance if this is the instance being destroyed
            if (instance == this)
            {
                Debug.Log("GameManager: Main instance being destroyed");

                try
                {
                    // Clear static references in Creature class
                    Creature.ClearStaticReferences();
                    Debug.Log("GameManager: Successfully cleared Creature static references");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"GameManager: Error clearing Creature static references: {e.Message}");
                }

                // Clear the instance reference
                instance = null;

                // Call LogManager cleanup to prevent the GameObject from lingering
                // Use direct reference to LogManager methods without creating new instances
                if (LogManager.Instance != null)
                {
                    try
                    {
                        LogManager.LogMessage("GameManager instance has been destroyed and static references cleared.");
                        Debug.Log("GameManager: Successfully logged final message");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"GameManager: Error logging final message: {e.Message}");
                    }

                    try
                    {
                        // Call cleanup separately
                        LogManager.Cleanup();
                        Debug.Log("GameManager: LogManager cleanup completed");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"GameManager: Error during LogManager cleanup: {e.Message}");
                    }
                }
                else
                {
                    Debug.Log("GameManager: LogManager instance is null, skipping cleanup");
                }

                ObjectPoolManager.ClearPools();
            }
            else
            {
                Debug.Log("GameManager: Not the main instance, skipping cleanup");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"GameManager: Unhandled error in OnDestroy: {e.Message}\n{e.StackTrace}");
        }
    }

    private void RestartTest()
    {
        try
        {
            LogManager.LogMessage($"Restarting test {currentRun}...");

            // Clear any existing creatures
            var existingCreatures = GameObject.FindObjectsOfType<Creature>();
            LogManager.LogMessage($"Destroying {existingCreatures.Length} existing creatures");

            foreach (var creature in existingCreatures)
            {
                ObjectPoolManager.ReturnObjectToPool(creature.gameObject); //TODO-OBJECTPOOL: return to this after implementing reset
                // Destroy(creature.gameObject);
            }

            ObjectPoolManager.ClearPools();

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

            switch (currentRun)
            {
                case CurrentRun.NewRunFromScratch:
                    SetupNewRunFromScratch();
                    break;
                case CurrentRun.NewRunWithSavedCreatures:
                    SetupNewRunWithSavedCreatures();
                    break;
                default:
                    SetupNewRunFromScratch();
                    break;
            }

            LogManager.LogMessage($"Run {currentRun} restarted successfully");

        }
        catch (System.Exception e)
        {
            LogManager.LogError($"Error in RestartAfterCleanup: {e.Message}\nStack trace: {e.StackTrace}");
        }
    }


    private void SetupNewRunFromScratch()
    {
        Debug.Log("Starting Test: Alberts vs Kais - Battle Simulation");

        // Spawn Alberts 
        for (int i = 0; i < InitialAlberts; i++)
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
                float startingAge = Random.Range(MinStartingAgeAlbert, MaxStartingAgeAlbert);
                albert.Lifetime = startingAge;

                // Set starting reproduction meter to a random value
                albert.reproductionMeter = Random.Range(0f, 0.2f);

                // Set generation to 0 for initial Alberts
                albert.generation = 0;

                LogManager.LogMessage($"Spawned Albert {i + 1}/{InitialAlberts} at {position}, age: {startingAge:F1}");
            }
        }

        // Spawn Kais
        for (int i = 0; i < InitialKais; i++)
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
                float startingAge = Random.Range(MinStartingAgeKai, MaxStartingAgeKai);
                kai.Lifetime = startingAge;

                // Set starting reproduction meter to a random value
                kai.reproductionMeter = Random.Range(0f, 0.2f);

                // Set generation to 0 for initial Kais
                kai.generation = 0;

                LogManager.LogMessage($"Spawned Kai {i + 1}/{InitialKais} at {position}, age: {startingAge:F1}");
            }
        }

        // Update counts
        CountAlberts();
        CountKais();

        LogManager.LogMessage("Alberts vs Kais battle simulation setup complete!");
        LogManager.LogMessage($"Left side (Alberts): {InitialAlberts} creatures near {spawnCenter}");
        LogManager.LogMessage($"Right side (Kais): {InitialKais} creatures near {rightSpawnCenter}");
        LogManager.LogMessage($"Population management: MIN_ALBERTS={MinAlberts}, MAX_ALBERTS={MaxAlberts}");
        LogManager.LogMessage($"Population management: MIN_KAIS={MinKais}, MAX_KAIS={MaxKais}");
    }



    private void SetupNewRunWithSavedCreatures()
    {
        Debug.Log("Starting Test: Load Creatures");

        int albertsLoaded = 0;
        int kaisLoaded = 0;

        // Handle Alberts loading
        if (!string.IsNullOrEmpty(albertsFolderPath))
        {
            if (albertsFolderPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                // Single file - load one Albert
                Debug.Log($"Loading single Albert from file: {albertsFolderPath}");

                Vector2 offset = Random.insideUnitCircle * spawnSpreadRadius;
                Vector3 position = new Vector3(
                    spawnCenter.x + offset.x,
                    spawnCenter.y + offset.y,
                    0f
                );

                var creature = LoadCreatureFromFile(albertsFolderPath, albertCreaturePrefab, position, Creature.CreatureType.Albert);
                if (creature != null)
                {
                    albertsLoaded = 1;
                    Debug.Log($"Successfully loaded Albert from {System.IO.Path.GetFileName(albertsFolderPath)}");
                }
            }
            else
            {
                // Folder - load multiple Alberts
                Debug.Log($"Loading Alberts from folder: {albertsFolderPath}");
                albertsLoaded = LoadAndSpawnCreaturesFromFolder(
                    albertsFolderPath,
                    spawnCenter,
                    spawnSpreadRadius,
                    albertCreaturePrefab,
                    Creature.CreatureType.Albert,
                    InitialAlberts,
                    resampleCreatures,
                    respectTypeInFiles
                );
            }
        }

        // Handle Kais loading
        if (!string.IsNullOrEmpty(kaisFolderPath))
        {
            if (kaisFolderPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                // Single file - load one Kai
                Debug.Log($"Loading single Kai from file: {kaisFolderPath}");

                Vector2 offset = Random.insideUnitCircle * rightSpawnSpreadRadius;
                Vector3 position = new Vector3(
                    rightSpawnCenter.x + offset.x,
                    rightSpawnCenter.y + offset.y,
                    0f
                );

                var creature = LoadCreatureFromFile(kaisFolderPath, kaiCreaturePrefab, position, Creature.CreatureType.Kai);
                if (creature != null)
                {
                    kaisLoaded = 1;
                    Debug.Log($"Successfully loaded Kai from {System.IO.Path.GetFileName(kaisFolderPath)}");
                }
            }
            else
            {
                // Folder - load multiple Kais
                Debug.Log($"Loading Kais from folder: {kaisFolderPath}");
                kaisLoaded = LoadAndSpawnCreaturesFromFolder(
                    kaisFolderPath,
                    rightSpawnCenter,
                    rightSpawnSpreadRadius,
                    kaiCreaturePrefab,
                    Creature.CreatureType.Kai,
                    InitialKais,
                    resampleCreatures,
                    respectTypeInFiles
                );
            }
        }

        // Update creature counts
        CurrentAlberts = albertsLoaded;
        CurrentKais = kaisLoaded;

        Debug.Log($"Load Creatures Test Complete: Loaded {albertsLoaded} Alberts and {kaisLoaded} Kais");

        if (albertsLoaded == 0 && kaisLoaded == 0)
        {
            Debug.LogWarning("No creatures were loaded. Please check the file/folder paths.");
        }
        else if (albertsLoaded > 0 && kaisLoaded > 0)
        {
            Debug.Log("Battle setup ready with both Albert and Kai creatures!");
        }
        else if (albertsLoaded > 0)
        {
            Debug.Log("Single-species test ready with Albert creatures.");
        }
        else if (kaisLoaded > 0)
        {
            Debug.Log("Single-species test ready with Kai creatures.");
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
            savedGenerationQueue.Clear();
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
                savedGenerationQueue.Enqueue(generation);
                if (savedGenerations.Count > maxSavedGenerationEntries)
                {
                    int oldGen = savedGenerationQueue.Dequeue();
                    savedGenerations.Remove(oldGen);
                }
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
                attackDamage = creature.attackDamage,
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
                            Random.Range(MinStartingAgeAlbert, MaxStartingAgeAlbert) :
                            Random.Range(MinStartingAgeKai, MaxStartingAgeKai);

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
            GameObject creatureObj = ObjectPoolManager.SpawnObject(prefab, position, Quaternion.identity); //TODO-OBJECTPOOL: return to this after implementing reset
                                                                                                           // GameObject creatureObj = Instantiate(prefab, position, Quaternion.identity);
            Creature creatureComponent = creatureObj.GetComponent<Creature>();

            if (creatureComponent == null)
            {
                Debug.LogError("Prefab does not have a Creature component");
                ObjectPoolManager.ReturnObjectToPool(creatureObj); //TODO-OBJECTPOOL: return to this after implementing reset
                                                                   // Destroy(creatureObj);
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
            creatureComponent.attackDamage = savedCreature.attackDamage;

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