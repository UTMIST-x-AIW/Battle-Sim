using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class RandomMovement : MonoBehaviour
{
    private float moveSpeed = 50.0f;
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

    void GenerateNewDirection()
    {
        float randomAngle = Random.Range(0f, Mathf.PI * 2);
        randomDirection = new Vector3(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle), 0f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.tag == "bounds")
        {
            randomDirection = -1 * randomDirection;
        }
    }
}
