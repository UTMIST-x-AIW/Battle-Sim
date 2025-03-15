using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stats : MonoBehaviour
{
    public float health = 100f;
    public float maxHealth = 100f;
    public float range = 1.5f;
    public float moveSpeed = 5f;
    public float attackCooldown = 3f;
    public float damage = 50f;

    private bool hasUpgradedClass = false;

    private float tankThreshold = 200f;
    private float tankScaling = 1.2f;
    
    private float scoutThreshold = 10f;
    
    private float swordsmanThreshold = 100f;
    [SerializeField] private GameObject basicSword;
    [SerializeField] private GameObject upgradedSword;
    [SerializeField] private GameObject upgradedBow;
    [SerializeField] private GameObject arrow;
    [SerializeField] private GameObject glasses;
    
    private float archerThreshold = 1f;
    

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

        if (maxHealth >= tankThreshold)
        {
            MakeTank();
        }
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
        
        if (moveSpeed >= scoutThreshold)
        {
            MakeScout();
        }
    }
    public void UpdateDamage(float value)
    {
        damage += value;
        damage = Mathf.Max(damage, 0);
        
        if (damage >= swordsmanThreshold)
        {
            MakeSwordsman();
        }
    }
    
    public void UpdateAttackCooldown(float value)
    {
        attackCooldown -= value;
        attackCooldown = Mathf.Max(attackCooldown, 0);
        
        if (attackCooldown <= archerThreshold)
        {
            MakeArcher();
        }
    }

    private void MakeTank()
    {
        if (!hasUpgradedClass)
        {
            hasUpgradedClass = true;
            gameObject.transform.localScale = new Vector3(-1f, 1f, 1f) * tankScaling;
            gameObject.GetComponent<DebugMovement>().sizeScaling = tankScaling;
        }
    }
    
    private void MakeScout()
    {
        if (!hasUpgradedClass)
        {
            hasUpgradedClass = true;
            // gameObject.transform.localScale = new Vector3(-1f, 1f, 1f)* 0.67f;
            // gameObject.GetComponent<DebugMovement>().sizeScaling = 0.67f;
            glasses.SetActive(true);
        }
    }
    
    private void MakeSwordsman()
    {
        if (!hasUpgradedClass)
        {
            hasUpgradedClass = true;
            basicSword.SetActive(false);
            upgradedBow.SetActive(false);
            arrow.SetActive(false);
            upgradedSword.SetActive(true);
        }
    }
    
    private void MakeArcher()
    {
        if (!hasUpgradedClass)
        {
            hasUpgradedClass = true;
            basicSword.SetActive(false);
            upgradedSword.SetActive(false);
            upgradedBow.SetActive(true);
            arrow.SetActive(true);
            gameObject.GetComponent<PlayerAttack>().attackRange = 4.5f;
            gameObject.GetComponentInChildren<Sword>().isArcher = true;
            gameObject.GetComponentInChildren<Arrow>().isArcher = true;
            
        }
    }
    
    
}
