using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private float moveSpeed = 20.0f;
    private float changeInterval = 1.0f;

    private Vector2 randomDirection;
    private float timer;

    void Start()
    {
        GenerateNewDirection();
    }

    void Update()
    {
        transform.Translate(randomDirection * moveSpeed * Time.deltaTime);
        timer += Time.deltaTime;
        if (timer >= changeInterval)
        {
            timer = 0;
            GenerateNewDirection();
        }
    }

    private void GenerateNewDirection()
    {
        // Generate a new random direction
        randomDirection = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
    }
}
