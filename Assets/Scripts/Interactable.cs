using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

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

    private void OnEnable()
    {
        // Reset state when the object is reused from the pool
        currentHP = hitPoints;
        if (renderer == null)
        {
            renderer = GetComponent<SpriteRenderer>();
        }
        if (renderer != null)
        {
            renderer.color = originalColor;
        }
    }

    private void OnDisable()
    {
        // Ensure any running tweens are cleaned up when returned to the pool
        DG.Tweening.DOTween.Kill(gameObject);
    }

    public virtual void TakeDamage(float damage, Creature byWhom)
    {
        currentHP -= damage;

        // Visual feedback - could be replaced with more sophisticated effects
        // StartCoroutine(FlashOnDamage());

        AnimatingDoTweenUtilities.PlayFlashRedAnimation(gameObject);

        if (currentHP <= 0)
        {
            //AnimatingDoTweenUtilities.PlayDeathAnimation(gameObject);
            OnDestroyed(byWhom);
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
        // Return to the object pool instead of destroying
        ObjectPoolManager.ReturnObjectToPool(gameObject);
    }
}
