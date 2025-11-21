using System.Collections;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.Rigidbody2D;

public class PlayerInput : MonoBehaviour
{
    public InputActionAsset actions;

    [Header("Movement")]
    public float groundSpeed;
    public float flightSpeed;
    public float jumpStrength;
    public float jumpTimer;

    private float currentSpeed;
    private bool flying = false;
    private bool jumping = false;
    private bool grounded = true;
    private Vector2 sideMovement = Vector2.zero;
    private float verticalMovement = 0;

    private Rigidbody body;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Set variables
        currentSpeed = groundSpeed;

        //Get component references
        body = GetComponent<Rigidbody>();

        //Set actions
        actions["Player/SwitchMovement"].started += SwitchMovement;
        actions["Player/Look"].performed += Look;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //Check if grounded
        if (Physics.Raycast(transform.position, Vector3.down, 1.1f, LayerMask.NameToLayer("Player")))
        {
            if (!grounded && !flying)
            {
                currentSpeed = groundSpeed;
            }
            grounded = true;
            if (flying && !jumping)
            {
                flying = false;
                currentSpeed = groundSpeed;
                body.useGravity = true;
            }
        }
        else
        {
            grounded = false;
        }

            //Get Inputs
            SideMove();
        if (flying && !jumping)
        {
            VerticalMove();
        }
        else
        {
            verticalMovement = body.linearVelocity.y;
        }
        //Update Movement
        body.linearVelocity = Quaternion.LookRotation(transform.forward, Vector3.up) * new Vector3(sideMovement.x, verticalMovement, sideMovement.y);
    }

    void SwitchMovement(InputAction.CallbackContext ctx)
    {
        Debug.Log("called switch");
        if (flying)
        {
            body.linearVelocity = new Vector3(sideMovement.x, body.linearVelocity.y - jumpStrength * 10, sideMovement.y);
            flying = false;
            body.useGravity = true;
        }
        else
        {
            flying = true;
            currentSpeed = flightSpeed;
            body.useGravity = false;
            //Jump if grounded
            if (grounded)
            {
                StartCoroutine(Jump());
                body.linearVelocity = new Vector3(sideMovement.x, body.linearVelocity.y + jumpStrength * 6, sideMovement.y);
            }
            grounded = false;
        }
    }

    void Look(InputAction.CallbackContext ctx)
    {
        transform.Rotate(new Vector3(0, ctx.ReadValue<Vector2>().x, 0));
    }

    void SideMove()
    {
        sideMovement = actions["Player/Move"].ReadValue<Vector2>() * currentSpeed;
    }

    void VerticalMove()
    {
        verticalMovement = actions["Player/Fly"].ReadValue<float>() * (currentSpeed/3)*2;
    }

    IEnumerator Jump()
    {
        jumping = true;
        float timeElapsed = 0;
        while (timeElapsed < jumpTimer)
        {
            timeElapsed += Time.deltaTime;
            yield return true;
        }
        jumping = false;
    }
}
