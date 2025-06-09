using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles toggling between AI and player control of selected creatures.
/// Press P to toggle control mode.
/// </summary>
public class ChartsVisibilityToggler : MonoBehaviour
{
    [Header("Control Settings")]
    [SerializeField] private KeyCode toggleKey = KeyCode.V;

    [SerializeField] List<GameObject> charts = new();

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            foreach (GameObject chart in charts)
            {
                chart.SetActive(!chart.activeSelf);
            }
        }
    }
}
