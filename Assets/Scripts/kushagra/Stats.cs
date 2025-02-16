using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stats : MonoBehaviour
{
    public int health = 100;
    public int maxHealth = 100;
    public int range = 1;
    public int moveSpeed = 5;
    public int attackCooldown = 5;
    public int damage = 25;

    public void UpdateHealth(int value)
    {
        health += value;
        health = Mathf.Clamp(health, 0, maxHealth);
    }

    public void UpdateMaxHealth(int value)
    {
        maxHealth += value;
        health += value;
        health = Mathf.Clamp(health, 0, maxHealth);
    }

    public void UpdateRange(int value)
    {
        range += value;
        range = Mathf.Max(range, 0);
    }

    public void UpdateMoveSpeed(int value)
    {
        moveSpeed += value;
        moveSpeed = Mathf.Max(moveSpeed, 0);
    }
    
    public void UpdateAttackCooldown(int value)
    {
        attackCooldown -= value;
        attackCooldown = Mathf.Max(attackCooldown, 0);
    }

    public void UpdateDamage(int value)
    {
        damage += value;
        damage = Mathf.Max(damage, 0);
    }
    
    
    
    
}
