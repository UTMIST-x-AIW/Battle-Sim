using UnityEngine;

public class TreeManagerSetup : MonoBehaviour
{
    // A simple script that adds the TreeManager to an object 
    // in the scene if needed.
    
    // This class is just a helper to make it easy to add the 
    // necessary components to a game object.
    
    private void Reset()
    {
        // This method is called when the component is first added in the Editor
        // or when the Reset button is clicked in the inspector
        
        // Add the TreeManager component if not already present
        if (GetComponent<TreeManager>() == null)
        {
            gameObject.AddComponent<TreeManager>();
            Debug.Log("Added TreeManager component to " + gameObject.name);
        }
    }
} 