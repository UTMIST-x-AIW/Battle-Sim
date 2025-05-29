using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System;
// Explicitly use UnityEngine.Random to avoid ambiguity with System.Random
using Random = UnityEngine.Random;
using NEAT.Genes;

public class Creature : MonoBehaviour
{
    // Add static counter at the top of the class
    private static int totalCreatures = 0; //IMPROVEMENT: move this to our game manager (neattest)
    [SerializeField] private static NEATTest neatTest;  // Cache NEATTest reference
    
    // Make TotalCreatures accessible through a property
    public static int TotalCreatures { get { return totalCreatures; } }
    
    // Method to reset the static reference when scene changes
    public static void ClearStaticReferences()
    {
        neatTest = null;
        totalCreatures = 0;
        Debug.Log("Creature static references cleared");
    }
    
    [Header("Basic Stats")]
    public float health = 3f;
    public float energyMeter = 0f;  // Renamed from energy
    public float maxHealth = 3f;
    public float maxEnergy = 1f;
    public float energyRechargeRate = 0.333f; // Fill from 0 to 1 in 3 seconds
    public float reproductionRechargeRate = 0.333f; // Fill from 0 to 1 in 10 seconds
    
    [Header("Aging Settings")]
    public float agingStartTime = 20f;  // Start aging after 20 seconds
    public float agingRate = 0.005f;    // Reduced from 0.01 to 0.005 for slower aging
    private float lifetime = 0f;        // How long the creature has lived
    public float Lifetime { get { return lifetime; } set { lifetime = value; } }  // Public property to access lifetime
    public int generation = 0;          // The generation number of this creature
    
    [Header("Movement Settings")]
    public float moveSpeed = 5f;  // Maximum speed in any direction
    public float pushForce = 20f; // Force applied for pushing, higher value means stronger pushing
    
    [Header("Reproduction Settings")]
    public float weightMutationRate = 0.8f;  // Chance of mutating each connection weight
    public float mutationRange = 0.5f;       // Maximum weight change during mutation
    public float addNodeRate = 0.2f;         // Chance of adding a new node
    public float addConnectionRate = 0.5f;    // Chance of adding a new connection
    public float deleteConnectionRate = 0.2f; // Chance of deleting a connection
    
    [Header("Network Settings")]
    public int maxHiddenLayers = 10;  // Maximum number of hidden layers allowed (set by NEATTest)
    
    [Header("Action Settings")]
    public float actionEnergyCost = 1.0f;
    public float chopDamage = 1.0f;
    public float swordDamage = 2.3f;
    public float closeRange = 1.5f;   // Range at which creatures can chop trees
    public float bowRange = 2.5f;  // Range at which creatures can bow attack other entities
    public float bowDamage = 1.0f;  // Damage dealt by bow
    
    [Header("Detection Settings")]
    public float[] dynamicVisionRanges = {2.5f, 5f, 10f, 15f, 20f};  // Progressive ranges for dynamic vision
    public int preAllocCollidersCount = 200;  // Size of pre-allocated collider array
    
    // NEW: Raycast-based detection settings (inspired by RayVisualizer)
    [Header("Raycast Detection Settings")]
    [SerializeField] private int detectionRayCount = 16;  // Number of rays for 360° detection
    [SerializeField] private float maxDetectionRange = 20f;  // Maximum detection distance
    [SerializeField] private bool enableDetectionVisualization = false;  // For debugging
    [SerializeField] private bool enableDetectionDebugLogs = false;  // For debugging detection issues
    [SerializeField] private LayerMask treeLayerMask = -1;  // Trees layer (default: all layers)
    [SerializeField] private LayerMask albertLayerMask = -1;  // Alberts layer  
    [SerializeField] private LayerMask kaiLayerMask = -1;  // Kais layer
    [SerializeField] private LayerMask groundLayerMask = -1;  // Ground layer
    
    // Type
    public enum CreatureType { Albert, Kai }
    public CreatureType type;
    
    // Neural Network
    public NEAT.NN.FeedForwardNetwork brain;
    private Rigidbody2D rb;
    
    // Add method to get brain
    public NEAT.NN.FeedForwardNetwork GetBrain()
    {
        return brain;
    }
    
    // Add at the top with other private fields
    private bool isReproducing = false;  // Flag to prevent multiple reproduction attempts
    private bool isMovingToMate = false;
    private bool isWaitingForMate = false;
    public bool canStartReproducing = false;  // New flag to control reproduction start, now public
    private Creature targetMate = null;
    public float reproductionMeter = 0f; // Renamed from reproductionCooldown, now public

    private bool hasCheckedNeatTest = false;

    // Flag to disable AI brain control
    public bool disableBrainControl = false;

    // OLD: These are no longer needed with raycast detection
    // private Collider2D[] nearbyTreeColliders;      
    // private Collider2D[] nearbyTeammateColliders;  
    // private Collider2D[] nearbyOpponentColliders;  
    // private Collider2D[] nearbyGroundColliders;    
    
    private TreeHealth nearestTree = null;
    private Creature nearestOpponent = null;
    private Creature nearestTeammate = null;  // Reference to nearest teammate
    private Collider2D nearestGround = null;          // Reference to nearest ground collider
    // private Vector2 nearestCherryPos = Vector2.zero;
    private Vector2 nearestTreePos = Vector2.zero;
    private Vector2 nearestGroundPos = Vector2.zero;
    private Vector2 nearestTeammatePos = Vector2.zero;
    private Vector2 nearestOpponentPos = Vector2.zero;
    private float nearestTreeDistance = float.MaxValue;
    private float nearestOpponentDistance = float.MaxValue;
    // private float nearestCherryDistance = float.MaxValue;
    private float nearestGroundDistance = float.MaxValue;
    private float nearestTeammateDistance = float.MaxValue;
    private float nearestOpponentHealthNormalized = 0f;
    
