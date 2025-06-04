using UnityEngine;
using TMPro;

public class CreatureStats : MonoBehaviour
{
    // UI references
    public TextMeshProUGUI reproductionText;
    public TextMeshProUGUI energyText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI ageText;
    public TextMeshProUGUI generationText;

    public TextMeshProUGUI maxHealthText;
    public TextMeshProUGUI attackCooldownText;
    public TextMeshProUGUI attackDamageText;
    public TextMeshProUGUI movementSpeedText;

    public TextMeshProUGUI classText;

    // Cached components
    private Creature selectedCreature;
    private NetworkVisualizer networkVisualizer;
    
    void Start()
    {
        // Start with the panel inactive
        gameObject.SetActive(false);
        
        // Find the NetworkVisualizer in the scene
        networkVisualizer = FindObjectOfType<NetworkVisualizer>();
    }
    
    void Update()
    {
        // If we have a selected creature, update the text
        if (selectedCreature != null)
        {
            UpdateStatsText();
        }
        
        // Check for input to close the panel
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HideStats();
        }
    }
    
    // This method updates all the stat text elements
    private void UpdateStatsText()
    {
        if (reproductionText != null)
        {
            // Convert to percentage with no decimal places
            int reproPercent = Mathf.RoundToInt(selectedCreature.reproductionMeter * 100);
            reproductionText.text = $"Reproduction Meter: {reproPercent}%";
        }
        
        if (energyText != null)
        {
            // Convert to percentage with no decimal places
            int energyPercent = Mathf.RoundToInt((selectedCreature.energyMeter / selectedCreature.maxEnergy) * 100);
            energyText.text = $"Energy Meter: {energyPercent}%";
        }
        
        if (nameText != null)
        {
            nameText.text = $"{selectedCreature.type}";
        }
        
        if (ageText != null)
        {
            ageText.text = $"Age: {selectedCreature.Lifetime:F0} yrs";
        }
        
        if (generationText != null)
        {
            generationText.text = $"Generation: {selectedCreature.generation}";
        }
    }
    
    // Call this method to select a creature and show the panel
    public void ShowStats(Creature creature)
    {
        if (creature == null) return;
        
        selectedCreature = creature;
        gameObject.SetActive(true);
        UpdateStatsText();
    }
    
    // Call this to hide the panel
    public void HideStats()
    {
        selectedCreature = null;
        gameObject.SetActive(false);
    }
} 