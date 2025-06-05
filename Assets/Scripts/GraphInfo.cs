using System;
using UnityEngine;
using XCharts.Runtime;

[Serializable]
public class GraphInfo 
{
    public string Name;
    public GameObject prefab;
    [HideInInspector] 
    public  Serie serie;
    public Color color;
    public float Opacity;
}