    // Cached range indicators - accessible as properties
    private float inChopRange = 0f;
    private float inSwordRange = 0f;
    private float inBowRange = 0f;
    
    // Public properties to access range indicators
    public bool InChopRange => inChopRange > 0.5f;
    public bool InSwordRange => inSwordRange > 0.5f;
    public bool InBowRange => inBowRange > 0.5f;
    
    // Track the actual tree vision range currently being used
    public float currentTreeVisionRange;
    public float currentTeammateVisionRange;
    public float currentOpponentVisionRange;
    public float currentGroundVisionRange;

    private void Awake()
    {
        try //IMPROVEMENT: in general i think we can remove most if not all try catches
    {
        // Initialize collider array with inspector-configured size
        // nearbyTreeColliders = new Collider2D[preAllocCollidersCount];
        // nearbyTeammateColliders = new Collider2D[preAllocCollidersCount];
        // nearbyOpponentColliders = new Collider2D[preAllocCollidersCount];
        // nearbyGroundColliders = new Collider2D[preAllocCollidersCount];
        
        // Cache NEATTest reference if not already cached
        neatTest = FindObjectOfType<NEATTest>();
        

        // Initialize stats
        health = maxHealth;
        reproductionMeter = 0f; // Initialize reproduction meter to 0
        lifetime = 0f;
        canStartReproducing = false;
        
        // Increment counter when creature is created
        totalCreatures++;
        // Debug.Log(string.Format("Creature created. Total creatures: {0}", totalCreatures));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in Creature Awake: {e.Message}\n{e.StackTrace}");
        }
    }

    public IEnumerator DelayedReproductionStart()
    {
        // Reset flags and meter after reproduction
        canStartReproducing = false;
        reproductionMeter = 0f; // Reset reproduction meter to 0
        
        // Wait a short time before allowing the meter to start filling again
        yield return new WaitForSeconds(0.05f);
        
        // No need to set canStartReproducing=true here anymore
        // It will be set when the meter fills to 1.0
    }
    
    private void Start()
    {
        // Setup Rigidbody2D
        rb = gameObject.GetComponent<Rigidbody2D>();
        
        // Auto-detect layer masks if they're set to default (-1)
        if (enableDetectionDebugLogs)
        {
            AutoDetectLayerMasks();
        }
    }

    private void AutoDetectLayerMasks()
    {
        Debug.Log($"=== AUTO-DETECTING LAYERS FOR {gameObject.name} ===");
        
        // Find a tree and detect its layer
        GameObject[] trees = GameObject.FindGameObjectsWithTag("Tree");
        Debug.Log($"Found {trees.Length} objects with 'Tree' tag");
        if (trees.Length > 0)
        {
            int treeLayer = trees[0].layer;
            Debug.Log($"Auto-detected Tree layer: {treeLayer} (LayerMask value: {1 << treeLayer})");
            Debug.Log($"First tree: {trees[0].name} at position {trees[0].transform.position}");
            
            // Check distance to first tree
            float distToTree = Vector3.Distance(transform.position, trees[0].transform.position);
            Debug.Log($"Distance to first tree: {distToTree:F2} (detection range: {maxDetectionRange})");
        }
        
        // Find creatures and detect their layers
        Creature[] allCreatures = FindObjectsOfType<Creature>();
        Debug.Log($"Found {allCreatures.Length} total creatures in scene");
        int albertCount = 0, kaiCount = 0;
        
        foreach (var creature in allCreatures)
        {
            if (creature == this) continue; // Skip self
            
            float distToCreature = Vector3.Distance(transform.position, creature.transform.position);
            
            if (creature.type == CreatureType.Albert)
            {
                albertCount++;
                if (albertCount == 1) // Log first Albert
                {
                    Debug.Log($"Auto-detected Albert layer: {creature.gameObject.layer} (LayerMask value: {1 << creature.gameObject.layer})");
                    Debug.Log($"First other Albert: {creature.name} at distance {distToCreature:F2}");
                }
            }
            else if (creature.type == CreatureType.Kai)
            {
                kaiCount++;
                if (kaiCount == 1) // Log first Kai
                {
                    Debug.Log($"Auto-detected Kai layer: {creature.gameObject.layer} (LayerMask value: {1 << creature.gameObject.layer})");
                    Debug.Log($"First Kai: {creature.name} at distance {distToCreature:F2}");
                }
            }
        }
        
        Debug.Log($"Other creatures: {albertCount} Alberts, {kaiCount} Kais");
        
        // Find ground objects
        GameObject[] grounds = GameObject.FindGameObjectsWithTag("Ground");
        Debug.Log($"Found {grounds.Length} objects with 'Ground' tag");
        if (grounds.Length > 0)
        {
            int groundLayer = grounds[0].layer;
            Debug.Log($"Auto-detected Ground layer: {groundLayer} (LayerMask value: {1 << groundLayer})");
            
            float distToGround = Vector3.Distance(transform.position, grounds[0].transform.position);
            Debug.Log($"Distance to first ground: {distToGround:F2}");
        }
        
        // Check current layer mask settings
        Debug.Log($"Current LayerMask settings - Tree: {treeLayerMask.value}, Albert: {albertLayerMask.value}, Kai: {kaiLayerMask.value}, Ground: {groundLayerMask.value}");
        Debug.Log($"=== END AUTO-DETECTION ===");
    }

