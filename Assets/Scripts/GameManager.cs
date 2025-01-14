using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class GameManager : MonoBehaviour
{
    #region Variables
    enum Mode
    {
        ML,
        NEAT
    }
    [SerializeField] 
    Mode AIMode = new Mode();

    [SerializeField] 
    Transform SpawnPoint;

    [SerializeField]
    GameObject PlayerPrefab;
    #endregion
    void Update()
    {
        Instantiate(PlayerPrefab);
    }
}
