using System;
using System.Collections.Generic;
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

    public void Initialize(Tilemap tilemap, int boundsize)
    {
        if (tilemap == null)
        {
            Debug.LogError("No Tilemap was provided.");
            return;
        }

        _tilePositions.Clear();
        for (int x = -boundsize; x < boundsize; x++)
        {
            for (int y = -boundsize; y < boundsize; y++)
            {
                Vector3Int cellPosition = new Vector3Int(x, y, 0);
                if (tilemap.HasTile(cellPosition))
                {
                    Vector3 worldPos = tilemap.CellToWorld(cellPosition);
                    _tilePositions.Add(new TilePos { pos = new Vector2(worldPos.x, worldPos.y) });
                }
            }

        }
        Debug.Log($"Stored {_tilePositions.Count} tile positions.");
    }
}
