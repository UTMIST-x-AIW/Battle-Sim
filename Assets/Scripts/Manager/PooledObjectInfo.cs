using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PooledObjectInfo
{
    public string LookupString;
    public List<GameObject> ActiveObjects = new List<GameObject>();
    public List<GameObject> InactiveObjects = new List<GameObject>();
}
