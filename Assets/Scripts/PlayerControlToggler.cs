using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles toggling between AI and player control of selected creatures.
/// Press P to toggle control mode.
/// </summary>
public class PlayerControlToggler : MonoBehaviour
{
    [Header("Control Settings")]
    [SerializeField] private KeyCode toggleKey = KeyCode.P;
    
    public NetworkVisualizer networkVisualizer;
    private PlayerController playerController;
    private Creature controlledCreature;
    private bool isPlayerControlled = false;
    public GameObject plumbobIndicator;
    
    private void Update()
    {
        // Check for toggle key press
        if (Input.GetKeyDown(toggleKey) && networkVisualizer != null)
        {
            // Get the currently selected creature
            Creature selectedCreature = networkVisualizer.selectedCreature;
            
            if (selectedCreature != null)
            {
                // If we're already controlling a creature
                if (isPlayerControlled && controlledCreature != null)
                {
                    // If it's the same creature, toggle off player control
                    if (controlledCreature == selectedCreature)
                    {
                        DisablePlayerControl();
                    }
                    // If it's a different creature, switch control to the new creature
                    else
                    {
                        DisablePlayerControl();
                        EnablePlayerControl(selectedCreature);
                    }
                }
                // If we're not controlling any creature, enable control for the selected one
                else
                {
                    EnablePlayerControl(selectedCreature);
                }
            }
        }
    }
    
    private void EnablePlayerControl(Creature creature)
    {
        controlledCreature = creature;
        
        // Disable brain control directly first to avoid any conflicts
        controlledCreature.disableBrainControl = true;
        
        // Add PlayerController component if it doesn't exist
        playerController = creature.gameObject.GetComponent<PlayerController>();
        if (playerController == null)
        {
            playerController = creature.gameObject.AddComponent<PlayerController>();
        }

        plumbobIndicator = controlledCreature.transform.Find("plumbob").gameObject;
        
        // Show the indicator
        plumbobIndicator.SetActive(true);
        
        // Enable the controller
        playerController.enabled = true;
        isPlayerControlled = true;
    }
    
    private void DisablePlayerControl()
    {
        if (controlledCreature != null && playerController != null)
        {
            // Don't destroy the component, just disable it
            // This preserves any configured settings
            playerController.enabled = false;
            
            // Re-enable brain control
            controlledCreature.disableBrainControl = false;

            plumbobIndicator = controlledCreature.transform.Find("plumbob").gameObject;
            // Hide the indicator
            plumbobIndicator.SetActive(false);
        }
        
        controlledCreature = null;
        playerController = null;
        isPlayerControlled = false;
    }
} 