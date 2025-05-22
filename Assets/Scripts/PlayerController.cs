using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Action Keys")]
    [SerializeField] private KeyCode chopKey = KeyCode.Space;
    [SerializeField] private KeyCode attackKey = KeyCode.LeftShift;
    
    private Rigidbody2D rb;
    private Creature creatureComponent;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        creatureComponent = GetComponent<Creature>();
        
        if (creatureComponent == null)
        {
            Debug.LogError("PlayerController requires a Creature component!");
            enabled = false;
            return;
        }
    }
    
    void FixedUpdate()
    {
        if (creatureComponent == null) return;
        
        // Keep energy meter and health full
        creatureComponent.energyMeter = creatureComponent.maxEnergy;
        creatureComponent.health = creatureComponent.maxHealth;
        
        // Get keyboard input
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        // Determine action desires (0 = no desire, 1 = full desire)
        float chopDesire = Input.GetKey(chopKey) ? 1.0f : 0.0f;
        float attackDesire = Input.GetKey(attackKey) ? 1.0f : 0.0f;
        
        // Create the actions array exactly as the neural network would:
        // [0] = horizontal movement (-1 to 1)
        // [1] = vertical movement (-1 to 1)
        // [2] = chop desire (0 to 1)
        // [3] = attack desire (0 to 1)
        float[] actions = new float[4];
        actions[0] = horizontal;
        actions[1] = vertical;
        actions[2] = chopDesire;
        actions[3] = attackDesire;
        
        // Pass the actions to the creature's processing method
        creatureComponent.ProcessActionCommands(actions);
    }
} 