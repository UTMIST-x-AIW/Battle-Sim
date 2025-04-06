using UnityEngine;
using TMPro;

public class CreatureStats : MonoBehaviour
{
    // UI references
    public TextMeshProUGUI reproductionText;
    public TextMeshProUGUI energyText;
    public TextMeshProUGUI nameText;
    
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
            reproductionText.text = $"Reproduction: {selectedCreature.reproductionMeter:F1} / 1.0";
        }
        
        if (energyText != null)
        {
            energyText.text = $"Energy: {selectedCreature.energyMeter:F1} / {selectedCreature.maxEnergy:F1}";
        }
        
        if (nameText != null)
        {
            nameText.text = $"{selectedCreature.type}";
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