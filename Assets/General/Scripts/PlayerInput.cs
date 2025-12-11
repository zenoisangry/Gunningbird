using System.Collections;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    public InputActionAsset actions;

    [Header("Camera")]
    public float camSensitivity;
    public Transform cameraPosition;

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

    [Header("Projected position for pathfinding")]
    public Vector3 projectedPosition = Vector3.zero;
    public float height;
    private RaycastHit projectionHit;

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

        //Update projected position
        if (Physics.Raycast(transform.position, Vector3.down, out projectionHit, 100f,  LayerMask.NameToLayer("Player")))
        {
            projectedPosition = projectionHit.point;
            height = projectionHit.distance;
        }
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
        transform.Rotate(new Vector3(0, ctx.ReadValue<Vector2>().x*camSensitivity, 0));
        cameraPosition.Rotate(new Vector3(-ctx.ReadValue<Vector2>().y * camSensitivity, 0, 0));
        if (Vector3.Angle(transform.forward, cameraPosition.forward) > 90)
        {
            Debug.Log(cameraPosition.rotation.eulerAngles);
            //Come capire se l'angolo è verso l'alto o verso il basso? Guardo componente x?
            if (cameraPosition.rotation.eulerAngles.x > 270)
            {
                cameraPosition.rotation = transform.rotation;
                cameraPosition.Rotate (new Vector3 (-90, 0, 0));
            }
            else
            {
                cameraPosition.rotation = transform.rotation;
                cameraPosition.Rotate(new Vector3(90, 0, 0));
            }
        }
        //If angle is higher than 90 degrees, set rotation to identical components but 90 degrees positive or negative.
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
