using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public sealed class Movement : MonoBehaviour
{

    private Animator _anim;
    private Rigidbody2D _rb;

    private ToolAnimation toolAnimation;
    private Transform toolPos;

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
        toolAnimation = gameObject?.GetComponentInChildren<ToolAnimation>();
        if (toolAnimation != null) toolPos = toolAnimation.transform;
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
        if (toolAnimation == null) return;
        
        // Only update tool position/rotation if animation is not playing
        if (!toolAnimation.IsAnimationPlaying())
        {
            switch (currentMovement){
                case MovementState.BottomLeft:
                    entry = toolAnimation.waypointEntries[0];
                    toolPos.position = entry.waypointTransform.position;
                    toolPos.rotation = Quaternion.Euler(0, 0, 60);
                    break;
                case MovementState.BottomRight:
                    entry = toolAnimation.waypointEntries[1];
                    toolPos.position = entry.waypointTransform.position;
                    toolPos.rotation = Quaternion.Euler(0, 180, 60);
                    break;
                case MovementState.TopLeft:
                    entry = toolAnimation.waypointEntries[2];
                    toolPos.position = entry.waypointTransform.position;
                    toolPos.rotation = Quaternion.Euler(0,0,60);
                    break;
                case MovementState.TopRight:
                    entry = toolAnimation.waypointEntries[3];
                    toolPos.position = entry.waypointTransform.position;
                    toolPos.rotation = Quaternion.Euler(0, 180, 60);
                    break;
            }
        }
        else
        {
            // If animation is playing, we still need to update the position
            // But leave the rotation to the animation
            switch (currentMovement){
                case MovementState.BottomLeft:
                    toolPos.position = toolAnimation.waypointEntries[0].waypointTransform.position;
                    break;
                case MovementState.BottomRight:
                    toolPos.position = toolAnimation.waypointEntries[1].waypointTransform.position;
                    break;
                case MovementState.TopLeft:
                    toolPos.position = toolAnimation.waypointEntries[2].waypointTransform.position;
                    break;
                case MovementState.TopRight:
                    toolPos.position = toolAnimation.waypointEntries[3].waypointTransform.position;
                    break;
            }
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
