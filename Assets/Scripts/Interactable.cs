using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    [Tooltip("Starting hit points for this object")]
    [SerializeField] public float hitPoints = 5f;
    protected float currentHP;

    // Expose currentHP for saving and loading game state
    public float CurrentHP
    {
        get => currentHP;
        set => currentHP = value;
    }

    protected Color originalColor;
    protected SpriteRenderer renderer;

    protected virtual void Start()
    {
        currentHP = hitPoints;
        renderer = GetComponent<SpriteRenderer>();
        originalColor = renderer.color;
    }

    protected virtual void OnEnable()
    {
        if (renderer == null)
        {
            renderer = GetComponent<SpriteRenderer>();
        }

        // Reset health and color each time the object is reused
        currentHP = hitPoints;

        if (renderer != null)
        {
            // If originalColor hasn't been set yet (first time or pooled object), capture it now
            if (originalColor == default(Color))
            {
                originalColor = renderer.color;
            }
            renderer.color = originalColor;
        }
    }

    public virtual void TakeDamage(float damage, Creature byWhom)
    {
        currentHP -= damage;

        // Visual feedback - could be replaced with more sophisticated effects
        // StartCoroutine(FlashOnDamage());

        AnimatingDoTweenUtilities.PlayFlashRedAnimation(gameObject);

        if (currentHP <= 0)
        {
            OnDestroyed(byWhom);
            // Let the death animation handle returning the object to the pool.
            // AnimatingDoTweenUtilities.PlayDeathAnimation(gameObject);
            Die();
        }
    }

    // Basic interaction method used by creatures. By default this simply
    // applies damage equal to the creature's chop damage.
    public virtual void Interact(Creature byWhom)
    {
        if (byWhom != null)
        {
            TakeDamage(byWhom.chopDamage, byWhom);
        }
    }

    protected virtual void OnDestroyed(Creature byWhom) { }

    public virtual void Die()
    {
        // Spawn some resources or particle effects
        // For now, just destroy the tree
        // Destroy(gameObject);
        ObjectPoolManager.ReturnObjectToPool(gameObject);
    }
}
