/*using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class TilemapLooper : MonoBehaviour
{
    public Tilemap tilemap; // Assign your Tilemap in the Inspector
    public Mesh gizmoMesh;
    private List<Vector3> tilePositions = new List<Vector3>();
   
    void TileLoop()
    {
        float maxY = tilemap.cellBounds.max.y;
        float maxX;
        // Get the bounds of the tilemap in grid space
        BoundsInt bounds = tilemap.cellBounds;
      //  Debug.Log(bounds.xMin + ", " + bounds.xMax + ", " + bounds.yMin + ", " + bounds.yMax);
        // Loop through each position in the bounds
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int gridPosition = new Vector3Int(x, y, 0);

                // Check if a tile exists at this position
                if (tilemap.GetTile(gridPosition) != null)
                {
                    
                    // Get the world position of the tile center
                    Vector3 worldPosition = tilemap.CellToWorld(gridPosition);
                    tilePositions.Add(worldPosition);
                   // Debug.Log(worldPosition);
                    // Draw the mesh at the tile center
                }
            }
        }
    }

    void Start()
    {
        TileLoop();
    }
}*/