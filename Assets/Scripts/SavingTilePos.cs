using System;
using UnityEngine;
using UnityEngine.Tilemaps;

public class SavingTilePos : MonoBehaviour
{
    [ExposedScriptableObject]
    public TilePosData tileData;
    [SerializeField] Tilemap tileMap;

   // [SerializeField, Range(20, 100)] int BoundsSize = 50;
    // Start is called before the first frame update
    void OnEnable()
    {
        // Only initialize if the tile data is empty
        if (tileData != null && tileData.TilePositions.Count == 0)
        {
            Debug.Log("No saved tile positions found. Initializing now...");
            tileData.Initialize(tileMap);
        }

    }

    public void RecalculatePos()
    {
        if (tileData != null)
        { 
            Debug.Log($"Recalculating positions for tilemap {tileMap.name}...");
            tileData.Initialize(tileMap);
            Debug.Log($"Recalculation complete. Stored {tileData.TilePositions.Count} tile positions.");
        }
        
    }



}
