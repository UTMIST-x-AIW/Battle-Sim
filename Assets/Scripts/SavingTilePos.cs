using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class SavingTilePos : MonoBehaviour
{

    public TilePosData tiledata;
    [SerializeField] Tilemap[] tilemaps;
    [SerializeField] int boundaryLength;
    // Start is called before the first frame update
    void OnEnable()
    {
        // Only initialize if the tile data is empty
        if (tiledata != null && tiledata.TilePositions.Count == 0)
        {
            Debug.Log("No saved tile positions found. Initializing now...");
            tiledata.Initialize(tilemaps[0], boundaryLength);
        }
        else
        {
            Debug.Log($"Loaded {tiledata.TilePositions.Count} saved tile positions.");
        }
    }

    void RecalculatePos(int tilemapNum)
    {
        if (tilemapNum < tilemaps.Length && tilemapNum >= 0)
        {
            Debug.Log($"Recalculating positions for tilemap {tilemapNum}...");
            tiledata.Initialize(tilemaps[tilemapNum], boundaryLength);
            Debug.Log($"Recalculation complete. Stored {tiledata.TilePositions.Count} tile positions.");
        }
        else
        {
            Debug.LogError($"Invalid tilemap index: {tilemapNum}. Valid range is 0-{tilemaps.Length - 1}");
        }
    }

    // Public method to force recalculation through the Inspector or other scripts
    public void ForceRecalculatePositions()
    {
        Debug.Log("Forcing recalculation of tile positions...");
        RecalculatePos(0); // Use the first tilemap by default
    }

}
