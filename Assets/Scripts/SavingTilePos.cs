using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

public class SavingTilePos : MonoBehaviour
{

    public TilePosData tiledata;
    public Tilemap tilemap;
    [SerializeField]public  int boundaryLength;
    // Start is called before the first frame update
    void OnEnable()
    {
        // Only initialize if the tile data is empty
        if (tiledata != null && tiledata.TilePositions.Count == 0)
        {
            Debug.Log("No saved tile positions found. Initializing now...");
            tiledata.Initialize(tilemap, boundaryLength);
        }
        else
        {
            Debug.Log($"Loaded {tiledata.TilePositions.Count} saved tile positions.");
        }
    }

    void RecalculatePos()
    {
        if (tilemap != null)
        {
            tiledata.Initialize(tilemap, boundaryLength);
            Debug.Log($"Recalculation complete. Stored {tiledata.TilePositions.Count} tile positions.");
        }
        else
        {
            Debug.LogError("Tilemap was not assigned in the Inspector");
        }
    }

    // Public method to force recalculation through the Inspector or other scripts
    public void ForceRecalculatePositions()
    {
        Debug.Log("Forcing recalculation of tile positions...");
        RecalculatePos(); // Use the first tilemap by default
    }

}
