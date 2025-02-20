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
        // Spawn initial Alberts near (-5, 5)
        for (int i = 0; i < 3; i++)
        {
            Vector3 position = new Vector3(-5, 5, 0) + Random.insideUnitSphere * 2f;
            position.z = 0;  // Ensure z is 0
            SpawnCreature(albertCreaturePrefab, position, Creature.CreatureType.Albert);
        }
        
        // Spawn initial Kais near (5, -5)
        for (int i = 0; i < 3; i++)
        {
            Vector3 position = new Vector3(5, -5, 0) + Random.insideUnitSphere * 2f;
            position.z = 0;  // Ensure z is 0
            SpawnCreature(kaiCreaturePrefab, position, Creature.CreatureType.Kai);
        }
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
        
        // Add input nodes (11 inputs):
        // 0: health
        // 1: energy
        // 2: reproduction
        // 3,4: same type x,y
        // 5: same type absolute sum
        // 6,7: opposite type x,y
        // 8: opposite type absolute sum
        // 9,10: current direction x,y
        for (int i = 0; i < 11; i++)
        {
            var node = new NEAT.Genes.NodeGene(i, NEAT.Genes.NodeType.Input);
            node.Layer = 0;  // Input layer
            genome.AddNode(node);
        }
        
        // Add output nodes (x,y velocity)
        var outputNode1 = new NEAT.Genes.NodeGene(11, NEAT.Genes.NodeType.Output);
        var outputNode2 = new NEAT.Genes.NodeGene(12, NEAT.Genes.NodeType.Output);
        outputNode1.Layer = 2;  // Output layer
        outputNode2.Layer = 2;  // Output layer
        genome.AddNode(outputNode1);
        genome.AddNode(outputNode2);
        
        // Add some basic connections with fixed weights
        // Only connect from input layer (0) to output layer (2)
        // Health to horizontal velocity
        genome.AddConnection(new NEAT.Genes.ConnectionGene(0, 0, 11, -0.5f));
        // Energy to vertical velocity
        genome.AddConnection(new NEAT.Genes.ConnectionGene(1, 1, 12, -0.5f));
        // Same type x position to horizontal velocity
        genome.AddConnection(new NEAT.Genes.ConnectionGene(2, 3, 11, 0.5f));
        // Same type y position to vertical velocity
        genome.AddConnection(new NEAT.Genes.ConnectionGene(3, 4, 12, 0.5f));
        // Current direction x to horizontal velocity
        genome.AddConnection(new NEAT.Genes.ConnectionGene(4, 9, 11, 0.3f));
        // Current direction y to vertical velocity
        genome.AddConnection(new NEAT.Genes.ConnectionGene(5, 10, 12, 0.3f));
        
        return genome;
    }
} 