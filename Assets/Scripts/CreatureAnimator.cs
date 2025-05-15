using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureAnimator : MonoBehaviour
{
    [Header("Animation References")]
    public SpriteRenderer spriteRenderer;    // The SpriteRenderer component 
    
    [Header("Animation Settings")]
    public float animationSpeed = 30f;       // Frames per second for animations
    public float directionChangeThreshold = 0.1f;  // Minimum velocity to change direction
    
    [Header("Sprite References")]
    public Sprite[] albertTopRightSprites;
    public Sprite[] albertTopLeftSprites;
    public Sprite[] albertBottomRightSprites;
    public Sprite[] albertBottomLeftSprites;
    public Sprite[] kaiTopRightSprites;
    public Sprite[] kaiTopLeftSprites;
    public Sprite[] kaiBottomRightSprites;
    public Sprite[] kaiBottomLeftSprites;
    
    // Cached sprites for each direction
    private Sprite[][] albertSprites; // [direction][frame]
    private Sprite[][] kaiSprites;    // [direction][frame]
    
    // Direction constants
    private const int TOP_RIGHT = 0;
    private const int TOP_LEFT = 1;
    private const int BOTTOM_RIGHT = 2;
    private const int BOTTOM_LEFT = 3;
    
    // State tracking
    private Creature creature;
    private int currentDirection = TOP_RIGHT;
    private int currentFrame = 0;
    private bool isMoving = false;
    private float frameTimer = 0f;
    private Sprite[][] currentCreatureSprites;
    
    private void Awake()
    {
        // Get required components
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        creature = GetComponent<Creature>();
        
        // Initialize the sprite arrays
        InitializeSpriteArrays();
        
        // Set initial sprites based on creature type
        SetCreatureType(creature.creatureType);
    }
    
    private void InitializeSpriteArrays()
    {
        // Initialize arrays
        albertSprites = new Sprite[4][];
        kaiSprites = new Sprite[4][];
        
        // Set arrays directly from the inspector-assigned sprites
        albertSprites[TOP_RIGHT] = albertTopRightSprites;
        albertSprites[TOP_LEFT] = albertTopLeftSprites;
        albertSprites[BOTTOM_RIGHT] = albertBottomRightSprites;
        albertSprites[BOTTOM_LEFT] = albertBottomLeftSprites;
        
        kaiSprites[TOP_RIGHT] = kaiTopRightSprites;
        kaiSprites[TOP_LEFT] = kaiTopLeftSprites;
        kaiSprites[BOTTOM_RIGHT] = kaiBottomRightSprites;
        kaiSprites[BOTTOM_LEFT] = kaiBottomLeftSprites;
    }
    
    public void SetCreatureType(Creature.CreatureType type)
    {
        // Set the appropriate sprite array based on creature type
        if (type == Creature.CreatureType.Albert)
        {
            currentCreatureSprites = albertSprites;
        }
        else
        {
            currentCreatureSprites = kaiSprites;
        }
        
        // Initialize with frame 0 of current direction
        if (currentCreatureSprites != null && 
            currentCreatureSprites[currentDirection] != null && 
            currentCreatureSprites[currentDirection].Length > 0)
        {
            spriteRenderer.sprite = currentCreatureSprites[currentDirection][0];
        }
    }
    
    private void Update()
    {
        // Get the creature's current velocity
        Rigidbody2D rb = creature.GetComponent<Rigidbody2D>();
        Vector2 velocity = rb.velocity;
        
        // Determine if the creature is moving
        isMoving = velocity.magnitude > directionChangeThreshold;
        
        if (isMoving)
        {
            // Determine the movement direction
            DetermineDirection(velocity);
            
            // Update animation based on frame rate
            frameTimer += Time.deltaTime;
            float frameInterval = 1f / animationSpeed;
            
            if (frameTimer >= frameInterval)
            {
                frameTimer = 0f;
                if (currentCreatureSprites[currentDirection] != null && 
                    currentCreatureSprites[currentDirection].Length > 0)
                {
                    currentFrame = (currentFrame + 1) % currentCreatureSprites[currentDirection].Length;
                    UpdateSprite();
                }
            }
        }
        // If not moving, stay on the first frame
        else if (currentFrame != 0)
        {
            currentFrame = 0;
            UpdateSprite();
        }
    }
    
    private void DetermineDirection(Vector2 velocity)
    {
        // Calculate angle of movement in degrees
        float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        
        // Normalize angle to 0-360 range
        if (angle < 0) angle += 360f;
        
        int newDirection;
        
        // Special case for pure horizontal movement
        if (Mathf.Abs(velocity.y) < 0.1f)
        {
            // Pure right movement uses bottom right
            if (velocity.x > 0)
                newDirection = BOTTOM_RIGHT;
            // Pure left movement uses bottom left
            else
                newDirection = BOTTOM_LEFT;
        }
        // Normal directional logic for other cases
        else if (angle >= 0 && angle < 90)
            newDirection = TOP_RIGHT;
        else if (angle >= 90 && angle < 180)
            newDirection = TOP_LEFT;
        else if (angle >= 180 && angle < 270)
            newDirection = BOTTOM_LEFT;
        else
            newDirection = BOTTOM_RIGHT;
        
        // Only update direction if it changed
        if (newDirection != currentDirection)
        {
            currentDirection = newDirection;
            // Reset frame counter when direction changes
            currentFrame = 0;
            UpdateSprite();
        }
    }
    
    private void UpdateSprite()
    {
        if (currentCreatureSprites != null && 
            currentCreatureSprites[currentDirection] != null && 
            currentFrame < currentCreatureSprites[currentDirection].Length && 
            currentCreatureSprites[currentDirection][currentFrame] != null)
        {
            spriteRenderer.sprite = currentCreatureSprites[currentDirection][currentFrame];
        }
    }
} 