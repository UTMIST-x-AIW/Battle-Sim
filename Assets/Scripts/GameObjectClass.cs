using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameObjectClass : MonoBehaviour
{
    enum type
    {
        Albert,
        Kai,
        Tree,
        Rock
    }
    public Animator animator {get; private set;}
    
    private int HP;
    
    public void Start() 
    {
        HP = this.HP;
        animator = GetComponent<Animator>();
    }

    public void Update()
    {
        
    }

    

    public virtual void TakeDamage(int damage)
    {
        HP -= damage;
    }

    public virtual void DoDamage(int damage)
    {
        
    }

    #region Inventory Implementation
    // A dictionary to store item names and their quantities
    private Dictionary<string, int> inventoryItems = new Dictionary<string, int>();

    // Add an item to the inventory
    public void AddItem(string itemName, int quantity = 1)
    {
        if (inventoryItems.ContainsKey(itemName))
        {
            inventoryItems[itemName] += quantity;
        }
        else
        {
            inventoryItems[itemName] = quantity;
        }
    }

    // Remove an item from the inventory
    public void RemoveItem(string itemName, int quantity = 1)
    {
        if (inventoryItems.ContainsKey(itemName))
        {
            inventoryItems[itemName] -= quantity;
            if (inventoryItems[itemName] <= 0)
            {
                inventoryItems.Remove(itemName);
            }
        }
    }
    

    #endregion
    

    #region Spawning Animation
    
    void PalyingSpawning()
    {
        animator.Play("Spawn"); // Play the spawn animation of the object
        // Each of the gameobjects will have a different spawn animation that will be named Spawn
    }
    #endregion
    #region Death Animation
    void PalyingDeath()
    {
        animator.Play("Death"); // Play the death animation of the object
        // Each of the gameobjects will have a different death animation that will be named Death
    }
     // At the end of the death animation clip in unity, there will be an animation event on the last frame
     // The event will trigger this function
     void OnDeathAnimation()
     {
         Destroy(this.gameObject);
     }
    #endregion
}
