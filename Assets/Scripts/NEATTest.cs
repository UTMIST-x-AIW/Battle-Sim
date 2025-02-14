using UnityEngine;
using System.Collections;

public class NEATTest : MonoBehaviour
{
    public GameObject creaturePrefab;  // Assign in inspector
    
    private GameObject albert;
    private GameObject kai;
    private Creature albertCreature;
    private CreatureObserver observer;
    private const float MAX_WAIT_TIME = 5f; // Maximum time to wait for initialization
    
    // Expected values for our test case
    private readonly float[] expectedObservations = new float[] {
        3.0f,  // Health
        5.0f,  // Energy
        0.0f,  // Reproduction
        0.0f,  // Same type magnitude (no other Alberts)
        0.0f,  // Same type direction (no other Alberts)
        0.0f,  // Same type absolute sum (no other Alberts)
        3.0f,  // Different type magnitude (Kai at (3,0))
        0.0f,  // Different type direction (Kai directly to right)
        3.0f   // Different type absolute sum
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
            observer = albert.GetComponent<CreatureObserver>();
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
        // Spawn test creatures
        albert = Instantiate(creaturePrefab, Vector3.zero, Quaternion.identity);
        kai = Instantiate(creaturePrefab, new Vector3(3, 0, 0), Quaternion.identity);
        
        // Setup components
        albertCreature = albert.GetComponent<Creature>();
        albertCreature.type = Creature.CreatureType.Albert;
        kai.GetComponent<Creature>().type = Creature.CreatureType.Kai;
        
        // Create test neural network with fixed weights
        var genome = CreateTestGenome();
        var network = NEAT.NN.FeedForwardNetwork.Create(genome);
        
        // Initialize Albert with test network
        albertCreature.InitializeNetwork(network);
    }
    
    NEAT.Genome.Genome CreateTestGenome()
    {
        var genome = new NEAT.Genome.Genome(0);
        
        // Add input nodes
        for (int i = 0; i < 9; i++)
        {
            genome.AddNode(new NEAT.Genes.NodeGene(i, NEAT.Genes.NodeType.Input));
        }
        
        // Add output nodes
        genome.AddNode(new NEAT.Genes.NodeGene(9, NEAT.Genes.NodeType.Output));  // Forward velocity
        genome.AddNode(new NEAT.Genes.NodeGene(10, NEAT.Genes.NodeType.Output)); // Angular velocity
        
        // Add some basic connections with fixed weights
        genome.AddConnection(new NEAT.Genes.ConnectionGene(0, 0, 9, 0.5f));   // Health to forward velocity
        genome.AddConnection(new NEAT.Genes.ConnectionGene(1, 1, 9, -0.5f));  // Energy to forward velocity
        genome.AddConnection(new NEAT.Genes.ConnectionGene(2, 6, 10, 0.5f));  // Opposite type magnitude to angular velocity
        
        return genome;
    }
    
    void RunTest()
    {
        if (observer == null)
        {
            Debug.LogError("Observer not initialized yet!");
            return;
        }
        
        Debug.Log("Starting test with initialized observer...");
        
        // Get observations
        float[] observations = observer.GetObservations(albertCreature);
        
        // Verify observations
        bool observationsCorrect = VerifyObservations(observations);
        Debug.Log(string.Format("Observations Test: {0}", observationsCorrect ? "PASSED" : "FAILED"));
        LogObservations(observations);
        
        // Run one neural network step
        float[] actions = albertCreature.GetActions();
        
        // Verify actions are in correct range (-1 to 1)
        bool actionsInRange = VerifyActionRanges(actions);
        Debug.Log(string.Format("Action Ranges Test: {0}", actionsInRange ? "PASSED" : "FAILED"));
        LogActions(actions);
        
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
        float initialEnergy = albertCreature.energy;
        Vector3 initialPosition = albert.transform.position;
        
        yield return new WaitForSeconds(2.0f);
        
        // Verify energy depletion
        bool energyDepleted = albertCreature.energy < initialEnergy;
        Debug.Log(string.Format("Energy Depletion Test: {0}", energyDepleted ? "PASSED" : "FAILED"));
        Debug.Log(string.Format("Energy: {0:F2} (Started at {1:F2})", albertCreature.energy, initialEnergy));
        
        // Verify position changed
        bool positionChanged = albert.transform.position != initialPosition;
        Debug.Log(string.Format("Movement Test: {0}", positionChanged ? "PASSED" : "FAILED"));
        Debug.Log(string.Format("Position: {0} (Started at {1})", albert.transform.position, initialPosition));
    }
} 