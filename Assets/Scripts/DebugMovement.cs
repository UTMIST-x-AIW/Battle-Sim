using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;

public class DebugMovement : MonoBehaviour
{
    private float _vInput;
    private float _hInput;

    public float sizeScaling = 1f;

    [SerializeField]
    public float MoveSpeed = 5f;

    public Vector2 lastdirection;


    // Update is called once per frame
    void Update()
    {
        Vector2 movementdir = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (movementdir != Vector2.zero) lastdirection = movementdir;
    
        _vInput = Input.GetAxis("Vertical") * MoveSpeed;
        _hInput = Input.GetAxis("Horizontal") * MoveSpeed;
    
        // Move the player
        this.transform.Translate(Vector3.up * _vInput * Time.deltaTime);
        this.transform.Translate(Vector3.right * _hInput * Time.deltaTime);

        // Flip the player when moving left or right
        if (_hInput < 0)
            this.transform.localScale = new Vector3(-1f, 1f, 1f) * sizeScaling;  // Flip to the left
        else if (_hInput > 0)
            this.transform.localScale = new Vector3(1f, 1f, 1f) * sizeScaling;   // Flip to the right
    }
}