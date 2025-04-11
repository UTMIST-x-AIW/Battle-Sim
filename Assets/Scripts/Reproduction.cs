using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NEAT.Genes;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Build.Content;

public class Reproduction : MonoBehaviour
{
    public List<GameObject> gameObject_mated_with = new List<GameObject>();
    public float pReproduction = 0.9f;
    public GameObject Reproduction_prefab;
    private bool isMating = false;
    private Creature creatureComponent;

    private void Start()
    {
        // Get the Creature component
        creatureComponent = GetComponent<Creature>();
    }

    private void LateUpdate()
    {
        // Skip if already in mating process
        if (isMating) return;

        // Skip if creature isn't ready to reproduce (meter not filled)
        if (creatureComponent == null || !creatureComponent.canStartReproducing) return;

        // Use the creature's vision range instead of fixed radius
        float detectionRadius = creatureComponent.visionRange;
        Collider2D[] nearbycollider = Physics2D.OverlapCircleAll(transform.position, detectionRadius);
        
        foreach (var collider in nearbycollider)
        {
            if (collider != null && collider.gameObject != gameObject)
            {
                EnableMating(collider);
            }
        }
    }

    private void EnableMating(Collider2D col)
    {
        // Skip if already in mating process
        if (isMating) return;
        
        GameObject other_character = col.gameObject;

        if (other_character == gameObject)
        {
            return;
        }

        // Check if this is a creature of the same type
        Creature otherCreature = other_character.GetComponent<Creature>();
        if (otherCreature == null || otherCreature.type != creatureComponent.type)
        {
            return;
        }

        // Check if we've mated before
        if (gameObject_mated_with.Contains(other_character))
        {
            return;
        }

        // Check if other creature is ready to reproduce
        if (!otherCreature.canStartReproducing)
        {
            return;
        }

        // New check for minimum age requirement (21 years)
        if (creatureComponent.Lifetime < 21f || otherCreature.Lifetime < 21f)
        {
            return; // At least one creature is too young
        }

        // Check if we're in each other's vision range (bidirectional check)
        float distanceBetween = Vector2.Distance(transform.position, other_character.transform.position);
        if (distanceBetween > creatureComponent.visionRange || distanceBetween > otherCreature.visionRange)
        {
            return; // Not in each other's vision range
        }

        // If we passed all checks, proceed with mating
        MateWith(other_character);
    }

    void MateWith(GameObject other)
    {
        // Validate input
        if (other == null)
        {
            return;
        }

        // Set mating flag
        isMating = true;

        // Add each other to the mated lists
        gameObject_mated_with.Add(other);

        Reproduction otherScript = other.GetComponent<Reproduction>();
        if (otherScript == null)
        {
            isMating = false;
            return;
        }

        float matingChance = Random.value;
        if (matingChance > pReproduction)
        {
            // Get reference to NEATTest
            var neatTest = FindObjectOfType<NEATTest>();
            if (neatTest != null && neatTest.CanReproduce())
            {
                // Get parent creatures
                Creature p1 = this.GetComponent<Creature>();
                Creature p2 = other.GetComponent<Creature>();

                if (p1 == null || p2 == null)
                {
                    isMating = false;
                    return;
                }

                // Log the mating event with ages
                if (LogManager.Instance != null)
                {
                    LogManager.LogMessage($"Mating between {p1.type} (Age: {p1.Lifetime:F1}, Gen: {p1.generation}) and {p2.type} (Age: {p2.Lifetime:F1}, Gen: {p2.generation})");
                }

                // Reset reproduction meter for both parents
                p1.reproductionMeter = 0f;
                p1.canStartReproducing = false;
                p2.reproductionMeter = 0f;
                p2.canStartReproducing = false;

                // Spawn child
                GameObject child = SpawnChild(p1, p2, Reproduction_prefab, (this.transform.position + other.transform.position) / 2);
                if (child == null)
                {
                    isMating = false;
                    return;
                }

                // Add to mated list
                otherScript.gameObject_mated_with.Add(this.gameObject);
            }
        }

        // Reset mating flag after a delay
        StartCoroutine(ResetMatingState());
    }

    private IEnumerator ResetMatingState()
    {
        // Wait a bit before allowing mating again
        yield return new WaitForSeconds(2f);
        isMating = false;
    }

