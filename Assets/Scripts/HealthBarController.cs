using UnityEngine;

public class HealthBarController : MonoBehaviour
{
    private Renderer healthBarRenderer;
    private Creature creature;
    
    void Start()
    {
        healthBarRenderer = GetComponent<Renderer>();
        creature = GetComponentInParent<Creature>();
        
        if (healthBarRenderer == null || creature == null)
        {
            Debug.LogError("HealthBarController: Missing required components!");
            enabled = false;
            return;
        }
    }
    
    void Update()
    {
        // Update the health bar's visual state based on the creature's health
        float healthPercentage = creature.health / creature.maxHealth;
        healthBarRenderer.material.SetFloat("_Health", healthPercentage);
    }
} 