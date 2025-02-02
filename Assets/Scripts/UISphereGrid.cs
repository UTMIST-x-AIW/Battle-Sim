using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

public class UISphereGrid : MonoBehaviour
{
    [Header("Tilemap Settings")]
    #region TileMap Variables
    public Tilemap tilemap;
    private BoundsInt bounds;
    #endregion
    
    [Header("Texture Settings")]
    #region Texture Variables
    [SerializeField] 
    Material samplingMaterial;

    [SerializeField, Range(0,0.05f)] 
    float TextureSize;

    [SerializeField] 
    bool ShowTexture;
    [SerializeField] Transform LookAtTexture;
    #endregion
    
    [FormerlySerializedAs("startPosition")]
    [Header("Point Settings")]
    [SerializeField]
    Transform Point;  
    [SerializeField, Range(0, 0.8f)] 
    float pointSize = 0.2f;

    public void LoadMapper()
    {
        if (this.gameObject.transform.childCount > 0)
        {
            foreach (Transform child in this.gameObject.transform) child.gameObject.SetActive(true);
            return;
        }

        if (ShowTexture)
        {
            LookAtTexture.gameObject.SetActive(true);
        }
        BoundsInt bounds = tilemap.cellBounds;
        samplingMaterial.SetFloat("_TextureSamplingScale", TextureSize);
        // Loop through each position in the bounds
        for (int x = (int)bounds.xMin; x < (int)bounds.xMax; x++)
        {
            for (int y = (int)bounds.yMin; y < (int)bounds.yMax; y++)
            {
                Vector3Int gridPosition = new Vector3Int(x, y, 0);
                TileBase tile = tilemap.GetTile(gridPosition);
                if (tile != null)
                {
                Vector3 position =tilemap.CellToWorld(gridPosition) + new Vector3(0,0.5f,-0.1f); 
                Transform point = Instantiate(Point, position, Quaternion.identity);
                point.localScale = new Vector3(pointSize, pointSize, 1);
                point.gameObject.SetActive(true);
                point.SetParent(this.transform, true);
                }
            }
        }
    }

    public void KillChildren()
    {
        while (this.transform.childCount > 0)
        {
            DestroyImmediate(this.transform.GetChild(0).gameObject);
        }
    }

    public void HideChildren()
    {
        if (this.gameObject.transform.childCount == 0) return;
        int counter = 0;
        for (int i = 0; i < this.transform.childCount; i++)
        {
            this.gameObject.transform.GetChild(counter).gameObject.SetActive(false);
            counter++;
        }
        LookAtTexture.gameObject.SetActive(false);   
    }
}