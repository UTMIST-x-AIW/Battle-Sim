using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Action Keys")]
    [SerializeField] private KeyCode chopKey = KeyCode.H;
    [SerializeField] private KeyCode swordKey = KeyCode.J;
    [SerializeField] private KeyCode reproduceKey = KeyCode.K;

    private Rigidbody2D rb;
    private Creature creatureComponent;
    
    void OnEnable()
    {
        rb = GetComponent<Rigidbody2D>();
        creatureComponent = GetComponent<Creature>();
        
        if (creatureComponent == null)
        {
            Debug.LogError("PlayerController requires a Creature component!");
            enabled = false;
            return;
        }
        
        // Disable the brain control when player control is enabled
        creatureComponent.disableBrainControl = true;
    }
    
    void OnDisable()
    {
        // Re-enable brain control when player control is disabled
        if (creatureComponent != null)
        {
            creatureComponent.disableBrainControl = false;
        }
    }
    
    void FixedUpdate()
    {
        if (creatureComponent == null) return;

        // Get keyboard input
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        // Determine action desires (0 = no desire, 1 = full desire)
        float interactDesire = Input.GetKey(chopKey) ? 1.0f : -1.0f;
        float attackDesire = Input.GetKey(swordKey) ? 1.0f : -1.0f;
        float reproduceDesire = Input.GetKey(reproduceKey) ? 1.0f : -1.0f;

        // Create the actions array exactly as the neural network would:
        // [0] = horizontal movement (-1 to 1)
        // [1] = vertical movement (-1 to 1)
        // [2] = interact desire (0 to 1)
        // [3] = attack desire (0 to 1)
        // [4] = reproduction desire (0 to 1)
        float[] actions = new float[NEATTest.ACTION_COUNT];
        actions[0] = horizontal;
        actions[1] = vertical;
        actions[2] = interactDesire;
        actions[3] = attackDesire;
        if (NEATTest.ACTION_COUNT > 4)
            actions[4] = reproduceDesire;
        // Pass the actions to the creature's processing method
        creatureComponent.ProcessActionCommands(actions);
    }
} 