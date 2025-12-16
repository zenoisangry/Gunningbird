using System.Collections;
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

    [Header("Weapons")]
    [SerializeField] private WeaponManager weaponManager;

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

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction flyAction;
    private InputAction switchMovementAction;

    private InputAction fireAction;
    private InputAction secondaryFireAction;
    private InputAction reloadAction;
    private InputAction slot1Action;
    private InputAction slot2Action;
    private InputAction slot3Action;
    private InputAction slot4Action;

    void Awake()
    {
        body = GetComponent<Rigidbody>();

        moveAction = actions["Player/Move"];
        lookAction = actions["Player/Look"];
        flyAction = actions["Player/Fly"];
        switchMovementAction = actions["Player/SwitchMovement"];

        fireAction = actions["Player/Fire"];
        secondaryFireAction = actions["Player/SecondaryFire"];
        reloadAction = actions["Player/Reload"];

        slot1Action = actions["Player/Weapon1"];
        slot2Action = actions["Player/Weapon2"];
        slot3Action = actions["Player/Weapon3"];
        slot4Action = actions["Player/Weapon4"];
    }

    void OnEnable()
    {
        moveAction.Enable();
        lookAction.Enable();
        flyAction.Enable();
        switchMovementAction.Enable();

        fireAction.Enable();
        secondaryFireAction.Enable();
        reloadAction.Enable();

        slot1Action.Enable();
        slot2Action.Enable();
        slot3Action.Enable();
        slot4Action.Enable();

        switchMovementAction.started += SwitchMovement;
        lookAction.performed += Look;

        fireAction.performed += Fire;
        secondaryFireAction.performed += SecondaryFire;
        reloadAction.performed += Reload;

        slot1Action.performed += _ => weaponManager.EquipWeapon(0);
        slot2Action.performed += _ => weaponManager.EquipWeapon(1);
        slot3Action.performed += _ => weaponManager.EquipWeapon(2);
        slot4Action.performed += _ => weaponManager.EquipWeapon(3);
    }

    void OnDisable()
    {
        switchMovementAction.started -= SwitchMovement;
        lookAction.performed -= Look;

        fireAction.performed -= Fire;
        secondaryFireAction.performed -= SecondaryFire;
        reloadAction.performed -= Reload;

        moveAction.Disable();
        lookAction.Disable();
        flyAction.Disable();
        switchMovementAction.Disable();

        fireAction.Disable();
        secondaryFireAction.Disable();
        reloadAction.Disable();

        slot1Action.Disable();
        slot2Action.Disable();
        slot3Action.Disable();
        slot4Action.Disable();
    }

    void Start() => currentSpeed = groundSpeed;

    void FixedUpdate()
    {
        if (Physics.Raycast(transform.position, Vector3.down, 1.1f))
        {
            if (!grounded && !flying) currentSpeed = groundSpeed;

            grounded = true;

            if (flying && !jumping)
            {
                flying = false;
                currentSpeed = groundSpeed;
                body.useGravity = true;
            }
        }
        else grounded = false;

        SideMove();

        if (flying && !jumping) VerticalMove();
        else verticalMovement = body.linearVelocity.y;

        body.linearVelocity =
            Quaternion.LookRotation(transform.forward, Vector3.up) *
            new Vector3(sideMovement.x, verticalMovement, sideMovement.y);

        if (Physics.Raycast(transform.position, Vector3.down, out projectionHit, 100f))
        {
            projectedPosition = projectionHit.point;
            height = projectionHit.distance;
        }
    }

    void SwitchMovement(InputAction.CallbackContext ctx)
    {
        if (flying)
        {
            body.linearVelocity = new Vector3(
                sideMovement.x,
                body.linearVelocity.y - jumpStrength * 10,
                sideMovement.y
            );
            flying = false;
            body.useGravity = true;
        }
        else
        {
            flying = true;
            currentSpeed = flightSpeed;
            body.useGravity = false;

            if (grounded)
            {
                StartCoroutine(Jump());
                body.linearVelocity = new Vector3(
                    sideMovement.x,
                    body.linearVelocity.y + jumpStrength * 6,
                    sideMovement.y
                );
            }

            grounded = false;
        }
    }

    void Look(InputAction.CallbackContext ctx)
    {
        Vector2 look = ctx.ReadValue<Vector2>();
        cameraPosition.Rotate(-look.y * camSensitivity, 0, 0);

        if (Vector3.Angle(transform.forward, cameraPosition.forward) > 90)
        {
            if (Vector3.Angle(Vector3.up, cameraPosition.forward) > Vector3.Angle(Vector3.down, cameraPosition.forward))
                cameraPosition.localRotation = Quaternion.Euler(90, 0, 0);
            else
                cameraPosition.localRotation = Quaternion.Euler(-90, 0, 0);
        }

        transform.Rotate(0, look.x * camSensitivity, 0);
    }

    void SideMove() => sideMovement = moveAction.ReadValue<Vector2>() * currentSpeed;
    void VerticalMove() => verticalMovement = flyAction.ReadValue<float>() * (currentSpeed / 3f) * 2f;

    IEnumerator Jump()
    {
        jumping = true;
        float t = 0f;
        while (t < jumpTimer)
        {
            t += Time.deltaTime;
            yield return null;
        }
        jumping = false;
    }

    void Fire(InputAction.CallbackContext ctx)
    {
        BaseWeapon weapon = weaponManager.GetCurrentWeapon();
        if (weapon != null && weapon.CanFire()) weapon.PrimaryFire();
    }

    void SecondaryFire(InputAction.CallbackContext ctx)
    {
        BaseWeapon weapon = weaponManager.GetCurrentWeapon();
        if (weapon != null && weapon.CanSecondaryFire()) weapon.SecondaryFire();
    }

    void Reload(InputAction.CallbackContext ctx)
    {
        BaseWeapon weapon = weaponManager.GetCurrentWeapon();
        if (weapon != null && weapon.CanReload()) weapon.Reload();
    }
}