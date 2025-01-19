using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class UISphereGrid : MonoBehaviour
{
    #region TileMap Variables
    public Tilemap tilemap;
    private BoundsInt bounds;
    #endregion
    
    #region Texture Variables
    [SerializeField] 
    Material samplingMaterial;

    [SerializeField, Range(0,0.05f)] 
    float TextureSize;

    [SerializeField] 
    bool ShowViewer;
    [SerializeField] Transform viewer;
    #endregion
    
    [SerializeField]
    Transform startPosition;  
    [SerializeField, Range(0, 0.8f)] 
    float pointSize = 0.2f;

    public void LoadMapper()
    {
        if (this.gameObject.transform.childCount > 0)
        {
            KillChildren();
        }

        if (ShowViewer)
        {
            viewer.gameObject.SetActive(true);
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
                Vector3 position =    tilemap.CellToWorld(gridPosition) + new Vector3(0,0.5f,-0.1f); 
                Transform point = Instantiate(startPosition, position, Quaternion.identity);
                point.localScale = new Vector3(pointSize, pointSize, 1);
                point.gameObject.SetActive(true);
                point.SetParent(this.transform, true);
                }
            }
        }
        
    }
    

    public void KillChildren()
    {
        
        while (this.gameObject.transform.childCount > 0)
        {
            DestroyImmediate(this.gameObject.transform.GetChild(0).gameObject);
        }

        viewer.gameObject.SetActive(false);
    }
}