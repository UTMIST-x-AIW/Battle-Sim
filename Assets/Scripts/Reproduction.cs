using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NEAT.Genes;
using System.Linq;
using Unity.VisualScripting;

public class Reproduction : MonoBehaviour
{
    [SerializeField]float radius_of_mating = 3f;
    //public Collider2D circle_of_mating;
    public List<GameObject> gameObject_mated_with = new List<GameObject>();
    public float pReproduction = 0.9f;

    private void LateUpdate()
    {

        Collider2D[] nearbycollider = Physics2D.OverlapCircleAll(transform.position, radius_of_mating);
        Collider2D collider = nearbycollider[0];
        EnableMating(collider);
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
        //Debug.Log(name + " is mating with " + other.name);

        // Add each other to the mated lists
        gameObject_mated_with.Add(other);

        Reproduction otherScript = other.GetComponent<Reproduction>();

        float matingChance = Random.value;
        if (matingChance > pReproduction)
        {
            Instantiate(this);

            otherScript.gameObject_mated_with.Add(this.gameObject);
        }

    }

    


    // Creating a child Network
    private NEAT.NN.FeedForwardNetwork CreateChildNetwork(NEAT.NN.FeedForwardNetwork parent1, NEAT.NN.FeedForwardNetwork parent2)
    {
        // Get parent network details via reflection
        System.Reflection.FieldInfo nodesField = parent1.GetType().GetField("_nodes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        System.Reflection.FieldInfo connectionsField = parent1.GetType().GetField("_connections", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (nodesField == null || connectionsField == null)
        {
            Debug.LogError("Failed to access network fields via reflection");
            return null;
        }

        var parent1Nodes = nodesField.GetValue(parent1) as Dictionary<int, NEAT.Genes.NodeGene>;
        var parent1Connections = connectionsField.GetValue(parent1) as Dictionary<int, NEAT.Genes.ConnectionGene>;

        var parent2Nodes = nodesField.GetValue(parent2) as Dictionary<int, NEAT.Genes.NodeGene>;
        var parent2Connections = connectionsField.GetValue(parent2) as Dictionary<int, NEAT.Genes.ConnectionGene>;

        if (parent1Nodes == null || parent1Connections == null || parent2Nodes == null || parent2Connections == null)
        {
            Debug.LogError("Failed to extract network components");
            return null;
        }

        // Create new dictionaries for the child
        var childNodes = new Dictionary<int, NEAT.Genes.NodeGene>();
        var childConnections = new Dictionary<int, NEAT.Genes.ConnectionGene>();

        // Add all nodes (taking randomly from either parent for matching nodes)
        var allNodeKeys = new HashSet<int>(parent1Nodes.Keys.Concat(parent2Nodes.Keys));
        foreach (var key in allNodeKeys)
        {
            if (parent1Nodes.ContainsKey(key) && parent2Nodes.ContainsKey(key))
            {
                // Both parents have this node, randomly choose one
                childNodes[key] = Random.value < 0.5f ?
                    (NEAT.Genes.NodeGene)parent1Nodes[key].Clone() :
                    (NEAT.Genes.NodeGene)parent2Nodes[key].Clone();
            }
            else if (parent1Nodes.ContainsKey(key))
            {
                // Only parent1 has this node
                childNodes[key] = (NEAT.Genes.NodeGene)parent1Nodes[key].Clone();
            }
            else
            {
                // Only parent2 has this node
                childNodes[key] = (NEAT.Genes.NodeGene)parent2Nodes[key].Clone();
            }
        }

        // Add connections (taking randomly from either parent for matching connections)
        var allConnectionKeys = new HashSet<int>(parent1Connections.Keys.Concat(parent2Connections.Keys));
        foreach (var key in allConnectionKeys)
        {
            if (parent1Connections.ContainsKey(key) && parent2Connections.ContainsKey(key))
            {
                // Both parents have this connection, randomly choose one
                childConnections[key] = Random.value < 0.5f ?
                    (NEAT.Genes.ConnectionGene)parent1Connections[key].Clone() :
                    (NEAT.Genes.ConnectionGene)parent2Connections[key].Clone();
            }
            else if (parent1Connections.ContainsKey(key))
            {
                // Only parent1 has this connection
                childConnections[key] = (NEAT.Genes.ConnectionGene)parent1Connections[key].Clone();
            }
            else
            {
                // Only parent2 has this connection
                childConnections[key] = (NEAT.Genes.ConnectionGene)parent2Connections[key].Clone();
            }

            // Apply mutation to weight (occasionally)
            if (Random.value < 0.8f)
            {
                var conn = childConnections[key];
                conn.Weight += Random.Range(-0.5f, 0.5f);
                conn.Weight = Mathf.Clamp((float)conn.Weight, -1f, 1f);
            }
        }

        // Create a new network with the crossover results
        return new NEAT.NN.FeedForwardNetwork(childNodes, childConnections);
    }
}
