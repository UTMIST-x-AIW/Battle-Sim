using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwordAnimation : MonoBehaviour
{
    [SerializeField] public WaypointEntry[] waypointEntries = new WaypointEntry[4];


}
[Serializable]
public struct WaypointEntry {
    [SerializeField] 
    public Transform waypointTransform;
    
}