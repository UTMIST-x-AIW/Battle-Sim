using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManualMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 50f;
    [SerializeField] private float smoothing = 0.1f;
    //[SerializeField] private float frictionValue = 2f;
    private float recoilDuration = 0.5f;
    private float recoilTimer = 0f;

    private Vector2 movement;
    private Rigidbody rb;
    private bool canMove = true;
    private Vector2 lastDirection;
    private Vector2 velocity = Vector2.zero;

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
            Vector2 targetDirection = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).normalized;
            velocity = Vector2.Lerp(velocity, targetDirection * moveSpeed, Time.deltaTime / smoothing); ;
            rb.velocity = velocity;

            lastDirection = movement;
            
            HandleFlipping();
        }

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
