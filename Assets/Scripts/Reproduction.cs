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
        //Debug.Log(name + " is mating with " + other.name);

        // Add each other to the mated lists
        gameObject_mated_with.Add(other);

        Reproduction otherScript = other.GetComponent<Reproduction>();

        float matingChance = Random.value;
        if (matingChance > pReproduction)
        {
            if (NEATTest.num_alberts < MaxCreatures)
            {
                Reproduction newObj = Instantiate(this);
                Creature p1 = this.GetComponent<Creature>();
                Creature p2 = other.GetComponent<Creature>();
                GameObject child = SpawnChild(p1, p2, Reproduction_prefab, (this.transform.position + other.transform.position) / 2);
                //Debug.Log("HAAALEELUUJAH");
            }

            otherScript.gameObject_mated_with.Add(this.gameObject);
        }

    }

    private GameObject SpawnChild(Creature p1, Creature p2, GameObject prefab, Vector3 position)
    {
        var creature = Instantiate(prefab, position, Quaternion.identity);
        var creatureComponent = creature.GetComponent<Creature>();

        // Create initial neural network with appropriate genome
        NEAT.NN.FeedForwardNetwork network = CreateChildNetwork(p1.GetBrain(), p2.GetBrain());
        creatureComponent.InitializeNetwork(network);

        // Pass the max hidden layers setting to the creature
        creatureComponent.maxHiddenLayers = p1.maxHiddenLayers;

        NEATTest.num_alberts++;

        return creature;
    }




    // Creating a child Network
    private NEAT.NN.FeedForwardNetwork CreateChildNetwork(NEAT.NN.FeedForwardNetwork parent1, NEAT.NN.FeedForwardNetwork parent2)
    {
        // Create a new genome for the child
        var childGenome = new NEAT.Genome.Genome(0);
        
        // Create input nodes (13 inputs)
        for (int i = 0; i < 13; i++)
        {
            var node = new NEAT.Genes.NodeGene(i, NEAT.Genes.NodeType.Input);
            node.Layer = 0;  // Input layer
            node.Bias = 0.0; // Explicitly set bias to 0 for input nodes
            childGenome.AddNode(node);
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

        // Explicitly set bias to 0 for output nodes
        outputNode1.Bias = 0.0;
        outputNode2.Bias = 0.0;
        outputNode3.Bias = 0.0;
        outputNode4.Bias = 0.0;
        outputNode5.Bias = 0.0;

        childGenome.AddNode(outputNode1);
        childGenome.AddNode(outputNode2);
        childGenome.AddNode(outputNode3);
        childGenome.AddNode(outputNode4);
        childGenome.AddNode(outputNode5);

        // Add connections with random weights
        for(int i = 0; i < 13; i++)
        {
            for (int j = 17; j < 22; j++)
            {
                // Randomly choose weight from either parent or create a new one
                float weight;
                if (Random.value < 0.5f)
                {
                    // Use a completely random weight
                    weight = Random.Range(-1f, 1f);
                }
                else
                {
                    // Blend weights from parents (if they exist)
                    float weight1 = Random.Range(-1f, 1f);
                    float weight2 = Random.Range(-1f, 1f);
                    weight = (weight1 + weight2) / 2f;
                }
                
                childGenome.AddConnection(new NEAT.Genes.ConnectionGene((i*5 + j + 22), i, j, weight));
            }
        }
        
        // Create a new network from the child genome
        return NEAT.NN.FeedForwardNetwork.Create(childGenome);
    }
}
