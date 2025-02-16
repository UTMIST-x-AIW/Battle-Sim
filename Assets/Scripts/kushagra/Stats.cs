using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stats : MonoBehaviour
{
    public float health = 100f;
    public float maxHealth = 100f;
    public float range = 1f;
    public float moveSpeed = 5f;
    public float attackCooldown = 5f;
    public float damage = 50f;

    public void UpdateHealth(float value)
    {
        health += value;
        health = Mathf.Clamp(health, 0f, maxHealth);
    }

    public void UpdateMaxHealth(float value)
    {
        maxHealth += value;
        health += (health * (maxHealth/(maxHealth-value)));
        health = Mathf.Clamp(health, 0, maxHealth);
        
        Transform healthBar = gameObject.transform.Find("HealthBar");
        healthBar.localScale = new Vector3((maxHealth/(maxHealth-value)), 0.2f, 1f);
    }

    public void UpdateRange(float value)
    {
        range += value;
        range = Mathf.Max(range, 0);
    }

    public void UpdateMoveSpeed(float value)
    {
        moveSpeed += value;
        moveSpeed = Mathf.Max(moveSpeed, 0);
    }
    
    public void UpdateAttackCooldown(float value)
    {
        attackCooldown -= value;
        attackCooldown = Mathf.Max(attackCooldown, 0);
    }

    public void UpdateDamage(float value)
    {
        damage += value;
        damage = Mathf.Max(damage, 0);
    }
    
    
    
    
}
