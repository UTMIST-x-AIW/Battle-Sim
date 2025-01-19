using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class PlayerAgent : Agent
{
    [SerializeField] private Transform target; // The position of Player 2
    [SerializeField] private float moveSpeed = 50f;
    [SerializeField] private float attackRange = 25f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private GameObject sword; // Reference to the sword object for animations

    private Rigidbody rb;
    private float attackTimer = 0f;
    private bool isFacingRight = true;
    
    // // Flag to check if in training or manual mode
    // [SerializeField] private bool isTrainingMode = true;

    private float prevReward = 0f;


    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        // Reset Player 1 position
        transform.position = new Vector3(-125, 25, 0);

        // Reset Player 1 velocity (ensure Rigidbody reference exists)
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Reset Player 2 position
        if (target != null)
        {
            target.transform.position = new Vector3(100, 0, 0);

            // Reset Player 2 velocity (if Player 2 has a Rigidbody)
            Rigidbody rb2 = target.GetComponent<Rigidbody>();
            if (rb2 != null)
            {
                rb2.velocity = Vector3.zero;
                rb2.angularVelocity = Vector3.zero;
            }
        }
        
        attackTimer = 0f;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Agent position
        sensor.AddObservation(transform.localPosition);

        // Target position
        sensor.AddObservation(target.localPosition);

        // Distance to the target
        sensor.AddObservation(Vector3.Distance(transform.localPosition, target.localPosition));

        // Whether the agent is ready to attack
        sensor.AddObservation(attackTimer <= 0f ? 1f : 0f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Continuous actions: movement
        float moveX = actions.ContinuousActions[0];
        float moveY = actions.ContinuousActions[1];

        // Discrete action: attack
        bool attack = actions.DiscreteActions[0] == 1;

        // Movement logic
        Vector3 movement = new Vector3(moveX, moveY, 0).normalized * moveSpeed * Time.deltaTime;
        rb.MovePosition(transform.position + movement);

        // Handle flipping for movement direction
        HandleFlipping(movement.x);
        
        // Penalize time taken (small negative reward for each step)
        AddReward(-0.001f);  // Small penalty for each step
        
        // Reward shaping: Penalize distance to the target
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        // AddReward(1 - 5 * 0.0001f * distanceToTarget);
        AddReward(1f - distanceToTarget/226.3846f);
        AddReward(-1 * prevReward);
        prevReward = 1f - distanceToTarget / 226.3846f;

        // Debug.Log("REWARD"+GetCumulativeReward());
            
        // Attack logic
        if (attack && attackTimer <= 0f)
        {
            AttemptAttack();
            sword.GetComponent<SwordSwing>().TriggerSwing(); // Trigger sword animation
            attackTimer = attackCooldown;
        }

        // Update attack cooldown timer
        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
        }


        // // Give reward if the agent successfully eliminates the target
        // if (distanceToTarget < attackRange && attack)
        // {
        //     AddReward(1.0f);
        //     Debug.Log("Cumulative Reward: " + (-0.001f * distanceToTarget) + 1.0f);
        //     // Debug.Log("Cumulative Reward: " + GetCumulativeReward());
        //     EndEpisode();
        // }
        // else
        // {
        //     Debug.Log("Cumulative Reward: " + (-0.001f * distanceToTarget));
        //     // Debug.Log("Cumulative Reward: " + GetCumulativeReward());
        // }
        
        

    }
    
    // public void ManualControl()
    // {
    //     if (!isTrainingMode)
    //     {
    //         // Get input from player for movement (use arrows or WASD keys)
    //         float moveX = Input.GetAxisRaw("Horizontal"); // Left/Right movement (A/D or arrow keys)
    //         float moveY = Input.GetAxisRaw("Vertical");   // Forward/Backward movement (W/S or arrow keys)
    //
    //         Vector3 movement = new Vector3(moveX, 0, moveY).normalized * moveSpeed * Time.deltaTime;
    //         rb.MovePosition(transform.position + movement);
    //
    //         // Handle flipping for movement direction
    //         HandleFlipping(moveX);
    //
    //         // Get input for attack (e.g., Space bar or Shift)
    //         if ((Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.LeftShift)) && attackTimer <= 0f)
    //         {
    //             AttemptAttack();
    //             sword.GetComponent<SwordSwing>().TriggerSwing(); // Trigger sword animation
    //             attackTimer = attackCooldown;
    //         }
    //
    //         // Update attack cooldown timer
    //         if (attackTimer > 0f)
    //         {
    //             attackTimer -= Time.deltaTime;
    //         }
    //     }
    // }

    private void AttemptAttack()
    {
        float distanceToTarget = Vector3.Distance(transform.position, target.position);

        // Check if the target is within range
        if (distanceToTarget <= attackRange)
        {
            
            AddReward(1.0f);
            // Debug.Log("Cumulative Reward: " + (-0.001f * distanceToTarget) + 1.0f);
            // Debug.Log("Cumulative Reward: " + GetCumulativeReward());
            EndEpisode();
        }
    }

    private void HandleFlipping(float moveX)
    {
        if (moveX < 0 && isFacingRight)
        {
            Flip();
        }
        else if (moveX > 0 && !isFacingRight)
        {
            Flip();
        }
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 currentScale = transform.localScale;
        currentScale.x *= -1;
        transform.localScale = currentScale;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxisRaw("Horizontal"); // X-axis movement
        continuousActions[1] = Input.GetAxisRaw("Vertical");   // Y-axis movement

        var discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.LeftShift) ? 1 : 0; // Attack action
    }
}
