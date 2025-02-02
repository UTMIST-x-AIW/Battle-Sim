using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;

public class DebugMovement : MonoBehaviour
{
    private float _vInput;
    private float _hInput;

    private float MoveSpeed = 5f;
    public Vector3 movementdir = Vector3.zero;
    public Color rayColor = Color.red;  // The color of the ray
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
     void Update()
    {
        _vInput = Input.GetAxis("Vertical") * MoveSpeed;
        _hInput = Input.GetAxis("Horizontal") * MoveSpeed; 
        movementdir = new Vector3(_hInput, _vInput, 0);
        this.transform.Translate(Vector3.up * _vInput * Time.deltaTime);
        this.transform.Translate(Vector3.right * _hInput * Time.deltaTime);
        //Debug.DrawLine(this.transform.position, this.transform.position + movementdir, rayColor);
        

    }
}