    private GameObject SpawnChild(Creature p1, Creature p2, GameObject prefab, Vector3 position)
    {
        // Validate input creatures
        if (p1 == null || p2 == null)
        {
            return null;
        }

        // Get parent brains
        var brain1 = p1.GetBrain();
        var brain2 = p2.GetBrain();

        if (brain1 == null || brain2 == null)
        {
            return null;
        }

        // Create child network
        var childNetwork = CreateChildNetwork(brain1, brain2);
        if (childNetwork == null)
        {
            return null;
        }

        // Spawn the child creature
        var child = Instantiate(prefab, position, Quaternion.identity);
        var childCreature = child.GetComponent<Creature>();
        
        if (childCreature == null)
        {
            Destroy(child);
            return null;
        }

        // Initialize the child's network
        childCreature.InitializeNetwork(childNetwork);
        
        // Copy max hidden layers setting from parent
        childCreature.maxHiddenLayers = p1.maxHiddenLayers;

        // Set child's generation to max parent generation + 1
        childCreature.generation = Mathf.Max(p1.generation, p2.generation) + 1;

        return child;
    }

    private NEAT.NN.FeedForwardNetwork CreateChildNetwork(NEAT.NN.FeedForwardNetwork parent1, NEAT.NN.FeedForwardNetwork parent2)
    {
        // Validate input networks
        if (parent1 == null || parent2 == null)
        {
            return null;
        }

        // Create a new genome for the child
        var childGenome = new NEAT.Genome.Genome(0);
        
        // Get parent genomes with null checks
        var parent1Genome = parent1.GetGenome();
        var parent2Genome = parent2.GetGenome();

        if (parent1Genome == null || parent2Genome == null)
        {
            return null;
        }

        if (parent1Genome.Nodes == null || parent2Genome.Nodes == null || 
            parent1Genome.Connections == null || parent2Genome.Connections == null)
        {
            return null;
        }
        
        // First, ensure we copy all input and output nodes
        // These are essential nodes that should never be missing

        // Add all input nodes (0-12)
        for (int i = 0; i <= 12; i++)
        {
            // Check if either parent has this input node
            if (parent1Genome.Nodes.ContainsKey(i) && parent1Genome.Nodes[i].Type == NEAT.Genes.NodeType.Input)
            {
                childGenome.AddNode((NEAT.Genes.NodeGene)parent1Genome.Nodes[i].Clone());
            }
            else if (parent2Genome.Nodes.ContainsKey(i) && parent2Genome.Nodes[i].Type == NEAT.Genes.NodeType.Input)
            {
                childGenome.AddNode((NEAT.Genes.NodeGene)parent2Genome.Nodes[i].Clone());
            }
            else
            {
                // Create a new input node if neither parent has it
                var newNode = new NEAT.Genes.NodeGene(i, NEAT.Genes.NodeType.Input);
                newNode.Layer = 0;
                newNode.Bias = 0.0;
                childGenome.AddNode(newNode);
                
                if (LogManager.Instance != null)
                {
                    LogManager.LogMessage($"Input node {i} missing from both parents, creating new one");
                }
                else
                {
                    Debug.LogWarning($"Input node {i} missing from both parents, creating new one");
                }
            }
        }
        
        // Add all output nodes (17-20)
        for (int i = 17; i <= 20; i++)
        {
            // Check if either parent has this output node
            if (parent1Genome.Nodes.ContainsKey(i) && parent1Genome.Nodes[i].Type == NEAT.Genes.NodeType.Output)
            {
                childGenome.AddNode((NEAT.Genes.NodeGene)parent1Genome.Nodes[i].Clone());
            }
            else if (parent2Genome.Nodes.ContainsKey(i) && parent2Genome.Nodes[i].Type == NEAT.Genes.NodeType.Output)
            {
                childGenome.AddNode((NEAT.Genes.NodeGene)parent2Genome.Nodes[i].Clone());
            }
            else
            {
                // Create a new output node if neither parent has it
                var newNode = new NEAT.Genes.NodeGene(i, NEAT.Genes.NodeType.Output);
                newNode.Layer = 2;
                newNode.Bias = 0.0;
                childGenome.AddNode(newNode);
                
                if (LogManager.Instance != null)
                {
                    LogManager.LogMessage($"Output node {i} missing from both parents, creating new one");
                }
                else
                {
                    Debug.LogWarning($"Output node {i} missing from both parents, creating new one");
                }
            }
        }
        
        // Add remaining hidden nodes (taking randomly from either parent for matching nodes)
        var allNodeKeys = new HashSet<int>(parent1Genome.Nodes.Keys.Concat(parent2Genome.Nodes.Keys));
        foreach (var key in allNodeKeys)
        {
            // Skip input and output nodes which we've already handled
            if (key <= 12 || (key >= 17 && key <= 20))
            {
                continue;
            }
            
            if (parent1Genome.Nodes.ContainsKey(key) && parent2Genome.Nodes.ContainsKey(key))
            {
                // Both parents have this node, randomly choose one
                childGenome.AddNode(Random.value < 0.5f ?
                    (NEAT.Genes.NodeGene)parent1Genome.Nodes[key].Clone() :
                    (NEAT.Genes.NodeGene)parent2Genome.Nodes[key].Clone());
            }
            else if (parent1Genome.Nodes.ContainsKey(key))
            {
                // Only parent1 has this node
                childGenome.AddNode((NEAT.Genes.NodeGene)parent1Genome.Nodes[key].Clone());
            }
            else
            {
                // Only parent2 has this node
                childGenome.AddNode((NEAT.Genes.NodeGene)parent2Genome.Nodes[key].Clone());
            }
        }

        // Add connections (taking randomly from either parent for matching connections)
        var allConnectionKeys = new HashSet<int>(parent1Genome.Connections.Keys.Concat(parent2Genome.Connections.Keys));
        foreach (var key in allConnectionKeys)
        {
            // Make sure the connection's input and output nodes exist in the child
            var conn = parent1Genome.Connections.ContainsKey(key) ? 
                parent1Genome.Connections[key] : parent2Genome.Connections[key];
                
            if (!childGenome.Nodes.ContainsKey(conn.InputKey) || !childGenome.Nodes.ContainsKey(conn.OutputKey))
            {
                continue;
            }
            
            if (parent1Genome.Connections.ContainsKey(key) && parent2Genome.Connections.ContainsKey(key))
            {
                // Both parents have this connection, randomly choose one
                childGenome.AddConnection(Random.value < 0.5f ?
                    (NEAT.Genes.ConnectionGene)parent1Genome.Connections[key].Clone() :
                    (NEAT.Genes.ConnectionGene)parent2Genome.Connections[key].Clone());
            }
            else if (parent1Genome.Connections.ContainsKey(key))
            {
                // Only parent1 has this connection
                childGenome.AddConnection((NEAT.Genes.ConnectionGene)parent1Genome.Connections[key].Clone());
            }
            else
            {
                // Only parent2 has this connection
                childGenome.AddConnection((NEAT.Genes.ConnectionGene)parent2Genome.Connections[key].Clone());
            }
        }

        // Ensure all output nodes have at least one connection
        for (int i = 17; i <= 20; i++)
        {
            bool hasConnection = childGenome.Connections.Values
                .Any(c => c.OutputKey == i && c.Enabled);
                
            if (!hasConnection)
            {
                // Find an input node to connect to this output
                for (int j = 0; j <= 12; j++)
                {
                    if (childGenome.Nodes.ContainsKey(j))
                    {
                        int connKey = childGenome.Connections.Count;
                        childGenome.AddConnection(new NEAT.Genes.ConnectionGene(
                            connKey, j, i, Random.Range(-1f, 1f)));
                        break;
                    }
                }
            }
        }

        // Apply mutations to the child genome
        ApplyMutations(childGenome);
        
        // Create a new network from the child genome
        return NEAT.NN.FeedForwardNetwork.Create(childGenome);
    }

