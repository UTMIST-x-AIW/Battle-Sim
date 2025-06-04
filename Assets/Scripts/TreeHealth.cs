using UnityEngine;
using System;

public class TreeHealth : Interactable
{
    // Static event that gets fired when any tree is destroyed

    protected override void Start()
    {
        base.Start();                // initialize health, renderer, etc.

        // Tree-specific setup:
        if (GetComponent<Collider2D>() == null)
            gameObject.AddComponent<BoxCollider2D>().isTrigger = false;
        if (gameObject.tag != "Tree")
            gameObject.tag = "Tree";
    }

    protected override void OnDestroyed(Creature byWhom)
    {
        if (byWhom != null)
        {
            byWhom.ModifyAttackCooldown();
        }
    }

}
