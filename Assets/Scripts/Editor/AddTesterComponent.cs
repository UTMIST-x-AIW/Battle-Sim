using UnityEngine;
using UnityEditor;

public class AddTesterComponent
{
    [MenuItem("UTMIST/Add NEAT Tester")]
    static void AddGameManagerer()
    {
        // Find the GameManager GameObject
        var gameManagerObj = GameObject.FindObjectOfType<GameManager>();

        if (gameManagerObj != null)
        {
            // Add our tester component if it doesn't already exist
            if (gameManagerObj.GetComponent<NEATNetworkTester>() == null)
            {
                gameManagerObj.gameObject.AddComponent<NEATNetworkTester>();
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