    private void ApplyMutations(NEAT.Genome.Genome genome)
    {
        // Mutation probabilities - increased for more frequent mutations
        const float ADD_NODE_PROB = 0.3f;        // Was 0.1f
        const float DELETE_NODE_PROB = 0.2f;     // Was 0.05f
        const float MODIFY_BIAS_PROB = 0.4f;     // Was 0.2f
        const float ADD_CONNECTION_PROB = 0.4f;  // Was 0.2f
        const float DELETE_CONNECTION_PROB = 0.2f; // Was 0.1f
        const float MODIFY_WEIGHT_PROB = 0.6f;   // Was 0.3f

        // 1. Add new node mutation
        if (Random.value < ADD_NODE_PROB && genome.Connections.Count > 0)
        {
            // Pick a random connection to split
            var connList = new List<NEAT.Genes.ConnectionGene>(genome.Connections.Values);
            var connToSplit = connList[Random.Range(0, connList.Count)];
            
            // Create new node
            int newNodeKey = genome.Nodes.Count;
            var newNode = new NEAT.Genes.NodeGene(newNodeKey, NEAT.Genes.NodeType.Hidden);
            
            // Set layer between input and output nodes
            var inputNode = genome.Nodes[connToSplit.InputKey];
            var outputNode = genome.Nodes[connToSplit.OutputKey];
            newNode.Layer = (inputNode.Layer + outputNode.Layer) / 2;
            
            // If the layer would be the same as the input layer, increment it
            if (newNode.Layer <= inputNode.Layer)
            {
                newNode.Layer = inputNode.Layer + 1;
            }
            
            // Disable the original connection
            connToSplit.Enabled = false;
            
            // Add the new node
            genome.AddNode(newNode);
            
            // Add two new connections
            var conn1 = new NEAT.Genes.ConnectionGene(
                genome.Connections.Count,
                connToSplit.InputKey,
                newNodeKey,
                1.0);
                
            var conn2 = new NEAT.Genes.ConnectionGene(
                genome.Connections.Count + 1,
                newNodeKey,
                connToSplit.OutputKey,
                connToSplit.Weight);
                
            genome.AddConnection(conn1);
            genome.AddConnection(conn2);
        }

        // 2. Delete node mutation
        if (Random.value < DELETE_NODE_PROB)
        {
            // Get all hidden nodes (ONLY hidden nodes - never delete input or output nodes)
            var hiddenNodes = genome.Nodes.Values
                .Where(n => n.Type == NEAT.Genes.NodeType.Hidden)
                .ToList();
                
            if (hiddenNodes.Count > 0)
            {
                // Pick a random hidden node to delete
                var nodeToDelete = hiddenNodes[Random.Range(0, hiddenNodes.Count)];
                
                // Remove all connections to/from this node
                var connectionsToRemove = genome.Connections.Values
                    .Where(c => c.InputKey == nodeToDelete.Key || c.OutputKey == nodeToDelete.Key)
                    .ToList();
                    
                foreach (var conn in connectionsToRemove)
                {
                    genome.Connections.Remove(conn.Key);
                }
                
                // Remove the node
                genome.Nodes.Remove(nodeToDelete.Key);
            }
        }

        // 3. Modify bias mutation
        if (Random.value < MODIFY_BIAS_PROB)
        {
            // Pick a random node
            var nodeList = new List<NEAT.Genes.NodeGene>(genome.Nodes.Values);
            var nodeToModify = nodeList[Random.Range(0, nodeList.Count)];
            
            // Modify bias by a small random amount
            nodeToModify.Bias += Random.Range(-0.5f, 0.5f);
        }

        // 4. Add connection mutation
        if (Random.value < ADD_CONNECTION_PROB)
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

        // 5. Delete connection mutation
        if (Random.value < DELETE_CONNECTION_PROB && genome.Connections.Count > 1)
        {
            // Only consider connections that don't connect to input or output nodes for deletion
            var deletableConnections = genome.Connections.Values
                .Where(c => 
                    // Don't delete connections to output nodes
                    (!genome.Nodes.ContainsKey(c.OutputKey) || 
                     genome.Nodes[c.OutputKey].Type != NEAT.Genes.NodeType.Output) &&
                    // Don't delete connections from input nodes
                    (!genome.Nodes.ContainsKey(c.InputKey) || 
                     genome.Nodes[c.InputKey].Type != NEAT.Genes.NodeType.Input))
                .ToList();
            
            if (deletableConnections.Count > 0)
            {
                // Pick a random connection to delete that doesn't affect input or output nodes
                var connToDelete = deletableConnections[Random.Range(0, deletableConnections.Count)];
                genome.Connections.Remove(connToDelete.Key);
            }
        }

        // 6. Modify weight mutation
        if (Random.value < MODIFY_WEIGHT_PROB)
        {
            // Pick a random connection
            var connList = new List<NEAT.Genes.ConnectionGene>(genome.Connections.Values);
            var connToModify = connList[Random.Range(0, connList.Count)];
            
            // Modify weight by a small random amount
            connToModify.Weight += Random.Range(-0.5f, 0.5f);
            
            // Clamp weight to valid range
            connToModify.Weight = Mathf.Clamp((float)connToModify.Weight, -1f, 1f);
        }
        
        // 7. Validation: Ensure all input and output nodes exist
        EnsureRequiredNodes(genome);
    }

