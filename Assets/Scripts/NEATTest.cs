using UnityEngine;

public class NEATTest : MonoBehaviour
{
    [Header("Creature Prefabs")]
    public GameObject albertCreaturePrefab;  // Assign in inspector
    public GameObject kaiCreaturePrefab;    // Assign in inspector
    
    [Header("Network Settings")]
    public int maxHiddenLayers = 10;  // Maximum number of hidden layers allowed
    
    void Start()
    {
        // Spawn one Albert near (-5, 5)
        Vector3 albertPosition = new Vector3(-5, 5, 0) + Random.insideUnitSphere * 2f;
        albertPosition.z = 0;  // Ensure z is 0
        SpawnCreature(albertCreaturePrefab, albertPosition, Creature.CreatureType.Albert);
        
        // Spawn one Kai near (5, -5)
        Vector3 kaiPosition = new Vector3(5, -5, 0) + Random.insideUnitSphere * 2f;
        kaiPosition.z = 0;  // Ensure z is 0
        SpawnCreature(kaiCreaturePrefab, kaiPosition, Creature.CreatureType.Kai);
    }
    
    private void SpawnCreature(GameObject prefab, Vector3 position, Creature.CreatureType type)
    {
        var creature = Instantiate(prefab, position, Quaternion.identity);
        var creatureComponent = creature.GetComponent<Creature>();
        creatureComponent.type = type;
        
        // Create initial neural network
        var genome = CreateInitialGenome();
        var network = NEAT.NN.FeedForwardNetwork.Create(genome);
        creatureComponent.InitializeNetwork(network);
        
        // Pass the max hidden layers setting to the creature
        creatureComponent.maxHiddenLayers = maxHiddenLayers;
    }
    
    NEAT.Genome.Genome CreateInitialGenome()
    {
        var genome = new NEAT.Genome.Genome(0);
        
        // Add input nodes (14 inputs):
        // 0: health
        // 1: energy
        // 2: reproduction
        // 3,4: same type x,y
        // 5: same type absolute sum
        // 6,7: opposite type x,y
        // 8: opposite type absolute sum
        // 9,10: cherry x,y
        // 11: cherry absolute sum
        // 12,13: current direction x,y
        for (int i = 0; i < 14; i++)
        {
            var node = new NEAT.Genes.NodeGene(i, NEAT.Genes.NodeType.Input);
            node.Layer = 0;  // Input layer
            genome.AddNode(node);
        }
        
        // Add output nodes (x,y velocity)
        var outputNode1 = new NEAT.Genes.NodeGene(14, NEAT.Genes.NodeType.Output);
        var outputNode2 = new NEAT.Genes.NodeGene(15, NEAT.Genes.NodeType.Output);
        outputNode1.Layer = 2;  // Output layer
        outputNode2.Layer = 2;  // Output layer
        genome.AddNode(outputNode1);
        genome.AddNode(outputNode2);
        
        // Add some basic connections with fixed weights
        // Only connect from input layer (0) to output layer (2)
        // Health to horizontal velocity
        genome.AddConnection(new NEAT.Genes.ConnectionGene(0, 0, 14, -0.5f));
        // Energy to vertical velocity
        genome.AddConnection(new NEAT.Genes.ConnectionGene(1, 1, 15, -0.5f));
        // Same type x position to horizontal velocity
        genome.AddConnection(new NEAT.Genes.ConnectionGene(2, 3, 14, 0.5f));
        // Same type y position to vertical velocity
        genome.AddConnection(new NEAT.Genes.ConnectionGene(3, 4, 15, 0.5f));
        // Cherry x position to horizontal velocity
        genome.AddConnection(new NEAT.Genes.ConnectionGene(4, 9, 14, 0.5f));
        // Cherry y position to vertical velocity
        genome.AddConnection(new NEAT.Genes.ConnectionGene(5, 10, 15, 0.5f));
        // Current direction x to horizontal velocity
        genome.AddConnection(new NEAT.Genes.ConnectionGene(6, 12, 14, 0.3f));
        // Current direction y to vertical velocity
        genome.AddConnection(new NEAT.Genes.ConnectionGene(7, 13, 15, 0.3f));
        
        return genome;
    }
} 