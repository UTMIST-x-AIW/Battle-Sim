using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class RandomMovement : MonoBehaviour
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
        randomDirection = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f).normalized;
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.tag == "bounds")
        {
            Debug.Log("Hit Bound");
            randomDirection = -1 * randomDirection;
        }
    }
}