    // Updated method to ensure all required nodes exist in the genome
    private void EnsureRequiredNodes(NEAT.Genome.Genome genome)
    {
        // Ensure all input nodes (0-12) exist
        for (int i = 0; i <= 12; i++)
        {
            // If the input node doesn't exist, recreate it
            if (!genome.Nodes.ContainsKey(i) || genome.Nodes[i].Type != NEAT.Genes.NodeType.Input)
            {
                if (LogManager.Instance != null)
                {
                    LogManager.LogMessage($"Missing input node {i} detected during mutation. Recreating it.");
                }
                else
                {
                    Debug.LogWarning($"Missing input node {i} detected during mutation. Recreating it.");
                }
                
                // Create the input node
                var inputNode = new NEAT.Genes.NodeGene(i, NEAT.Genes.NodeType.Input);
                inputNode.Layer = 0;
                inputNode.Bias = 0.0;
                
                // If it already exists but with wrong type, remove it first
                if (genome.Nodes.ContainsKey(i))
                {
                    genome.Nodes.Remove(i);
                }
                
                // Add the input node
                genome.AddNode(inputNode);
                
                // Connect this input to at least one output to ensure it's used
                for (int j = 17; j <= 20; j++)
                {
                    if (genome.Nodes.ContainsKey(j) && genome.Nodes[j].Type == NEAT.Genes.NodeType.Output)
                    {
                        int connKey = genome.Connections.Count;
                        genome.AddConnection(new NEAT.Genes.ConnectionGene(
                            connKey,
                            i,
                            j,
                            Random.Range(-1f, 1f)));
                        break;
                    }
                }
            }
        }
        
        // Check for output nodes 17-20 (existing code)
        for (int i = 17; i <= 20; i++)
        {
            // If the output node doesn't exist, recreate it
            if (!genome.Nodes.ContainsKey(i) || genome.Nodes[i].Type != NEAT.Genes.NodeType.Output)
            {
                if (LogManager.Instance != null)
                {
                    LogManager.LogMessage($"Missing output node {i} detected during mutation. Recreating it.");
                }
                else
                {
                    Debug.LogWarning($"Missing output node {i} detected during mutation. Recreating it.");
                }
                
                // Create the output node
                var outputNode = new NEAT.Genes.NodeGene(i, NEAT.Genes.NodeType.Output);
                outputNode.Layer = 2;
                outputNode.Bias = 0.0;
                
                // If it already exists but with wrong type, remove it first
                if (genome.Nodes.ContainsKey(i))
                {
                    genome.Nodes.Remove(i);
                }
                
                // Add the output node
                genome.AddNode(outputNode);
                
                // Add at least one connection to this output node to ensure it's used
                // Find the first available input node and connect to it
                var inputNode = genome.Nodes.Values.FirstOrDefault(n => n.Type == NEAT.Genes.NodeType.Input);
                if (inputNode != null)
                {
                    int connKey = genome.Connections.Count;
                    genome.AddConnection(new NEAT.Genes.ConnectionGene(
                        connKey,
                        inputNode.Key,
                        i,
                        Random.Range(-1f, 1f)));
                }
            }
        }
        
        // Ensure all output nodes have at least one incoming connection
        for (int i = 17; i <= 20; i++)
        {
            bool hasConnection = genome.Connections.Values
                .Any(c => c.OutputKey == i && c.Enabled);
                
            if (!hasConnection)
            {
                if (LogManager.Instance != null)
                {
                    LogManager.LogMessage($"Output node {i} has no connections. Adding one.");
                }
                else
                {
                    Debug.LogWarning($"Output node {i} has no connections. Adding one.");
                }
                
                // Find an appropriate node to connect to this output
                var inputNode = genome.Nodes.Values.FirstOrDefault(n => n.Type == NEAT.Genes.NodeType.Input);
                if (inputNode != null)
                {
                    int connKey = genome.Connections.Count;
                    genome.AddConnection(new NEAT.Genes.ConnectionGene(
                        connKey,
                        inputNode.Key,
                        i,
                        Random.Range(-1f, 1f)));
                }
            }
        }
        
        // Ensure all input nodes have at least one outgoing connection
        for (int i = 0; i <= 12; i++)
        {
            bool hasConnection = genome.Connections.Values
                .Any(c => c.InputKey == i && c.Enabled);
                
            if (!hasConnection)
            {
                if (LogManager.Instance != null)
                {
                    LogManager.LogMessage($"Input node {i} has no connections. Adding one.");
                }
                else
                {
                    Debug.LogWarning($"Input node {i} has no connections. Adding one.");
                }
                
                // Connect to an output node
                for (int j = 17; j <= 20; j++)
                {
                    if (genome.Nodes.ContainsKey(j) && genome.Nodes[j].Type == NEAT.Genes.NodeType.Output)
                    {
                        int connKey = genome.Connections.Count;
                        genome.AddConnection(new NEAT.Genes.ConnectionGene(
                            connKey,
                            i,
                            j,
                            Random.Range(-1f, 1f)));
                        break;
                    }
                }
            }
        }
    }
}