    public void InitializeNetwork(NEAT.NN.FeedForwardNetwork network)
    {
        brain = network;
    }
    
    // OPTIMIZED: Single raycast-based detection system (replaces all OverlapCircle calls)
    private void DetectNearbyObjects()
    {
        // Reset all cached values
        nearestTree = null;
        nearestOpponent = null;
        nearestTeammate = null;
        nearestGround = null;
        nearestTreePos = Vector2.zero;
        nearestGroundPos = Vector2.zero;
        nearestTeammatePos = Vector2.zero;
        nearestOpponentPos = Vector2.zero;
        nearestTreeDistance = float.MaxValue;
        nearestOpponentDistance = float.MaxValue;
        nearestGroundDistance = float.MaxValue;
        nearestTeammateDistance = float.MaxValue;
        nearestOpponentHealthNormalized = 0f;

        // Reset range indicators
        inChopRange = 0f;
        inSwordRange = 0f;
        inBowRange = 0f;

        // OPTIMIZED: Single 360° raycast sweep (inspired by RayVisualizer)
        DetectObjectsWithRaycast();

        // Calculate range indicators
        CalculateRangeIndicators();
    }

    private void DetectObjectsWithRaycast()
    {
        // Debug: Log layer mask setup on first frame
        if (enableDetectionDebugLogs && Time.fixedTime < 1f)
        {
            Debug.Log($"{gameObject.name} Layer Masks - Tree: {treeLayerMask.value}, Albert: {albertLayerMask.value}, Kai: {kaiLayerMask.value}, Ground: {groundLayerMask.value}");
            Debug.Log($"{gameObject.name} is type: {type}, using teammate mask: {GetTeammateLayerMask().value}, opponent mask: {GetOpponentLayerMask().value}");
        }
        
        // Cast rays in all directions (360°) - much more efficient than multiple OverlapCircle calls
        LayerMask combinedMask = treeLayerMask | GetTeammateLayerMask() | GetOpponentLayerMask() | groundLayerMask;
        int hitCount = 0;
        int selfHitCount = 0;
        
        for (int i = 0; i < detectionRayCount; i++)
        {
            // Calculate ray direction (evenly distributed around 360°)
            float angle = (360f / detectionRayCount) * i * Mathf.Deg2Rad;
            Vector2 rayDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            
            // Cast ray and get ALL hits (not just first)
            RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, rayDirection, maxDetectionRange, combinedMask);
            
            // Process each hit, skipping self
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider != null)
                {
                    // IMPROVED: Skip self-detection more reliably
                    if (hit.collider.gameObject == gameObject || hit.distance < 0.1f)
                    {
                        selfHitCount++;
                        continue; // Skip self
                    }
                    
                    hitCount++;
                    
                    // Debug: Log what we hit (limit to avoid spam)
                    if (enableDetectionDebugLogs && hitCount <= 5)
                    {
                        Debug.Log($"{gameObject.name} Ray {i} hit: {hit.collider.name} on layer {hit.collider.gameObject.layer} with tag '{hit.collider.tag}' at distance {hit.distance:F2}");
                    }
                    
                    ProcessRaycastHit(hit);
                }
            }
            
            // Debug visualization (optional) - use first non-self hit
            if (enableDetectionVisualization)
            {
                RaycastHit2D firstHit = new RaycastHit2D();
                foreach (var hit in hits)
                {
                    if (hit.collider != null && hit.collider.gameObject != gameObject && hit.distance >= 0.1f)
                    {
                        firstHit = hit;
                        break;
                    }
                }
                
                Color rayColor = firstHit.collider != null ? Color.red : Color.green;
                Vector2 rayEnd = firstHit.collider != null ? firstHit.point : (Vector2)transform.position + rayDirection * maxDetectionRange;
                Debug.DrawRay(transform.position, (Vector3)rayEnd - transform.position, rayColor, 0.1f);
            }
        }
        
        // Debug: Log detection results
        if (enableDetectionDebugLogs)
        {
            Debug.Log($"{gameObject.name} Detection Results - Valid Hits: {hitCount}, Self Hits: {selfHitCount}, Tree: {nearestTreeDistance:F2}, Teammate: {nearestTeammateDistance:F2}, Opponent: {nearestOpponentDistance:F2}, Ground: {nearestGroundDistance:F2}");
            
            // If no valid hits, suggest fixes
            if (hitCount == 0)
            {
                Debug.LogWarning($"{gameObject.name} No valid hits detected! Check: 1) Layer masks are correct, 2) Objects have right tags, 3) Objects are within {maxDetectionRange} range, 4) Objects exist in scene");
            }
        }
    }

    private void ProcessRaycastHit(RaycastHit2D hit)
    {
        // Extra safety check: don't process very close hits (likely self-hits)
        if (hit.distance < 0.1f || hit.collider.gameObject == gameObject)
        {
            return;
        }
        
        float distance = hit.distance;
        Vector2 relativePos = hit.point - (Vector2)transform.position;
        
        bool wasClassified = false;
        
        // Process based on what we hit
        if (IsTreeLayer(hit.collider.gameObject.layer) && hit.collider.CompareTag("Tree"))
        {
            wasClassified = true;
            if (distance < nearestTreeDistance)
            {
                nearestTreeDistance = distance;
                nearestTreePos = relativePos;
                nearestTree = hit.collider.GetComponent<TreeHealth>();
                
                if (enableDetectionDebugLogs)
                {
                    Debug.Log($"{gameObject.name} Found closer tree: {hit.collider.name} at distance {distance:F2}");
                }
            }
        }
        else if (IsTeammateLayer(hit.collider.gameObject.layer))
        {
            wasClassified = true;
            Creature teammate = hit.collider.GetComponent<Creature>();
            if (teammate != null && teammate != this && distance < nearestTeammateDistance) // Extra check: teammate != this
            {
                nearestTeammateDistance = distance;
                nearestTeammatePos = relativePos;
                nearestTeammate = teammate;
                
                if (enableDetectionDebugLogs)
                {
                    Debug.Log($"{gameObject.name} Found closer teammate: {hit.collider.name} at distance {distance:F2}");
                }
            }
        }
        else if (IsOpponentLayer(hit.collider.gameObject.layer))
        {
            wasClassified = true;
            Creature opponent = hit.collider.GetComponent<Creature>();
            if (opponent != null && opponent != this && distance < nearestOpponentDistance) // Extra check: opponent != this
            {
                nearestOpponentDistance = distance;
                nearestOpponentPos = relativePos;
                nearestOpponent = opponent;
                nearestOpponentHealthNormalized = opponent.health / opponent.maxHealth;
                
                if (enableDetectionDebugLogs)
                {
                    Debug.Log($"{gameObject.name} Found closer opponent: {hit.collider.name} at distance {distance:F2}");
                }
            }
        }
        else if (IsGroundLayer(hit.collider.gameObject.layer) && hit.collider.CompareTag("Ground"))
        {
            wasClassified = true;
            if (distance < nearestGroundDistance)
            {
                nearestGroundDistance = distance;
                nearestGroundPos = relativePos;
                nearestGround = hit.collider;
                
                if (enableDetectionDebugLogs)
                {
                    Debug.Log($"{gameObject.name} Found closer ground: {hit.collider.name} at distance {distance:F2}");
                }
            }
        }
        
        // Debug: Log unclassified hits
        if (!wasClassified && enableDetectionDebugLogs)
        {
            Debug.LogWarning($"{gameObject.name} Hit unclassified object: {hit.collider.name} on layer {hit.collider.gameObject.layer} with tag '{hit.collider.tag}' " +
                           $"- TreeLayer: {IsTreeLayer(hit.collider.gameObject.layer)}, " +
                           $"TeammateLayer: {IsTeammateLayer(hit.collider.gameObject.layer)}, " +
                           $"OpponentLayer: {IsOpponentLayer(hit.collider.gameObject.layer)}, " +
                           $"GroundLayer: {IsGroundLayer(hit.collider.gameObject.layer)}");
        }
    }

    // Helper methods for layer checking
    private LayerMask GetTeammateLayerMask()
    {
        return (type == CreatureType.Albert) ? albertLayerMask : kaiLayerMask;
    }

    private LayerMask GetOpponentLayerMask()
    {
        return (type == CreatureType.Albert) ? kaiLayerMask : albertLayerMask;
    }

    private bool IsTreeLayer(int layer) => (treeLayerMask.value & (1 << layer)) != 0;
    private bool IsTeammateLayer(int layer) => (GetTeammateLayerMask().value & (1 << layer)) != 0;
    private bool IsOpponentLayer(int layer) => (GetOpponentLayerMask().value & (1 << layer)) != 0;
    private bool IsGroundLayer(int layer) => (groundLayerMask.value & (1 << layer)) != 0;
    
    private void CalculateRangeIndicators()
    {
        // Calculate range indicators
        if (nearestTreeDistance <= closeRange)
        {
            inChopRange = 1f;
        }
        if (nearestOpponentDistance <= closeRange)
        {
            inSwordRange = 1f;
        }
        if (nearestOpponent != null && nearestOpponentDistance <= bowRange)
        {
            Vector2 directionToOpposite = nearestOpponent.transform.position - transform.position;
            RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, directionToOpposite, nearestOpponentDistance);
            
            bool hitOppositeTypeFirst = false;
            bool blockedByObstacle = false;
            
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.gameObject == gameObject) continue; // Skip self
                
                // Check what we hit
                if (hit.collider.CompareTag("Tree") || hit.collider.CompareTag("Ground"))
                {
                    // If we hit an obstacle before the opposite type creature
                    blockedByObstacle = true;
                    break;
                }
                else
                {
                    Creature hitCreature = hit.collider.GetComponent<Creature>();
                    if (hitCreature != null)
                    {
                        if (hitCreature.type != type && hit.collider.gameObject == nearestOpponent.gameObject)
                        {
                            // We hit the opposite type creature before any obstacles
                            hitOppositeTypeFirst = true;
                            break;
                        }
                        // Creatures of same type are ignored and we continue checking
                    }
                }
            }
            
            if (hitOppositeTypeFirst && !blockedByObstacle)
            {
                inBowRange = 1f;
            }
        }
    }

    // Generate observations for neural network using cached detection data
    public float[] GetObservations()
    {
        float[] obs = new float[NEATTest.OBSERVATION_COUNT]; 
        
        // Basic stats - normalize health to 0-1 range
        obs[0] = health / maxHealth; // Normalized health
        obs[1] = energyMeter; // Energy meter (already 0-1)
        obs[2] = reproductionMeter; // Reproduction meter (already 0-1)
        
        // Transform the observations according to the formula:
        // 0 when outside FOV, 0 at FOV border, increases linearly to the max vision range when hugging creature
        
        // Get maximum detection range for consistent normalization
        float maxRange = maxDetectionRange;
        
        // Teammate observations (x,y components) - normalize by max range
        Vector2 sameTypeObs = Vector2.zero;
        if (nearestTeammateDistance <= maxRange && nearestTeammateDistance > 0)
        {
            // Calculate intensity (0 at max range, maxRange when hugging)
            float intensityFactor = 1.0f - nearestTeammateDistance / maxRange;
            sameTypeObs = nearestTeammatePos * intensityFactor;
        }
        
        // Opponent observations (x,y components) - normalize by max range
        Vector2 oppositeTypeObs = Vector2.zero;
        if (nearestOpponentDistance <= maxRange && nearestOpponentDistance > 0)
        {
            // Calculate intensity (0 at max range, maxRange when hugging)
            float intensityFactor = 1.0f - nearestOpponentDistance / maxRange;
            oppositeTypeObs = nearestOpponentPos * intensityFactor;
        }
        
        // Tree observations (x,y components) - normalize by max range
        Vector2 treeObs = Vector2.zero;
        if (nearestTreeDistance <= maxRange && nearestTreeDistance > 0)
        {
            // Calculate intensity (0 at max range, maxRange when hugging)
            float intensityFactor = 1.0f - nearestTreeDistance / maxRange;
            treeObs = nearestTreePos * intensityFactor;
        }

        // Ground observations (x,y components) - normalize by max range
        Vector2 groundObs = Vector2.zero;
        if (nearestGroundDistance <= maxRange && nearestGroundDistance > 0)
        {
            // Calculate intensity (0 at max range, maxRange when hugging)
            float intensityFactor = 1.0f - nearestGroundDistance / maxRange;
            groundObs = nearestGroundPos * intensityFactor;
        }
        
        // Use cached range indicators instead of calculating them again
        // Assign the transformed values to the observation array
        obs[3] = sameTypeObs.x;
        obs[4] = sameTypeObs.y;
        
        obs[5] = oppositeTypeObs.x;
        obs[6] = oppositeTypeObs.y;
        
        // obs[7] = cherryObs.x;
        // obs[8] = cherryObs.y;
        
        obs[7] = treeObs.x;
        obs[8] = treeObs.y;

        obs[9] = groundObs.x;
        obs[10] = groundObs.y;
        
        obs[11] = this.inChopRange;
        obs[12] = this.inSwordRange;
        obs[13] = this.inBowRange;

        obs[14] = nearestOpponentHealthNormalized;
        
        // Debug: Log observation values occasionally
        if (enableDetectionDebugLogs && Random.value < 0.01f) // Log 1% of the time to avoid spam
        {
            Debug.Log($"{gameObject.name} Observations: Health={obs[0]:F3}, Energy={obs[1]:F3}, Repro={obs[2]:F3}, " +
                     $"Teammate=({obs[3]:F3},{obs[4]:F3}), Opponent=({obs[5]:F3},{obs[6]:F3}), " +
                     $"Tree=({obs[7]:F3},{obs[8]:F3}), Ground=({obs[9]:F3},{obs[10]:F3}), " +
                     $"Ranges=(C:{obs[11]},S:{obs[12]},B:{obs[13]}), OppHealth={obs[14]:F3}");
        }
        
        return obs;
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
    
    public float[] GetActions(float[] observations) 
    //TODO: remove all this debug scaffolding
    {
        try
        {
            if (brain == null)
            {
                // Debug.LogWarning(string.Format("{0}: Brain is null, returning zero movement", gameObject.name));
                return new float[] { 0f, 0f, 0f, 0f, 0f };  // 5 outputs: move x, move y, chop, sword, bow
            }
            
            double[] doubleObservations = ConvertToDouble(observations); //TODO: remove this debugging code if it works without
            
            // Use a separate try-catch for the activation to detect stack overflow specifically
            double[] doubleOutputs;
            try
            {
                // Add a timeout or stack overflow protection here
                doubleOutputs = brain.Activate(doubleObservations); //IMPROVEMENT: try using multithreading here, with jobs
            }
            catch (System.StackOverflowException e)
            {
                // Create a simple backup neural network or return default values
                if (LogManager.Instance != null)
                {
                    LogManager.LogError($"Stack overflow in neural network for {gameObject.name} (Gen {generation}). Creating fallback outputs.");
                }
                else
                {
                    Debug.LogError($"Stack overflow in neural network for {gameObject.name} (Gen {generation}). Creating fallback outputs.");
                }
                
                // Return safe values
                return new float[] { 0f, 0f, 0f, 0f };
            }
            catch (System.Exception e)
            {
                // Handle other activation errors
                if (LogManager.Instance != null)
                {
                    LogManager.LogError($"Neural network activation error for {gameObject.name} (Gen {generation}): {e.Message}");
                }
                else
                {
                    Debug.LogError($"Neural network activation error for {gameObject.name} (Gen {generation}): {e.Message}");
                }
                
                // Return safe values
                return new float[] { 0f, 0f, 0f, 0f };
            }
            
            float[] outputs = ConvertToFloat(doubleOutputs);
            
            // Log the neural network outputs for debugging
            string outputInfo = $"NN outputs for {gameObject.name} (Gen {generation}): Length={outputs.Length}, Values=[";
            for (int i = 0; i < outputs.Length; i++)
            {
                outputInfo += $"{outputs[i]:F3}";
                if (i < outputs.Length - 1) outputInfo += ", ";
            }
            outputInfo += "]";
            
            if (LogManager.Instance != null)
            {
                // Only log if the output length is not what we expect (to avoid too many log entries)
                if (outputs.Length != NEATTest.ACTION_COUNT)
                {
                    LogManager.LogError(outputInfo);
                }
                else if (Random.value < 0.01) // Log only 1% of normal outputs as samples
                {
                    LogManager.LogMessage(outputInfo);
                }
            }
            
            // Ensure outputs are in range [-1, 1]
            for (int i = 0; i < outputs.Length; i++)
            {
                outputs[i] = Mathf.Clamp(outputs[i], -1f, 1f);
            }
            
            // Double-check that we're getting the expected number of outputs
            if (outputs.Length != NEATTest.ACTION_COUNT)
            {
                string errorMsg = $"Neural network returned {outputs.Length} outputs instead of {NEATTest.ACTION_COUNT}. Creating adjusted array.";
                if (LogManager.Instance != null)
                {
                    LogManager.LogError(errorMsg);
                }
                else
                {
                    Debug.LogError(errorMsg);
                }
                
                // Create a new array of exactly NEATTest.ACTION_COUNT elements
                float[] adjustedOutputs = new float[NEATTest.ACTION_COUNT];
                
                // Copy the values we have
                for (int i = 0; i < Mathf.Min(outputs.Length, NEATTest.ACTION_COUNT); i++)
                {
                    adjustedOutputs[i] = outputs[i];
                }
                
                return adjustedOutputs;
            }
            
            return outputs;
        }
        catch (System.Exception e)
        {
            string errorMsg = $"Error in GetActions for {gameObject.name}: {e.Message}";
            if (LogManager.Instance != null)
            {
                LogManager.LogError($"{errorMsg}\nStack trace: {e.StackTrace}");
            }
            else
            {
                Debug.LogError(errorMsg);
            }
            
            return new float[] { 0f, 0f, 0f, 0f };  // Return default values on error
        }
    }

    // Note: The ApplyMutations method has been moved to Reproduction.cs and is no longer used here.
    // All mutations are now handled during reproduction in the Reproduction.cs script.
    // This includes weight mutations, node additions/deletions, and connection mutations.

    private void FixedUpdate()
    {
        try
        {
            // Update lifetime and health
            lifetime += Time.fixedDeltaTime;
            
            // Start aging process after a threshold time
            if (lifetime > agingStartTime)
            {
                health -= agingRate * Time.fixedDeltaTime;
            }

            // Recharge energy gradually
            energyMeter = Mathf.Min(maxEnergy, energyMeter + energyRechargeRate * Time.fixedDeltaTime);
            
            // Check if we're dead
            if (health <= 0f)
            {
                // Log the death
                if (LogManager.Instance != null)
                {
                    LogManager.LogMessage($"Creature dying due to health <= 0 - Type: {type}, Health: {health}, Age: {lifetime}, Generation: {generation}");
                }
                else
                {
                    Debug.Log($"Creature dying due to health <= 0 - Type: {type}, Health: {health}, Age: {lifetime}, Generation: {generation}");
                }
                
                // Die
                Destroy(gameObject);
                return;
            }
            
            // Only get actions if we're not reproducing or moving to mate
            if (!isReproducing && !isMovingToMate && !isWaitingForMate)
            {
                try
                {
                    // FIXED: Only fill reproduction meter if it's not full yet
                    if (reproductionMeter < 1.0f)
                    {
                        reproductionMeter = Mathf.Min(1f, reproductionMeter + reproductionRechargeRate * Time.fixedDeltaTime);
                        
                        // When meter is full, set canStartReproducing to true but don't reset the meter
                        if (reproductionMeter >= 1f)
                        {
                            canStartReproducing = true;
                        }
                    }
                    
                    // Detect nearby objects once per frame and cache results
                    DetectNearbyObjects();
                    
                    // Get network outputs (x, y velocities, chop desire, sword desire, bow desire)
                    float[] observations = GetObservations();
                    float[] actions = GetActions(observations);

                    if (!disableBrainControl)
                    {
                        // Process the network's action commands
                        ProcessActionCommands(actions); //IMPROVEMENT: try using multithreading here, with jobs
                    }
                }
                catch (System.Exception e)
                {
                    // Log detailed error information
                    string errorMessage = $"Error in Creature FixedUpdate action processing: {e.Message}\nStack trace: {e.StackTrace}";
                    if (LogManager.Instance != null)
                    {
                        LogManager.LogError(errorMessage);
                    }
                    else
                    {
                        Debug.LogError(errorMessage);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            // Log very detailed error information if the main loop fails
            string errorMessage = $"CRITICAL ERROR in Creature FixedUpdate: {e.Message}\nStack trace: {e.StackTrace}\nCreature info: Type={type}, Generation={generation}, Health={health}, Age={lifetime}";
            if (LogManager.Instance != null)
            {
                LogManager.LogError(errorMessage);
            }
            else
            {
                Debug.LogError(errorMessage);
            }
        }
    }

    public void ApplyMovement(Vector2 desiredVelocity)
    {
        // All checks passed, apply the original movement force
        rb.AddForce(desiredVelocity, ForceMode2D.Force);
        
        // Limit maximum velocity to prevent excessive speeds from accumulating forces
        if (rb.velocity.magnitude > moveSpeed * 1.2f)
        {
            rb.velocity = rb.velocity.normalized * moveSpeed * 1.2f;
        }
    }
    
    public void ProcessActionCommands(float[] actions)
    {
        try
        {
            // Log if the array length wasn't NEATTest.ACTION_COUNT
            if (actions.Length != NEATTest.ACTION_COUNT)
            {
                string errorMsg = $"ProcessActionCommands for {gameObject.name}: actions array length {actions.Length}";
                if (LogManager.Instance != null)
                {
                    LogManager.LogError(errorMsg);
                }
                else
                {
                    Debug.LogError(errorMsg);
                }
            }
            
            // Apply movement based on neural network output (first two values)
            Vector2 moveDirection = new Vector2(actions[0], actions[1]);
            
            // Normalize to ensure diagonal movement isn't faster
            if (moveDirection.magnitude > 1f)
            {
                moveDirection.Normalize();
            }
            
            // Apply move speed with physics force (using pushForce value)
            Vector2 desiredVelocity = moveDirection * pushForce;
            ApplyMovement(desiredVelocity);
            
            // Process action commands
            float[] desires = {actions[2], actions[3], actions[4]};

            
            // Energy-limited action: Allow action only if we have sufficient energy
            if (energyMeter >= actionEnergyCost)
            {
                

                // If there are any positive desires, process the strongest one
                if (desires.Any(desire => desire > 0.0f))
                {
                    // Process the strongest desire
                    int strongestDesireIndex = Array.IndexOf(desires, desires.Max());

                    // Get animation for the strongest desire
                    ToolAnimation toolAnim = GetComponentInChildren<ToolAnimation>();

                    switch (strongestDesireIndex) {
                        case 0:
                            // Chop tree
                            if (InChopRange)
                            {
                                nearestTree.TakeDamage(chopDamage);
                                energyMeter -= actionEnergyCost;
                                health = maxHealth;
                                toolAnim.SwingTool(ToolAnimation.ToolType.Axe);
                                StartCoroutine(FlashHealthRestoration());
                            }
                            break;
                        case 1:
                            // Sword attack
                            if (InSwordRange)
                            {
                                nearestOpponent.TakeDamage(swordDamage);
                                energyMeter -= actionEnergyCost;
                                toolAnim.SwingTool(ToolAnimation.ToolType.Sword);
                            }
                            break;
                        case 2:
                            // Bow attack
                            if (InBowRange)
                            {
                                toolAnim.SwingTool(ToolAnimation.ToolType.Bow);
                                energyMeter -= actionEnergyCost;

                                // Fire arrow animation
                                if (ArrowsManager.Instance != null)
                                {
                                    ArrowsManager.Instance.FireArrow(
                                        transform.position, 
                                        nearestOpponent.transform.position, 
                                        bowRange
                                    );
                                }

                                nearestOpponent.TakeDamage(bowDamage);
                            }
                            break;
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            string errorMsg = $"Error in ProcessActionCommands for {gameObject.name}: {e.Message}\nStack trace: {e.StackTrace}";
            if (LogManager.Instance != null)
            {
                LogManager.LogError(errorMsg);
            }
            else
            {
                Debug.LogError(errorMsg);
            }
        }
    }

 

    private IEnumerator FlashHealthRestoration()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>(); //IMPROVEMENT: minor improvement, instead of getting the component every time, just call it in start or something. also just check the rest of the codebase for this same thing happening elsewhere
        if (renderer != null)
        {
            Color originalColor = renderer.color;
            renderer.color = Color.green;
            yield return new WaitForSeconds(0.1f);
            renderer.color = originalColor;
        }
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        
        // Visual feedback when taking damage
        StartCoroutine(FlashOnDamage());
    }

    private IEnumerator FlashOnDamage()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>(); //IMPROVEMENT: minor improvement, instead of getting the component every time, just call it in start or something.
        if (renderer != null)
        {
            Color originalColor = renderer.color;
            renderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            renderer.color = originalColor;
        }
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if we collided with another creature
        Creature otherCreature = collision.gameObject.GetComponent<Creature>();
        if (otherCreature != null && otherCreature.type != type)
        {
            // Check if healths are approximately equal (within 0.1)
            if (Mathf.Abs(health - otherCreature.health) < 0.1f)
            {
                // Both creatures take half damage
                float damage = health / 2f;  // Use either health value since they're equal
                health = Mathf.Max(0, health - damage);
                otherCreature.health = Mathf.Max(0, otherCreature.health - damage);
                
                // If the damage killed either creature, destroy them
                if (health <= 0)
                {
                    Destroy(gameObject);
                }
                if (otherCreature.health <= 0)
                {
                    Destroy(otherCreature.gameObject);
                }
            }
            // Only handle the collision once (let the creature with higher health handle it)
            else if (health > otherCreature.health)
            {
                // Calculate damage as half of the killed creature's health
                float damage = otherCreature.health / 2f;
                
                // Apply damage to surviving creature
                health = Mathf.Max(0, health - damage);
                
                // Kill the creature with lower health
                Destroy(otherCreature.gameObject);
                
                // If the damage killed us too, destroy ourselves
                if (health <= 0)
                {
                    Destroy(gameObject);
                }
            }
        }
    }

    private void OnDestroy()
    {
        try
        {
            try
            {
                // Try to log, but catch any exceptions if LogManager is gone
                if (LogManager.Instance != null)
                {
                    LogManager.LogMessage($"Creature being destroyed - Type: {type}, Health: {health}, Generation: {generation}");
                }
            }
            catch (System.Exception)
            {
                // If LogManager is already cleaned up, just silently continue
            }
            
        // If we were someone's target mate, free them but handle exceptions
        if (targetMate != null && targetMate.gameObject != null && targetMate.gameObject.activeInHierarchy)
            {
                try
        {
            // Break the circular reference first
            var tempMate = targetMate;
            targetMate = null;
            
            // Reset their flags directly instead of using FreeMate which may start coroutines
            tempMate.isMovingToMate = false;
            tempMate.isWaitingForMate = false;
            tempMate.isReproducing = false;
            tempMate.canStartReproducing = false;
            tempMate.targetMate = null;
            
            // If they're still alive and active, they can start their own timer
            if (tempMate.gameObject.activeInHierarchy)
            {
                try 
                {
                    tempMate.StartCoroutine(tempMate.DelayedReproductionStart());
                }
                        catch (System.Exception e)
                {
                            Debug.LogError($"Error starting reproduction timer for mate: {e.Message}");
                }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error handling target mate in OnDestroy: {e.Message}");
            }
        }

        // Decrement counter when creature is destroyed
        totalCreatures--;
            
            try
            {
                // Try to log, but catch any exceptions if LogManager is gone
                if (LogManager.Instance != null)
                {
                    LogManager.LogMessage($"Creature destroyed. Total creatures: {totalCreatures}");
                }
            }
            catch (System.Exception)
            {
                // If LogManager is already cleaned up, just silently continue
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in Creature OnDestroy: {e.Message}\nStack trace: {e.StackTrace}");
        }
    }

    private void OnGUI()
    {
        // Safety check - if no camera, exit early
        if (Camera.main == null) return;

        // Only check for NEATTest reference once
        if (!hasCheckedNeatTest) hasCheckedNeatTest = true;

        // If we still don't have a NEATTest reference or labels are disabled, return early
        if (neatTest == null || !neatTest.showCreatureLabels) return;

        // Get screen position for this creature
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        screenPos.y = Screen.height - screenPos.y; // GUI uses top-left origin

        // Only show if on screen
        if (screenPos.x >= 0 && screenPos.x <= Screen.width && 
            screenPos.y >= 0 && screenPos.y <= Screen.height)
        {
                // Set text color to black
                GUI.color = Color.black;
                
                // Format age display - handle large values
                string ageDisplay;
                if (lifetime >= 10000f)
                {
                    ageDisplay = "10,000+";
                }
                else
                {
                    ageDisplay = lifetime.ToString("F1");
                }

                // Show age and generation with increased width for larger numbers
                GUI.Label(new Rect(screenPos.x - 70, screenPos.y - 40, 140, 20), 
                            string.Format("Gen: {0}; Age: {1}", generation, ageDisplay));
                
                // Reset color back to white
                GUI.color = Color.white;
            }
    }

    private void OnDrawGizmos()
    {
        // Only draw if visualization is enabled and NEATTest reference exists
        if (neatTest != null)
        {
            if (neatTest.showTreeVisionRange)
            {
                // Draw tree vision range (green)
                if (dynamicVisionRanges.Length > 0)
                {
                    Color treeRangeColor = new Color(0f, 1f, 0f, 0.05f);
                    Gizmos.color = treeRangeColor;
                    Gizmos.DrawSphere(transform.position, currentTreeVisionRange);
                    
                    // Draw wire frame for tree range
                    treeRangeColor.a = 0.15f;
                    Gizmos.color = treeRangeColor;
                    Gizmos.DrawWireSphere(transform.position, currentTreeVisionRange);
                }
            }
                
            if (neatTest.showTeammateVisionRange)
            {
                // Draw teammate vision range (orange)
                Color teammateRangeColor = new Color(1f, 0.5f, 0f, 0.05f);
                Gizmos.color = teammateRangeColor;
                Gizmos.DrawSphere(transform.position, currentTeammateVisionRange);
                
                // Draw wire frame for teammate range
                teammateRangeColor.a = 0.15f;
                Gizmos.color = teammateRangeColor;
                Gizmos.DrawWireSphere(transform.position, currentTeammateVisionRange);
            }
            
            if (neatTest.showOpponentVisionRange)
            {
                // Draw opponent vision range (red)
                Color opponentRangeColor = new Color(1f, 0f, 0f, 0.05f);
                Gizmos.color = opponentRangeColor;
                Gizmos.DrawSphere(transform.position, currentOpponentVisionRange);
                
                // Draw wire frame for opponent range
                opponentRangeColor.a = 0.15f;
                Gizmos.color = opponentRangeColor;
                Gizmos.DrawWireSphere(transform.position, currentOpponentVisionRange);
            }

            if (neatTest.showGroundVisionRange)
            {
                // Draw ground vision range (yellow)
                Color groundRangeColor = new Color(1f, 1f, 0f, 0.03f);
                Gizmos.color = groundRangeColor;
                Gizmos.DrawSphere(transform.position, currentGroundVisionRange);
                
                // Draw wire frame for ground range
                groundRangeColor.a = 0.1f;
                Gizmos.color = groundRangeColor;
                Gizmos.DrawWireSphere(transform.position, currentGroundVisionRange);
            }
                
            // Draw close range if enabled
            if (neatTest.showCloseRange)
            {
                Gizmos.color = neatTest.closeRangeColor;
                Gizmos.DrawWireSphere(transform.position, closeRange);
            }

            // Draw bow range if enabled
            if (neatTest.showBowRange)
            {
                Gizmos.color = neatTest.bowRangeColor;
                Gizmos.DrawWireSphere(transform.position, bowRange);
            }
        }
    }
    
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