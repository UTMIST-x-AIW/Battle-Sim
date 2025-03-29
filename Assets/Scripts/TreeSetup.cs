using UnityEngine;

public class TreeSetup : MonoBehaviour
{
    void Start()
    {
        // Find all objects with names containing "tree"
        GameObject[] potentialTrees = GameObject.FindObjectsOfType<GameObject>();
        
        foreach (GameObject obj in potentialTrees)
        {
            if (obj.name.ToLower().Contains("tree"))
            {
                // Set the tree tag
                obj.tag = "Tree";
                
                // Add a TreeHealth component if it doesn't already have one
                if (obj.GetComponent<TreeHealth>() == null)
                {
                    obj.AddComponent<TreeHealth>();
                }
                
                // Ensure the tree has a collider
                if (obj.GetComponent<Collider2D>() == null)
                {
                    BoxCollider2D collider = obj.AddComponent<BoxCollider2D>();
                    collider.isTrigger = false;
                    
                    // Adjust collider size based on the sprite if available
                    SpriteRenderer renderer = obj.GetComponent<SpriteRenderer>();
                    if (renderer != null)
                    {
                        collider.size = renderer.bounds.size * 0.8f;
                    }
                }
                
                Debug.Log("Set up tree: " + obj.name);
            }
        }
    }
} 