using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class Movement : MonoBehaviour
{

    private Animator _anim;
    private Rigidbody2D _rb;

    private float _hInput;
    private float _vInput;


    public Vector2 lastdirection;
    private void OnEnable()
    {
        _anim = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody2D>();
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        _hInput = _rb.velocity.x;
        _vInput = _rb.velocity.y;

        //Vector2 movement = new Vector2(
        //_hInput - _vInput,   
        //    (_hInput + _vInput) / 2 ).normalized;
        //this.transform.Translate(movement*Time.fixedDeltaTime);
        _anim.SetFloat("MoveX", _hInput);
        _anim.SetFloat("MoveY", _vInput);
        _anim.SetBool("IsMoving", _rb.velocity.magnitude > 0.1);
    }

}
