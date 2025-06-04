using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    [Tooltip("Starting hit points for this object")]
    [SerializeField] public float hitPoints = 5f;
    protected float currentHP;

    protected Color originalColor;
    protected SpriteRenderer renderer;

    protected virtual void Start()
    {
        currentHP = hitPoints;
        renderer = GetComponent<SpriteRenderer>();
        originalColor = renderer.color;
    }

    public virtual void TakeDamage(float damage, Creature byWhom)
    {
        currentHP -= damage;

        // Visual feedback - could be replaced with more sophisticated effects
        // StartCoroutine(FlashOnDamage());

        AnimatingDoTweenUtilities.PlayFlashRedAnimation(gameObject);

        if (currentHP <= 0)
        {
            AnimatingDoTweenUtilities.PlayDeathAnimation(gameObject);
            OnDestroyed(byWhom);
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

    protected virtual void OnDestroyed(Creature byWhom) { }

    public virtual void Die()
    {
        // Spawn some resources or particle effects
        // For now, just destroy the tree
        Destroy(gameObject);
    }
}
