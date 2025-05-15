using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "TileMapPosition", menuName = "Tiles/Positions")]
public class TilePosData : ScriptableObject
{
    [SerializeField] private List<TilePos> _tilePositions = new List<TilePos>();

    [Serializable]
    public class TilePos
    {
        public Vector2 pos;
    }

    public IReadOnlyList<TilePos> TilePositions => _tilePositions;
    void Start()
    {
        Debug.Log($"TilePosData initialized with {TilePositions?.Count} positions.");
    }


    public void Initialize(Tilemap tilemap)
    {
        if (tilemap == null)
        {
            Debug.LogError("No Tilemap was provided.");
            return;
        }

        BoundsInt bounds = tilemap.cellBounds;
        _tilePositions.Clear();
        foreach (Vector3Int cellPosition in bounds.allPositionsWithin)
        {

                if (tilemap.HasTile(cellPosition))
                {
                    Vector3 worldPos = tilemap.CellToWorld(cellPosition);
                    _tilePositions.Add(new TilePos { pos = new Vector2(worldPos.x, worldPos.y) });
                }
        }
          
        Debug.Log($"Stored {_tilePositions.Count} tile positions.");
    }
}

