using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float actionCooldown = 0.5f;
    
    [Header("Action Keys")]
    [SerializeField] private KeyCode chopKey = KeyCode.Space;
    [SerializeField] private KeyCode attackKey = KeyCode.LeftShift;
    
    private Rigidbody2D rb;
    private Creature creatureComponent;
    private SwordAnimation swordAnimation;
    private float lastActionTime = 0f;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        creatureComponent = GetComponent<Creature>();
        swordAnimation = GetComponentInChildren<SwordAnimation>();
        
        // Disable AI behavior from the Creature component if it exists
        if (creatureComponent != null)
        {
            // We'll set the creature's energyMeter to max to allow actions
            creatureComponent.energyMeter = creatureComponent.maxEnergy;
        }
    }
    
    private void Update()
    {
        // Handle action inputs
        if (Time.time > lastActionTime + actionCooldown)
        {
            // Chop action
            if (Input.GetKeyDown(chopKey))
            {
                if (creatureComponent != null && creatureComponent.TryChopTree())
                {
                    // If the creature successfully chopped, the sword animation will be triggered there
                    lastActionTime = Time.time;
                }
                else if (swordAnimation != null)
                {
                    // If no creature component or no successful chop, just animate the sword
                    swordAnimation.SwingSword();
                    lastActionTime = Time.time;
                }
            }
            
            // Attack action
            if (Input.GetKeyDown(attackKey))
            {
                if (creatureComponent != null && creatureComponent.TryAttackCreature())
                {
                    // If the creature successfully attacked, the sword animation will be triggered there
                    lastActionTime = Time.time;
                }
                else if (swordAnimation != null)
                {
                    // If no creature component or no successful attack, just animate the sword
                    swordAnimation.SwingSword();
                    lastActionTime = Time.time;
                }
            }
        }
    }
    
    private void FixedUpdate()
    {
        // Get input for movement
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        // Create movement vector
        Vector2 movement = new Vector2(horizontal, vertical);
        
        // Normalize the movement vector to prevent faster diagonal movement
        if (movement.magnitude > 1f)
        {
            movement.Normalize();
        }
        
        // Apply movement
        rb.velocity = movement * moveSpeed;
        
        // If we have a creature component, keep its energy meter full for actions
        if (creatureComponent != null)
        {
            creatureComponent.energyMeter = creatureComponent.maxEnergy;
            creatureComponent.health = creatureComponent.maxHealth; // Keep health maxed out too
        }
    }
} 