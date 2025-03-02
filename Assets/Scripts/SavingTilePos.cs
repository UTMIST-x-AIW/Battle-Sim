using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class SavingTilePos : MonoBehaviour
{

    public TilePosData tiledata;
    [SerializeField] Tilemap[] tilemaps;
    // Start is called before the first frame update
    void Start()
    {
        tiledata.Initialize(tilemaps[0],50);
    }

    void RecalculatePos(int tilemapNum)
    {
        if (tilemapNum <= tilemaps.Length && tilemapNum >= 0){ 
            tiledata.Initialize(tilemaps[tilemapNum], 50);
        }
    }


}
