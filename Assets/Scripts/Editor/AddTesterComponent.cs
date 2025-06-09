using UnityEngine;
using UnityEditor;

public class AddTesterComponent
{
    [MenuItem("UTMIST/Add NEAT Tester")]
    static void AddNEATTester()
    {
        // Find the NEATTest GameObject
        var neatTestObj = GameObject.FindObjectOfType<NEATTest>();
        
        if (neatTestObj != null)
        {
            // Add our tester component if it doesn't already exist
            if (neatTestObj.GetComponent<NEATNetworkTester>() == null)
            {
                neatTestObj.gameObject.AddComponent<NEATNetworkTester>();
                Debug.Log("Added NEATNetworkTester to NEATTest GameObject.");
            }
            else
            {
                // Debug.Log("NEATNetworkTester already exists on NEATTest GameObject.");
            }
        }
        else
        {
            Debug.LogError("Could not find NEATTest GameObject in the scene. Please make sure it exists.");
        }
    }
} 