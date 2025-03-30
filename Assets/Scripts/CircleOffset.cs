using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleOffset : MonoBehaviour
{
    [SerializeField]
    float XOffset, YOffset;

    // Update is called once per frame
    void Update()
    {
        this.transform.position = new Vector2(XOffset, YOffset);
    }
}
