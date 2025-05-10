using UnityEngine;

public class Cherry : MonoBehaviour
{
    public float healthRestoration = 1f;
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        Creature creature = other.GetComponent<Creature>();
        if (creature != null)
        {
            // Restore health, capped at maxHealth
            creature.health = Mathf.Min(creature.health + healthRestoration, creature.maxHealth);
            ObjectPoolManager.ReturnObjectToPool(gameObject);
        }
    }
} 