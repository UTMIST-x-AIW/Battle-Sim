using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cupcake : Interactable
{
    // Static event that gets fired when any tree is destroyed

    protected override void Start()
    {
        base.Start();                // initialize health, renderer, etc.

        if (GetComponent<Collider2D>() == null)
            gameObject.AddComponent<BoxCollider2D>().isTrigger = false;
        if (gameObject.tag != "Cupcake")
            gameObject.tag = "Cupcake";
    }

    protected override void OnDestroyed(Creature byWhom)
    {
        if (byWhom != null)
        {
            byWhom.ModifyMoveSpeed();
            byWhom.ModifyHealth();
        }
    }
}
