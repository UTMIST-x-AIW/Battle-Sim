using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class RandomMovement : MonoBehaviour
{
    private Vector2 randomDirection;
    private float moveSpeed = 50f;
    private float recoilDuration = 0.5f;
    private float recoilTimer = 0f;

    private Vector2 movement;
    private Rigidbody rb;
    private bool canMove = true;
    private Vector2 lastDirection;

    private float newDirectionTimer = 0f;
    private float newDirectionFrequency = 1f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        GenerateNewDirection();
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
            movement = randomDirection.normalized;
            lastDirection = movement;

            newDirectionTimer += Time.deltaTime;

            if (newDirectionTimer > newDirectionFrequency)
            {
                GenerateNewDirection();
                newDirectionTimer = 0f;
            }
        }

        rb.velocity = movement * moveSpeed;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("bounds"))
        {
            canMove = false;
            recoilTimer = recoilDuration;
            rb.velocity = Vector2.zero;
            GenerateNewDirection();
        }
    }
    
    void GenerateNewDirection()
    {
        float randomAngle = Random.Range(0f, Mathf.PI * 2);
        randomDirection = new Vector3(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle), 0f);
    }
}
