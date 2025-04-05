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
    public int MaxCreatures = 100;
    public GameObject Reproduction_prefab;

    private void LateUpdate()
    {

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

        // Add each other to the mated lists
        gameObject_mated_with.Add(other);

        Reproduction otherScript = other.GetComponent<Reproduction>();
        if (otherScript == null)
        {
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
                    return;
                }

                // Spawn child
                GameObject child = SpawnChild(p1, p2, Reproduction_prefab, (this.transform.position + other.transform.position) / 2);
                if (child == null)
                {
                    return;
                }

                // Add to mated list
                otherScript.gameObject_mated_with.Add(this.gameObject);
            }
        }
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

        NEATTest.num_alberts++;

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
        var creature = GetComponent<Creature>();
        if (creature != null)
        {
            // Apply weight mutations
            foreach (var conn in childGenome.Connections.Values)
            {
                if (Random.value < creature.weightMutationRate)
                {
                    if (Random.value < 0.9f)
                    {
                        // Perturb weight
                        conn.Weight += Random.Range(-creature.mutationRange, creature.mutationRange);
                        conn.Weight = Mathf.Clamp((float)conn.Weight, -1f, 1f);
                    }
                    else
                    {
                        // Assign new random weight
                        conn.Weight = Random.Range(-1f, 1f);
                    }
                }
            }

            // Add node mutation
            if (Random.value < creature.addNodeRate && childGenome.Connections.Count > 0)
            {
                // Check if we've reached the maximum number of hidden layers
                int maxCurrentLayer = 0;
                foreach (var node in childGenome.Nodes.Values)
                {
                    if (node.Type == NEAT.Genes.NodeType.Hidden)
                    {
                        maxCurrentLayer = Mathf.Max(maxCurrentLayer, node.Layer);
                    }
                }
                
                // Only proceed if we haven't reached the max hidden layers
                if (maxCurrentLayer < creature.maxHiddenLayers + 1)
                {
                    // Original connection-splitting logic
                    var connList = new List<NEAT.Genes.ConnectionGene>(childGenome.Connections.Values);
                    var connToSplit = connList[Random.Range(0, connList.Count)];
                    connToSplit.Enabled = false;

                    // Create new node
                    int newNodeKey = childGenome.Nodes.Count;
                    var newNode = new NEAT.Genes.NodeGene(newNodeKey, NEAT.Genes.NodeType.Hidden);
                    
                    // Set layer between input and output nodes
                    var inputNode = childGenome.Nodes[connToSplit.InputKey];
                    var outputNode = childGenome.Nodes[connToSplit.OutputKey];
                    newNode.Layer = (inputNode.Layer + outputNode.Layer) / 2;
                    
                    // If the layer would be the same as the input layer, increment it
                    if (newNode.Layer <= inputNode.Layer)
                    {
                        // Check if incrementing would exceed max layers
                        if (inputNode.Layer + 1 >= creature.maxHiddenLayers + 1)
                        {
                            // Skip this mutation if it would exceed max layers
                            return NEAT.NN.FeedForwardNetwork.Create(childGenome);
                        }
                        newNode.Layer = inputNode.Layer + 1;
                    }
                    
                    childGenome.AddNode(newNode);

                    // Add two new connections
                    var conn1 = new NEAT.Genes.ConnectionGene(
                        childGenome.Connections.Count,
                        connToSplit.InputKey,
                        newNodeKey,
                        1.0);

                    var conn2 = new NEAT.Genes.ConnectionGene(
                        childGenome.Connections.Count + 1,
                        newNodeKey,
                        connToSplit.OutputKey,
                        connToSplit.Weight);

                    childGenome.AddConnection(conn1);
                    childGenome.AddConnection(conn2);
                }
            }

            // Add connection mutation
            if (Random.value < creature.addConnectionRate)
            {
                for (int tries = 0; tries < 5; tries++)
                {
                    var nodeList = new List<NEAT.Genes.NodeGene>(childGenome.Nodes.Values);
                    var sourceNode = nodeList[Random.Range(0, nodeList.Count)];
                    var targetNode = nodeList[Random.Range(0, nodeList.Count)];

                    if (sourceNode.Layer >= targetNode.Layer ||
                        sourceNode.Type == NEAT.Genes.NodeType.Output ||
                        targetNode.Type == NEAT.Genes.NodeType.Input)
                    {
                        continue;
                    }

                    bool exists = childGenome.Connections.Values.Any(c =>
                        c.InputKey == sourceNode.Key && c.OutputKey == targetNode.Key);

                    if (!exists)
                    {
                        var newConn = new NEAT.Genes.ConnectionGene(
                            childGenome.Connections.Count,
                            sourceNode.Key,
                            targetNode.Key,
                            Random.Range(-1f, 1f));

                        childGenome.AddConnection(newConn);
                        break;
                    }
                }
            }

            // Delete connection mutation
            if (Random.value < creature.deleteConnectionRate && childGenome.Connections.Count > 1)
            {
                var connList = new List<NEAT.Genes.ConnectionGene>(childGenome.Connections.Values);
                var connToDelete = connList[Random.Range(0, connList.Count)];
                childGenome.Connections.Remove(connToDelete.Key);
            }
        }
        
        // Create a new network from the child genome
        return NEAT.NN.FeedForwardNetwork.Create(childGenome);
    }
}
