using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public sealed class Movement : MonoBehaviour
{

    private Animator _anim;
    private Rigidbody2D _rb;

    private SwordAnimation swordAnimation;
    private Transform swordPos;

    private float _hInput;
    private float _vInput;

    private enum MovementState {
    TopRight,
    TopLeft,
    BottomRight,
    BottomLeft
    }

    private MovementState currentMovement;

    public Vector2 lastdirection;
    private void OnEnable()
    {
        _anim = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody2D>();
        swordAnimation = gameObject?.GetComponentInChildren<SwordAnimation>();
        if (swordAnimation != null) swordPos = swordAnimation.transform;
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        _hInput = _rb.velocity.x;
        _vInput = _rb.velocity.y;
        WaypointEntry entry;

        SetEnumState(_rb.velocity);
        
        _anim.SetFloat("MoveX", _hInput);
        _anim.SetFloat("MoveY", _vInput);
        _anim.SetBool("IsMoving", _rb.velocity.magnitude > 0.1);
        if (swordAnimation == null) return;
        switch (currentMovement){
			case MovementState.BottomLeft:
				 entry = swordAnimation.waypointEntries[0];
				swordPos.position = entry.waypointTransform.position;
				swordPos.rotation = Quaternion.Euler(0, 0, 60);
				break;
			case MovementState.BottomRight:
				entry = swordAnimation.waypointEntries[1];
				swordPos.position = entry.waypointTransform.position;
				swordPos.rotation = Quaternion.Euler(0, 180, 60);
				break;
            case MovementState.TopLeft:
                entry = swordAnimation.waypointEntries[2];
                swordPos.position = entry.waypointTransform.position;
                swordPos.rotation = Quaternion.Euler(0,0,60);
                break;
			case MovementState.TopRight:
				 entry = swordAnimation.waypointEntries[3];
				swordPos.position = entry.waypointTransform.position;
				swordPos.rotation = Quaternion.Euler(0, 180, 60);
				break;
		}
    }

    void SetEnumState(Vector2 velocity){
        if (velocity.x > 0.1f && velocity.y > 0.1) currentMovement = MovementState.TopRight;
        else if (velocity.x > 0.1f && velocity.y < 0.1) currentMovement = MovementState.BottomRight;
        else if (velocity.x < 0.1f && velocity.y > 0.1) currentMovement = MovementState.TopLeft;
        else if (velocity.x < 0.1f && velocity.y < 0.1) currentMovement = MovementState.BottomLeft;
    }

    void SwitchMovementState(){

    }

}
