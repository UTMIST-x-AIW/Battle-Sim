using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManualMovement : MonoBehaviour
{
    private float moveSpeed = 50f;
    private float recoilDuration = 0.5f;
    private float recoilTimer = 0f;

    private Vector2 movement;
    private Rigidbody rb;
    private bool canMove = true;
    private Vector2 lastDirection;
    
    private bool isFacingRight = true;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {

        if (recoilTimer > 0)
        {
            recoilTimer -= Time.deltaTime;
            movement = -lastDirection;
        }
        else
        {
            canMove = true;
        }
        if (canMove)
        {
            movement.x = Input.GetAxisRaw("Horizontal");
            movement.y = Input.GetAxisRaw("Vertical");

            movement = movement.normalized;
            lastDirection = movement;
            
            HandleFlipping();
        }

        rb.AddForce(movement*moveSpeed);

        // Frictional Force
        rb.AddForce(-rb.velocity);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("bounds"))
        {
            canMove = false;
            recoilTimer = recoilDuration;
            rb.velocity = Vector2.zero;
        }
    }
    
    private void HandleFlipping()
    {
        if (movement.x < 0 && isFacingRight)
        {
            Flip();
        }
        else if (movement.x > 0 && !isFacingRight)
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
}
