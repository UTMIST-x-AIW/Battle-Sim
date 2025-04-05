using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NEAT.Genes;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Build.Content;

public class Reproduction : MonoBehaviour
{
    [SerializeField]float radius_of_mating = 3f;
    //public Collider2D circle_of_mating;
    public List<GameObject> gameObject_mated_with = new List<GameObject>();
    public float pReproduction = 0.9f;
    public GameObject Reproduction_prefab;
    private bool isMating = false;

    private void LateUpdate()
    {
        // Skip if already in mating process
        if (isMating) return;

        Collider2D[] nearbycollider = Physics2D.OverlapCircleAll(transform.position, radius_of_mating);
        if (nearbycollider != null || nearbycollider.Length > 0)
        {
            Collider2D collider = nearbycollider[0];
            if (collider != null)
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
        string other_character_name = other_character.name.Substring(0, other_character.name.Length - 7);
        if (!gameObject_mated_with.Contains(other_character) && other_character_name == this.name.Substring(0, name.Length - 7))
        {
            MateWith(other_character);
        }
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
        
        // Add all nodes (taking randomly from either parent for matching nodes)
        var allNodeKeys = new HashSet<int>(parent1Genome.Nodes.Keys.Concat(parent2Genome.Nodes.Keys));
        foreach (var key in allNodeKeys)
        {
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
            // Get all hidden nodes
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
            var connList = new List<NEAT.Genes.ConnectionGene>(genome.Connections.Values);
            var connToDelete = connList[Random.Range(0, connList.Count)];
            genome.Connections.Remove(connToDelete.Key);
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
    }
}
