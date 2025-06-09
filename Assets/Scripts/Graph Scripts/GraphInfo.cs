using System;
using UnityEngine;
using UnityEngine.Serialization;
using XCharts.Runtime;
using Object = UnityEngine.Object;

[Serializable]
public class GraphInfo 
{
    public string Name;
    public GameObject[] prefabs;
    [HideInInspector] 
    public Serie serie;
    public Color color;
    public float Opacity;
}