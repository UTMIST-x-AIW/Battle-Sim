using UnityEngine;
using UnityEditor;

public class AddTesterComponent
{
    [MenuItem("UTMIST/Add NEAT Tester")]
    static void AddNEATTester()
    {
        // Find the GameManager GameObject
        var neatTestObj = GameObject.FindObjectOfType<GameManager>();
        
        if (neatTestObj != null)
        {
            // Add our tester component if it doesn't already exist
            if (neatTestObj.GetComponent<NEATNetworkTester>() == null)
            {
                neatTestObj.gameObject.AddComponent<NEATNetworkTester>();
                Debug.Log("Added NEATNetworkTester to GameManager GameObject.");
            }
            else
            {
                // Debug.Log("NEATNetworkTester already exists on GameManager GameObject.");
            }
        }
        else
        {
            Debug.LogError("Could not find GameManager GameObject in the scene. Please make sure it exists.");
        }
    }
} 