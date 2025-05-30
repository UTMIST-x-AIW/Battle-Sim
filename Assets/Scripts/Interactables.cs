using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interactables : MonoBehaviour
{
    // Static event that gets fired when any tree is destroyed
    [Tooltip("How much health this object has at start")]
    public float maxHealth = 5f;
    [HideInInspector]
    public float health;

    protected Color originalColor;
    protected SpriteRenderer renderer;

    protected virtual void Start()
    {
        health = maxHealth;
        renderer = GetComponent<SpriteRenderer>();
        originalColor = renderer.color;
    }

    public void TakeDamage(float damage)
    {
        health -= damage;

        // Visual feedback - could be replaced with more sophisticated effects
        // StartCoroutine(FlashOnDamage());

        AnimatingDoTweenUtilities.PlayFlashRedAnimation(gameObject);

        if (health <= 0)
        {
            AnimatingDoTweenUtilities.PlayDeathAnimation(gameObject);
            Die();
        }
    }

    private System.Collections.IEnumerator FlashOnDamage()
    {
        
        if (renderer != null)
        {
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
        // Spawn some resources or particle effects
        // For now, just destroy the tree
        Destroy(gameObject);
    }
}
