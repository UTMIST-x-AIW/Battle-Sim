using UnityEngine;
using System.Collections;

public class NEATTest : MonoBehaviour
{
    public GameObject albertCreaturePrefab;  // Assign in inspector
    public GameObject kaiCreaturePrefab;  // Assign in inspector


    private GameObject mainAlbert;
    private Creature mainAlbertCreature;
    private CreatureObserver observer;
    private const float MAX_WAIT_TIME = 5f; // Maximum time to wait for initialization
    private bool hasLoggedObservations = false;
    
    // Expected values for our complex test case
    private readonly float[] expectedObservations = new float[] {
        3.0f,   // Health
        5.0f,   // Energy
        0.0f,   // Reproduction
        0.0f,   // Same type magnitude (no other Alberts)
        0.0f,   // Same type direction (no other Alberts)
        0.0f,   // Same type absolute sum (no other Alberts)
        3.0f,   // Different type magnitude (one Kai 3 units away)
        0.0f,   // Different type direction (Kai at 0 degrees)
        3.0f    // Different type absolute sum
    };
    
    void Start()
    {
        SetupTest();
        StartCoroutine(WaitForInitialization());
    }
    
    IEnumerator WaitForInitialization()
    {
        float waitTime = 0f;
        
        // Wait until observer is initialized or timeout
        while (observer == null && waitTime < MAX_WAIT_TIME)
        {
            observer = mainAlbert.GetComponent<CreatureObserver>();
            waitTime += Time.deltaTime;
            yield return null;
        }
        
        if (observer == null)
        {
            Debug.LogError("Failed to initialize observer after " + MAX_WAIT_TIME + " seconds");
            yield break;
        }
        
        RunTest();
    }
    
    void SetupTest()
    {
        // Spawn main Albert (our test subject)
        mainAlbert = Instantiate(albertCreaturePrefab, Vector3.zero, Quaternion.identity);
        mainAlbertCreature = mainAlbert.GetComponent<Creature>();
        mainAlbertCreature.type = Creature.CreatureType.Albert;
        
        // Spawn one Kai
        SpawnCreature(kaiCreaturePrefab, new Vector3(3, 0, 0), Creature.CreatureType.Kai);  // 3 units to the right
        
        // Create test neural network with fixed weights
        var genome = CreateTestGenome();
        var network = NEAT.NN.FeedForwardNetwork.Create(genome);
        mainAlbertCreature.InitializeNetwork(network);
    }
    
    private void SpawnCreature(GameObject prefab, Vector3 position, Creature.CreatureType type)
    {
        var creature = Instantiate(prefab, position, Quaternion.identity);
        var creatureComponent = creature.GetComponent<Creature>();
        creatureComponent.type = type;
    }
    
    NEAT.Genome.Genome CreateTestGenome()
    {
        var genome = new NEAT.Genome.Genome(0);
        
        // Add input nodes (11 inputs):
        // 0: health
        // 1: energy
        // 2: reproduction
        // 3,4: same type x,y
        // 5: same type count
        // 6,7: opposite type x,y
        // 8: opposite type count
        // 9,10: current direction x,y
        for (int i = 0; i < 11; i++)
        {
            genome.AddNode(new NEAT.Genes.NodeGene(i, NEAT.Genes.NodeType.Input));
        }
        
        // Add output nodes (x,y velocity)
        genome.AddNode(new NEAT.Genes.NodeGene(11, NEAT.Genes.NodeType.Output));  // Horizontal velocity
        genome.AddNode(new NEAT.Genes.NodeGene(12, NEAT.Genes.NodeType.Output));  // Vertical velocity
        
        // Add some basic connections with fixed weights
        // Health to horizontal velocity
        genome.AddConnection(new NEAT.Genes.ConnectionGene(0, 0, 11, -0.5f));
        // Energy to vertical velocity
        genome.AddConnection(new NEAT.Genes.ConnectionGene(1, 1, 12, -0.5f));
        // Same type x position to horizontal velocity
        genome.AddConnection(new NEAT.Genes.ConnectionGene(2, 3, 11, 0.5f));
        // Same type y position to vertical velocity
        genome.AddConnection(new NEAT.Genes.ConnectionGene(3, 4, 12, 0.5f));
        
        return genome;
    }
    
    void RunTest()
    {
        if (observer == null)
        {
            Debug.LogError("Observer not initialized yet!");
            return;
        }
        
        if (!hasLoggedObservations)
        {
            Debug.Log("Starting complex test scenario observations...");
            
            float[] observations = observer.GetObservations(mainAlbertCreature);
            
            bool observationsCorrect = VerifyObservations(observations);
            Debug.Log(string.Format("Observations Test: {0}", observationsCorrect ? "PASSED" : "FAILED"));
            LogObservations(observations);
            
            hasLoggedObservations = true;
        }
        
        // Run one neural network step
        float[] actions = mainAlbertCreature.GetActions();
        
        // Verify actions are in correct range (-1 to 1)
        bool actionsInRange = VerifyActionRanges(actions);
        if (!hasLoggedObservations)
        {
            Debug.Log(string.Format("Action Ranges Test: {0}", actionsInRange ? "PASSED" : "FAILED"));
            LogActions(actions);
        }
        
        // Let it run for 2 seconds
        StartCoroutine(RunMovementTest());
    }
    
    bool VerifyObservations(float[] observations)
    {
        if (observations.Length != expectedObservations.Length) return false;
        
        for (int i = 0; i < observations.Length; i++)
        {
            if (Mathf.Abs(observations[i] - expectedObservations[i]) > 0.1f)
                return false;
        }
        
        return true;
    }
    
    bool VerifyActionRanges(float[] actions)
    {
        return actions.Length == 2 &&
               actions[0] >= -1f && actions[0] <= 1f &&
               actions[1] >= -1f && actions[1] <= 1f;
    }
    
    void LogObservations(float[] observations)
    {
        string log = "Observations:\n";
        for (int i = 0; i < observations.Length; i++)
        {
            log += string.Format("[{0}] Expected: {1:F2}, Got: {2:F2}\n", i, expectedObservations[i], observations[i]);
        }
        Debug.Log(log);
    }
    
    void LogActions(float[] actions)
    {
        Debug.Log(string.Format("Actions: Forward = {0:F2}, Angular = {1:F2}", actions[0], actions[1]));
    }
    
    IEnumerator RunMovementTest()
    {
        float initialEnergy = mainAlbertCreature.energy;
        Vector3 initialPosition = mainAlbert.transform.position;
        
        yield return new WaitForSeconds(2.0f);
        
        // Verify energy depletion
        bool energyDepleted = mainAlbertCreature.energy < initialEnergy;
        Debug.Log(string.Format("Energy Depletion Test: {0}", energyDepleted ? "PASSED" : "FAILED"));
        Debug.Log(string.Format("Energy: {0:F2} (Started at {1:F2})", mainAlbertCreature.energy, initialEnergy));
        
        // Verify position changed
        bool positionChanged = mainAlbert.transform.position != initialPosition;
        Debug.Log(string.Format("Movement Test: {0}", positionChanged ? "PASSED" : "FAILED"));
        Debug.Log(string.Format("Position: {0} (Started at {1})", mainAlbert.transform.position, initialPosition));
    }
} 