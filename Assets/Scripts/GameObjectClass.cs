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

    private int HP;

    public virtual void TakeDamage(int damage)
    {
        HP -= damage;
    }

    public virtual void DoDamage(int damage)
    {
        
    }
    
    // A dictionary to store item names and their quantities
    private Dictionary<string, int> Items = new Dictionary<string, int>();

    // Add an item to the inventory
    public void AddItem(string itemName, int quantity = 1)
    {
        if (Items.ContainsKey(itemName))
        {
            Items[itemName] += quantity;
        }
        else
        {
            Items[itemName] = quantity;
        }
    }

    // Remove an item from the inventory
    public void RemoveItem(string itemName, int quantity = 1)
    {
        if (Items.ContainsKey(itemName))
        {
            Items[itemName] -= quantity;
            if (Items[itemName] <= 0)
            {
                Items.Remove(itemName);
            }
        }
    }

}
