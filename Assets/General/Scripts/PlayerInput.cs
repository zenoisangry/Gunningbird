using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    public InputActionAsset actions;

    [Header("Movement")]
    public float groundSpeed;
    public float flightSpeed;
    public float jumpStrength;

    private float currentSpeed;
    private bool flying = false;
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
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //Get Inputs
        SideMove();
        if (flying)
        {
            VerticalMove();
        }
        else
        {
            verticalMovement = body.linearVelocity.y;
        }
        //Update Movement
        body.linearVelocity = new Vector3(sideMovement.x, verticalMovement, sideMovement.y);
    }

    void SwitchMovement(InputAction.CallbackContext ctx)
    {
        Debug.Log("called switch");
        if (flying)
        {
            flying = false;
            currentSpeed = groundSpeed;
        }
        else
        {
            flying = true;
            grounded = false;
            currentSpeed = flightSpeed;
        }
    }

    void SideMove()
    {
        sideMovement = actions["Player/Move"].ReadValue<Vector2>() * currentSpeed;
    }

    void VerticalMove()
    {
        verticalMovement = actions["Player/Fly"].ReadValue<float>() * currentSpeed;
    }
}
