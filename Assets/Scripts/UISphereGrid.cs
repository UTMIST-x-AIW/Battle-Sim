using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

public class UISphereGrid : MonoBehaviour
{
    [Header("Tilemap Settings")]
    #region TileMap Variables
    public Tilemap tilemap;
    private BoundsInt bounds;
    #endregion
    
    [Header("Texture Settings")]
    #region Texture Variables
    [SerializeField] Material KaiMaterial;
    [SerializeField] Material AlbertMaterial;
    [SerializeField, Range(0,0.05f)] float TextureSize;
    [SerializeField] bool ShowTexture;
    [SerializeField] GameObject FullTexture;
    [SerializeField] bool AlbertSpawnMapBool = false;
    [SerializeField] bool KaiSpawnMapBool = false;
    #endregion
    
    [Header("Point Settings")]
    #region Point Variables
    [SerializeField] Transform KaiSpawnPoint;
    [SerializeField] Transform AlbertSpawnPoint;
    [SerializeField, Range(0, 0.8f)] 
    float pointSize = 0.2f;
    #endregion
    
    public void LoadAlbertMap()
    {
        if (GameObject.Find("AlbertSpawnPoint(Clone)") != null && AlbertSpawnMapBool)
        {
            foreach (Transform child in this.transform)
            {
                if (child.name == "AlbertSpawnPoint(Clone)") child.gameObject.GetComponent<MeshRenderer>().enabled = true;
           }
            return;
        }
       
        BoundsInt bounds = tilemap.cellBounds;
        AlbertMaterial.SetFloat("_TextureSamplingScale", TextureSize);
        AlbertMaterial.SetInt("_AlbertSpawnMapEnabled", 1);
        if (ShowTexture) FullTexture.SetActive(true);
        //KaiMaterial.SetInt("_KaiSpawnMapEnabled", 0);
        for (int x = (int)bounds.xMin; x < (int)bounds.xMax; x++)
        {
            for (int y = (int)bounds.yMin; y < (int)bounds.yMax; y++)
            {
                Vector3Int gridPosition = new Vector3Int(x, y, 0);
                TileBase tile = tilemap.GetTile(gridPosition);
                if (tile != null)
                {
                Vector3 position =tilemap.CellToWorld(gridPosition) + new Vector3(0,0.5f,-0.1f); 
                Transform point = Instantiate(AlbertSpawnPoint, position, Quaternion.identity);
                point.localScale = new Vector3(pointSize, pointSize, 1);
                point.gameObject.SetActive(true);
                point.SetParent(this.transform, true);
                }
            }
        }
    }
    public void LoadKaiMap()
    {
        if (GameObject.Find("KaiSpawnPoint(Clone)") != null && KaiSpawnMapBool) 
        {
            foreach (Transform child in this.transform)
            {
                if (child.name == "KaiSpawnPoint(Clone)") child.gameObject.GetComponent<MeshRenderer>().enabled = true;
            }
            return;
        }

        BoundsInt bounds = tilemap.cellBounds;
        KaiMaterial.SetFloat("_TextureSamplingScale", TextureSize);
        KaiMaterial.SetInt("_KaiSpawnMapEnabled", 1);
        if (ShowTexture) FullTexture.SetActive(true);
      //  AlbertMaterial.SetInt("_AlbertSpawnMapEnabled",0);
        for (int x = (int)bounds.xMin; x < (int)bounds.xMax; x++)
        {
            for (int y = (int)bounds.yMin; y < (int)bounds.yMax; y++)
            {
                Vector3Int gridPosition = new Vector3Int(x, y, 0);
                TileBase tile = tilemap.GetTile(gridPosition);
                if (tile != null)
                {
                    Vector3 position =tilemap.CellToWorld(gridPosition) + new Vector3(0,0.5f,-0.1f); 
                    Transform point = Instantiate(KaiSpawnPoint, position, Quaternion.identity);
                    point.localScale = new Vector3(pointSize, pointSize, 1);
                    point.gameObject.SetActive(true);
                    point.SetParent(this.transform, true);
                }
            }
        }
    }

    public void KillChildren()
    {
        while (this.transform.childCount > 0)
        {
            DestroyImmediate(this.transform.GetChild(0).gameObject);
        }
        AlbertMaterial.SetInt("_AlbertSpawnMapEnabled", 0);
        KaiMaterial.SetInt("_KaiSpawnMapEnabled", 0);
        FullTexture.gameObject.SetActive(false);
    }

    public void HideChildren()
    {
        if (this.gameObject.transform.childCount == 0) return;
        int counter = 0;
        for (int i = 0; i < this.transform.childCount; i++)
        {
            this.gameObject.transform.GetChild(counter).GetComponent<MeshRenderer>().enabled = false;
            counter++;
        }
        FullTexture.gameObject.SetActive(false);   
    }
}