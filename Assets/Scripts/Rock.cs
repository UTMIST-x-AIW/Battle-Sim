using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rock : Interactables
{
    // Static event that gets fired when any tree is destroyed

    protected override void Start()
    {
        base.Start();                // initialize health, renderer, etc.

        // Tree-specific setup:
        if (GetComponent<Collider2D>() == null)
            gameObject.AddComponent<BoxCollider2D>().isTrigger = false;
        if (gameObject.tag != "Cupcake")
            gameObject.tag = "Cupcake";
    }
}
