using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rock : Interactable
{
    // Static event that gets fired when any tree is destroyed

    protected override void Start()
    {
        base.Start();                // initialize health, renderer, etc.

        if (GetComponent<Collider2D>() == null)
            gameObject.AddComponent<BoxCollider2D>().isTrigger = false;
        if (gameObject.tag != "Rock")
            gameObject.tag = "Rock";
    }

    protected override void OnDestroyed(Creature byWhom)
    {
        if (byWhom != null)
        {
            byWhom.ModifyMaxHealth();
        }
    }
}
