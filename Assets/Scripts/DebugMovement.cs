using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
//using static UnityEditor.Searcher.SearcherWindow.Alignment;

public class DebugMovement : MonoBehaviour
{
    private float _vInput;
    private float _hInput;
    private Animator _anim;

    [SerializeField]
    float MoveSpeed = 5f;

    public Vector2 lastdirection;
    private void OnEnable()
    {
        _anim = GetComponent<Animator>();
    }
    // Update is called once per frame
    void Update()
    {
        Vector2 movementdir = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        lastdirection = movementdir;
        _vInput = Input.GetAxis("Vertical") * MoveSpeed;
        _hInput = Input.GetAxis("Horizontal") * MoveSpeed;
        //this.transform.Translate(Vector3.up * _vInput * Time.deltaTime);
        //this.transform.Translate(Vector3.right * _hInput * Time.deltaTime);
        Vector2 movement = new Vector2(
        _hInput - _vInput,   
            (_hInput + _vInput) / 2 ).normalized;
        this.transform.Translate(movement*Time.deltaTime);
        _anim.SetFloat("MoveX", movement.x);
        _anim.SetFloat("MoveY", movement.y);
        _anim.SetBool("IsMoving", movement.sqrMagnitude > 0);
    }

}
