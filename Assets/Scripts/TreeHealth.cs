using UnityEngine;
using System;

public class TreeHealth : MonoBehaviour
{
    // Static event that gets fired when any tree is destroyed
    public static event Action OnTreeDestroyed;
    
    public float maxHealth = 5f;
    public float health;
    
    private void Start()
    {
        health = maxHealth;
        
        // Add a collider if one doesn't exist
        if (GetComponent<Collider2D>() == null)
        {
            BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = false;
        }
        
        // Make sure it has the "Tree" tag
        if (gameObject.tag != "Tree")
        {
            gameObject.tag = "Tree";
        }
    }
    
    public void TakeDamage(float damage)
    {
        health -= damage;
        
        // Visual feedback - could be replaced with more sophisticated effects
        StartCoroutine(FlashOnDamage());
        
        if (health <= 0)
        {
            Die();
        }
    }
    
    private System.Collections.IEnumerator FlashOnDamage()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            Color originalColor = renderer.color;
            renderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            renderer.color = originalColor;
        }
        else
        {
            yield return null;
        }
    }
    
    private void Die()
    {
        // Invoke the event before destroying the tree
        OnTreeDestroyed?.Invoke();
        
        // Spawn some resources or particle effects
        // For now, just destroy the tree
        Destroy(gameObject);
    }
} 