using UnityEngine;

public class Creature : MonoBehaviour
{
    // Basic stats
    public float health = 3f;
    public float energy = 5f;
    public float reproduction = 0f;
    
    // Type
    public enum CreatureType { Albert, Kai }
    public CreatureType type;
    
    // Neural Network
    private NEAT.NN.FeedForwardNetwork brain;
    private CreatureObserver observer;
    private Rigidbody2D rb;
    
    private void Start()
    {
        observer = gameObject.AddComponent<CreatureObserver>();
        rb = gameObject.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f; // No gravity in 2D top-down
        }
    }
    
    public void InitializeNetwork(NEAT.NN.FeedForwardNetwork network)
    {
        brain = network;
    }
    
    private double[] ConvertToDouble(float[] floatArray)
    {
        double[] doubleArray = new double[floatArray.Length];
        for (int i = 0; i < floatArray.Length; i++)
        {
            doubleArray[i] = (double)floatArray[i];
        }
        return doubleArray;
    }
    
    private float[] ConvertToFloat(double[] doubleArray)
    {
        float[] floatArray = new float[doubleArray.Length];
        for (int i = 0; i < doubleArray.Length; i++)
        {
            floatArray[i] = (float)doubleArray[i];
        }
        return floatArray;
    }
    
    public float[] GetActions()
    {
        if (brain == null) return new float[] { 0f, 0f };
        
        float[] observations = observer.GetObservations(this);
        double[] doubleObservations = ConvertToDouble(observations);
        double[] doubleOutputs = brain.Activate(doubleObservations);
        float[] outputs = ConvertToFloat(doubleOutputs);
        
        // Ensure outputs are in range [-1, 1]
        outputs[0] = Mathf.Clamp(outputs[0], -1f, 1f);
        outputs[1] = Mathf.Clamp(outputs[1], -1f, 1f);
        
        // Update energy based on movement
        energy = Mathf.Max(0, energy - Mathf.Abs(outputs[0]) * Time.deltaTime);
        
        // Apply movement using Rigidbody2D
        Vector2 movement = transform.right * outputs[0];
        rb.velocity = movement * 5f; // Scale movement speed
        rb.angularVelocity = outputs[1] * 180f; // Convert to degrees/sec
        
        return outputs;
    }
    
    private void Update()
    {
        if (brain != null)
        {
            GetActions();
        }
    }
} 