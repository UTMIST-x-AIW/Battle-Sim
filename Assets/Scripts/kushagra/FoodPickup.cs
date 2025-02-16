using System;
using UnityEngine;

public class FoodPickup : MonoBehaviour
{
    public float speedIncrease = 2.5f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"Trigger detected with {other.gameObject.name}");
        Stats playerStats = other.gameObject.GetComponent<Stats>();
        if (playerStats != null)
        {
            playerStats.UpdateMoveSpeed(speedIncrease);
        }
        DebugMovement movement = other.gameObject.GetComponent<DebugMovement>();
        if (movement != null)
        {
            movement.MoveSpeed += speedIncrease;
            Destroy(gameObject);
        }
    }
}
