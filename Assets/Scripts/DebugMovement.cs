using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

public class DebugMovement : MonoBehaviour
{
    private float _vInput;
    private float _hInput;

    [SerializeField]
    float MoveSpeed = 5f;

    public Vector2 lastdirection;
    //private Dictionary<Vector2Int, float> elevation = new Dictionary<Vector2Int, float>();
    //public float z = 0;
    //public float GroundHeight;
    //public Vector3 velocity;
    //public float gravity = 9.8f;
    //public Tilemap Tilemap;
    //Transform character;

    //private void Awake()
    //{
    //    //LoadTileMapElevation(Tilemap);
    //    character = this.GetComponent<Transform>();
    //    z = transform.position.z;
    //}

    // Update is called once per frame
    void Update()
    {
        //z = transform.position.y;
        Vector2 movementdir = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        lastdirection = movementdir;
        _vInput = Input.GetAxis("Vertical") * MoveSpeed;
        _hInput = Input.GetAxis("Horizontal") * MoveSpeed;
        this.transform.Translate(Vector3.up * _vInput * Time.deltaTime);
        this.transform.Translate(Vector3.right * _hInput * Time.deltaTime);
        //Rigidbody2D rb = this.GetComponent<Rigidbody2D>();
        //rb.MovePosition(rb.position + movementdir * MoveSpeed * Time.fixedDeltaTime);
        //velocity = (Vector3.right * _hInput * Time.deltaTime + Vector3.up * _vInput * Time.deltaTime).normalized;
        //GravitySim();
    }
// Wanted to simulate gravity; I will make do it another day
    //void GravitySim()
    //{
    //    if (elevation.ContainsKey(new Vector2Int((int)transform.position.x, (int)transform.position.y)))
    //    {
    //        GroundHeight = elevation[new Vector2Int((int)transform.position.x, (int)transform.position.y)];
    //    }
    //    else
    //    {
    //        GroundHeight = 4;
    //    }
    //    if (z > GroundHeight)
    //    {
    //        z -= gravity * Time.deltaTime;  // Fall down
    //        this.transform.Translate(Vector3.down * Time.deltaTime);
    //    }
    //    else
    //    {
    //        z = GroundHeight;  // Snap to ground
    //        //character.position.z = GroundHeight;
    //    }
    //}

    //void LoadTileMapElevation(Tilemap tilemap)
    //{
    //    foreach (Vector3Int position in tilemap.cellBounds.allPositionsWithin)
    //    {
    //        if (tilemap.HasTile(position))
    //        {
    //            Vector2Int gridPos = new Vector2Int(position.x, position.y);
    //            float tileElevation = GetHeightFromTilemap(tilemap);
    //            if (!elevation.ContainsKey(gridPos))
    //            {
    //                elevation.Add(gridPos, tileElevation);
    //            }

    //        }
    //    }
    //}
    //float GetHeightFromTilemap(Tilemap tilemap)
    //{
    //    if (tilemap.name.Contains("Ground")) return 1.0f;
    //    return 0.0f;
    //}
}
