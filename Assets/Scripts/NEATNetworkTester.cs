using UnityEngine;

public class NEATNetworkTester : MonoBehaviour
{
    void Start()
    {
        TestNEATLibrary();
    }

    void TestNEATLibrary()
    {
        Debug.Log("=== NEAT LIBRARY TEST: STARTING ===");
        
        // Create a simple genome with our new bias implementation
        var genome = new NEAT.Genome.Genome(0);
        
        // Add a single input node
        var inputNode = new NEAT.Genes.NodeGene(0, NEAT.Genes.NodeType.Input);
        inputNode.Layer = 0;
        genome.AddNode(inputNode);
        
        // Add a single output node with a bias
        var outputNode = new NEAT.Genes.NodeGene(1, NEAT.Genes.NodeType.Output);
        outputNode.Layer = 1;
        outputNode.Bias = 0.5f; // Set a bias value
        genome.AddNode(outputNode);
        
        // Add a connection from input to output
        var connection = new NEAT.Genes.ConnectionGene(0, 0, 1, 1.0f);
        genome.AddConnection(connection);
        
        // Create a neural network from the genome
        var network = NEAT.NN.FeedForwardNetwork.Create(genome);
        
        // Test the network with different input values
        double[] input1 = new double[] { 0.0 };
        double[] output1 = network.Activate(input1);
        
        double[] input2 = new double[] { 1.0 };
        double[] output2 = network.Activate(input2);
        
        // Log the results
        float expectedTanh1 = (float)System.Math.Tanh(0.5);
        Debug.Log(string.Format("Test 1: Input=0.0, Output={0:F4}, Expected≈{1:F4}", output1[0], expectedTanh1));
        bool test1Success = Mathf.Approximately((float)output1[0], expectedTanh1);
        
        float expectedTanh2 = (float)System.Math.Tanh(1.5);
        Debug.Log(string.Format("Test 2: Input=1.0, Output={0:F4}, Expected≈{1:F4}", output2[0], expectedTanh2));
        bool test2Success = Mathf.Approximately((float)output2[0], expectedTanh2);
        
        if (test1Success && test2Success) {
            Debug.Log("=== NEAT LIBRARY TEST: SUCCESS! Bias is working correctly ===");
        } else {
            Debug.LogError("=== NEAT LIBRARY TEST: FAILED! Bias is not working correctly ===");
        }
    }
} 