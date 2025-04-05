using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("UI References")]
    public Toggle labelsToggle;  // Assign in inspector
    public Toggle detectionRadiusToggle;  // Assign in inspector

    private NEATTest neatTest;

    private void Start()
    {
        // Find NEATTest instance
        neatTest = FindObjectOfType<NEATTest>();
        
        // Initialize toggles to match current settings
        if (labelsToggle != null && neatTest != null)
        {
            labelsToggle.isOn = neatTest.showCreatureLabels;
            labelsToggle.onValueChanged.AddListener(OnLabelsToggleChanged);
        }
        
        if (detectionRadiusToggle != null && neatTest != null)
        {
            detectionRadiusToggle.isOn = neatTest.showDetectionRadius;
            detectionRadiusToggle.onValueChanged.AddListener(OnDetectionRadiusToggleChanged);
        }
    }

    private void OnLabelsToggleChanged(bool isOn)
    {
        if (neatTest != null)
        {
            neatTest.showCreatureLabels = isOn;
        }
    }

    private void OnDetectionRadiusToggleChanged(bool isOn)
    {
        if (neatTest != null)
        {
            neatTest.showDetectionRadius = isOn;
        }
    }
} 