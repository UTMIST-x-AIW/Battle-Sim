using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class SavingTilePos : MonoBehaviour
{

    public TilePosData tiledata;
    [SerializeField] Tilemap tilemap;
    [SerializeField, Min(10)] int TileMapBounds = 50; 
    // Start is called before the first frame update
    void OnEnable()
    {
        if (tiledata == null)
        {
            tiledata.Initialize(tilemap,TileMapBounds);
        }
    }



    //void RecalculatePos(int tilemapNum)
    //{
    //    if (tilemapNum <= tilemaps.Length && tilemapNum >= 0){ 
    //        tiledata.Initialize(tilemaps[tilemapNum], 50);
    //    }
    //}


}
