using UnityEngine;

public class Creature : MonoBehaviour
{
    // Basic stats
    public float health = 3f;
    public float energy = 5f;
    public float reproduction = 0f;
    
    // Movement settings
    public float moveSpeed = 5f;
    public float rotateSpeed = 180f;
    public float movementEnergyCost = 1f;
    public float rotationEnergyCost = 0.5f;
    
    // Type
    public enum CreatureType { Albert, Kai }
    public CreatureType type;
    
    // Neural Network
    private NEAT.NN.FeedForwardNetwork brain;
    private CreatureObserver observer;
    private Rigidbody2D rb;
    
    private void Awake()
    {
        // Initialize stats
        health = 3f;
        energy = 5f;
        reproduction = 0f;
    }
    
    private void Start()
    {
        observer = gameObject.AddComponent<CreatureObserver>();
        
        // Setup Rigidbody2D
        rb = gameObject.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        
        // Configure Rigidbody2D
        rb.gravityScale = 0f;
        rb.drag = 1f;
        rb.angularDrag = 1f;
        rb.constraints = RigidbodyConstraints2D.None;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        
        // Debug initial state
        Debug.Log(string.Format("Creature initialized - Energy: {0}, Health: {1}", energy, health));
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
        
        return outputs;
    }
    
    private void FixedUpdate()
    {
        if (brain != null)
        {
            float[] actions = GetActions();
            
            // Calculate movement
            float forwardSpeed = actions[0] * moveSpeed;
            float rotationSpeed = actions[1] * rotateSpeed;
            
            // Debug movement values
            Debug.Log(string.Format("Movement - Energy: {0}, Forward: {1}, Rotation: {2}", 
                energy, forwardSpeed, rotationSpeed));
            
            // Apply movement if we have enough energy
            if (energy > 0)
            {
                // Calculate energy costs
                float moveCost = Mathf.Abs(forwardSpeed) * movementEnergyCost * Time.fixedDeltaTime;
                float rotateCost = Mathf.Abs(rotationSpeed) * rotationEnergyCost * Time.fixedDeltaTime;
                float totalEnergyCost = moveCost + rotateCost;
                
                if (totalEnergyCost <= energy)
                {
                    // Apply forward movement
                    Vector2 movement = (Vector2)(transform.right * forwardSpeed);
                    rb.velocity = movement;
                    
                    // Apply rotation
                    rb.angularVelocity = rotationSpeed;
                    
                    // Deduct energy
                    energy = Mathf.Max(0, energy - totalEnergyCost);
                    
                    // Debug energy consumption
                    Debug.Log(string.Format("Energy consumed: {0} (Move: {1}, Rotate: {2})", 
                        totalEnergyCost, moveCost, rotateCost));
                }
                else
                {
                    // Not enough energy for full movement, use what we have
                    float energyRatio = energy / totalEnergyCost;
                    rb.velocity = (Vector2)(transform.right * forwardSpeed * energyRatio);
                    rb.angularVelocity = rotationSpeed * energyRatio;
                    energy = 0;
                }
            }
            else
            {
                // No energy, stop movement
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }
    }
} 