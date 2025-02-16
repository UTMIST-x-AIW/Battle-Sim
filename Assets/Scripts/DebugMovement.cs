using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;

public class DebugMovement : MonoBehaviour
{
    private float _vInput;
    private float _hInput;

    [SerializeField]
    float MoveSpeed = 5f;

    public Vector2 lastdirection;


    // Update is called once per frame
     void Update()
    {
        Vector2 movementdir = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")); 
        
        lastdirection = movementdir;
        Debug.Log(movementdir + ": " + lastdirection);
        _vInput = Input.GetAxis("Vertical") * MoveSpeed;
        _hInput = Input.GetAxis("Horizontal") * MoveSpeed; 
        this.transform.Translate(Vector3.up * _vInput * Time.deltaTime);
        this.transform.Translate(Vector3.right * _hInput * Time.deltaTime);

    }
}
