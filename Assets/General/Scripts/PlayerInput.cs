using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

public class PlayerInput : MonoBehaviour
{
    public InputActionAsset actions;
    public MotionLinestoggle motionlines;

    [Header("Camera")]
    public float camSensitivity;
    public Transform cameraPosition;

    [Header("Movement")]
    public float groundSpeed;
    public float flightSpeed;
    public float jumpStrength;
    public float diveStrength;

    [Header("Weapons")]
    [SerializeField] private WeaponManager weaponManager;

    [Header("Health")]
    [SerializeField] private HealthSystem healthSystem;

    [Header("Footsteps")]
    [SerializeField] private AudioSource footstepSource;
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private float footstepInterval = 0.45f;

    [Header("Flight Sounds")]
    [SerializeField] private AudioSource flightSource;
    [SerializeField] private AudioClip[] flapClips;
    [SerializeField] private float flapInterval = 0.5f;

    [Header("Dive Sound")]
    [SerializeField] private AudioSource diveSource;
    [SerializeField] private AudioClip diveClip;

    private float footstepTimer;
    private float flapTimer;

    private float currentSpeed;
    private bool flying = false;
    public bool grounded = true;
    public bool diving = false;
    private Vector2 sideMovement = Vector2.zero;
    private float verticalMovement = 0;
    private bool isDead = false;

    [Header("Projected position for pathfinding")]
    public Vector3 projectedPosition = Vector3.zero;
    public float height;
    private RaycastHit projectionHit;

    private Rigidbody body;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction flyAction;
    private InputAction switchMovementAction;
    private InputAction pauseAction;
    private InputAction jumpAction;
    private InputAction diveAction;

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
        pauseAction = actions["Player/Pause"];
        jumpAction = actions["Player/Jump"];
        diveAction = actions["Player/Dive"];

        fireAction = actions["Player/Fire"];
        secondaryFireAction = actions["Player/SecondaryFire"];
        reloadAction = actions["Player/Reload"];

        slot1Action = actions["Player/Weapon1"];
        slot2Action = actions["Player/Weapon2"];
        slot3Action = actions["Player/Weapon3"];
        slot4Action = actions["Player/Weapon4"];

        if (healthSystem == null)
            healthSystem = GetComponent<HealthSystem>();
    }

    void OnEnable()
    {
        moveAction.Enable();
        lookAction.Enable();
        flyAction.Enable();
        switchMovementAction.Enable();
        pauseAction.Enable();
        jumpAction.Enable();
        diveAction.Enable();

        fireAction.Enable();
        secondaryFireAction.Enable();
        reloadAction.Enable();

        slot1Action.Enable();
        slot2Action.Enable();
        slot3Action.Enable();
        slot4Action.Enable();

        // Movement
        jumpAction.started += Jump;
        jumpAction.performed += SwitchMovement;
        flyAction.started += AltSwitch;
        diveAction.started += SwitchToGrounded;

        lookAction.performed += Look;
        pauseAction.performed += OnPause;

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
        flyAction.started -= AltSwitch;
        lookAction.performed -= Look;
        pauseAction.performed -= OnPause;

        fireAction.performed -= Fire;
        secondaryFireAction.performed -= SecondaryFire;
        reloadAction.performed -= Reload;

        moveAction.Disable();
        lookAction.Disable();
        flyAction.Disable();
        switchMovementAction.Disable();
        pauseAction.Disable();
        jumpAction.Disable();
        diveAction.Disable();

        fireAction.Disable();
        secondaryFireAction.Disable();
        reloadAction.Disable();

        slot1Action.Disable();
        slot2Action.Disable();
        slot3Action.Disable();
        slot4Action.Disable();
    }

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        currentSpeed = groundSpeed;

        if (UIManager.Instance != null)
            UIManager.Instance.RegisterPlayer(this);

        if (healthSystem != null)
        {
            healthSystem.DamageTaken += HandleDamageTaken;
            healthSystem.Died += HandleDeath;
        }
        else
        {
            Debug.LogWarning(
                $"[PlayerInput] HealthSystem not found on {gameObject.name}.",
                this
            );
        }
    }

    private void OnDestroy()
    {
        if (healthSystem != null)
        {
            healthSystem.DamageTaken -= HandleDamageTaken;
            healthSystem.Died -= HandleDeath;
        }
    }

    void FixedUpdate()
    {
        if (isDead) return;

        if (Physics.Raycast(transform.position, Vector3.down, 1.1f))
        {
            if (!grounded && !flying)
                currentSpeed = groundSpeed;

            grounded = true;
            diving = false;

            if (flying)
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

        SideMove();

        HandleFootsteps();
        HandleFlapSounds();

        if (flying)
            VerticalMove();
        else
            verticalMovement = body.linearVelocity.y;

        body.linearVelocity =
            Quaternion.LookRotation(transform.forward, Vector3.up) *
            new Vector3(sideMovement.x, verticalMovement, sideMovement.y);

        if (
            Physics.Raycast(
                transform.position,
                Vector3.down,
                out projectionHit,
                100f,
                LayerMask.GetMask("Default", "Terrain")
            )
        )
        {
            projectedPosition = projectionHit.point;
            height = projectionHit.distance;
        }
    }

    void AltSwitch(InputAction.CallbackContext ctx)
    {
        if (!flying && !diving)
        {
            flying = true;
            currentSpeed = flightSpeed;
            body.useGravity = false;
        }
    }

    void Jump(InputAction.CallbackContext ctx)
    {
        if (grounded)
        {
            body.linearVelocity = new Vector3(
                sideMovement.x,
                body.linearVelocity.y + jumpStrength,
                sideMovement.y
            );

            grounded = false;
        }
    }

    void SwitchMovement(InputAction.CallbackContext ctx)
    {
        if (ctx.interaction is HoldInteraction)
        {
            if (!grounded && !diving)
            {
                flying = true;
                currentSpeed = flightSpeed;
                body.useGravity = false;
            }
        }
    }

    void SwitchToGrounded(InputAction.CallbackContext ctx)
    {
        if (flying)
        {
            body.linearVelocity = new Vector3(
                sideMovement.x,
                body.linearVelocity.y - diveStrength,
                sideMovement.y
            );

            flying = false;
            body.useGravity = true;
            diving = true;

            PlayDiveSound();
        }
    }

    void Look(InputAction.CallbackContext ctx)
    {
        Vector2 look = ctx.ReadValue<Vector2>();

        cameraPosition.Rotate(-look.y * camSensitivity, 0, 0);

        if (Vector3.Angle(transform.forward, cameraPosition.forward) > 90)
        {
            if (
                Vector3.Angle(Vector3.up, cameraPosition.forward)
                > Vector3.Angle(Vector3.down, cameraPosition.forward)
            )
            {
                cameraPosition.localRotation = Quaternion.Euler(90, 0, 0);
            }
            else
            {
                cameraPosition.localRotation = Quaternion.Euler(-90, 0, 0);
            }
        }

        transform.Rotate(0, look.x * camSensitivity, 0);
    }

    void SideMove()
    {
        sideMovement = moveAction.ReadValue<Vector2>() * currentSpeed;
    }

    void VerticalMove()
    {
        verticalMovement = flyAction.ReadValue<float>() * (currentSpeed / 3f) * 2f;
    }

    void HandleFootsteps()
    {
        if (isDead) return;

        // Solo a terra
        if (!grounded) return;

        // Niente passi mentre vola
        if (flying) return;

        Vector2 movement = moveAction.ReadValue<Vector2>();

        // Player fermo
        if (movement.magnitude < 0.1f)
        {
            footstepTimer = 0f;
            return;
        }

        footstepTimer -= Time.fixedDeltaTime;

        if (footstepTimer <= 0f)
        {
            PlayFootstep();
            footstepTimer = footstepInterval;
        }
    }

    void PlayFootstep()
    {
        if (footstepSource == null) return;

        if (footstepClips == null || footstepClips.Length == 0) return;

        int index = Random.Range(0, footstepClips.Length);

        footstepSource.PlayOneShot(footstepClips[index]);
    }

    void HandleFlapSounds()
    {
        if (isDead) return;

        // Suona solo mentre si sta volando
        if (!flying)
        {
            flapTimer = 0f;
            return;
        }

        flapTimer -= Time.fixedDeltaTime;

        if (flapTimer <= 0f)
        {
            PlayFlap();
            flapTimer = flapInterval;
        }
    }

    void PlayFlap()
    {
        if (flightSource == null) return;

        if (flapClips == null || flapClips.Length == 0) return;

        int index = Random.Range(0, flapClips.Length);

        flightSource.PlayOneShot(flapClips[index]);
    }

    void PlayDiveSound()
    {
        if (diveSource == null || diveClip == null) return;

        diveSource.PlayOneShot(diveClip);
    }

    void Fire(InputAction.CallbackContext ctx)
    {
        if (isDead) return;

        BaseWeapon weapon = weaponManager.GetCurrentWeapon();

        if (weapon != null && weapon.CanFire())
            weapon.PrimaryFire();
    }

    void SecondaryFire(InputAction.CallbackContext ctx)
    {
        if (isDead) return;

        BaseWeapon weapon = weaponManager.GetCurrentWeapon();

        if (weapon != null && weapon.CanSecondaryFire())
            weapon.SecondaryFire();
    }

    void Reload(InputAction.CallbackContext ctx)
    {
        if (isDead) return;

        BaseWeapon weapon = weaponManager.GetCurrentWeapon();

        if (weapon != null && weapon.CanReload())
            weapon.Reload();
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPause(context);
        }
    }

    private void HandleDamageTaken(float damage)
    {
        if (isDead) return;

        if (healthSystem.GetHealth() <= 0f)
        {
            HandleDeath();
            return;
        }
    }

    private void HandleDeath()
    {
        if (isDead) return;

        isDead = true;

        // Usa DisableGameplayInput invece di OnDisable() —
        // OnDisable rimuove i listener delle action, che non vengono
        // riagganci da EnableGameplayInput() al Revive, causando il freeze al restart.
        DisableGameplayInput();

        if (weaponManager != null)
            weaponManager.enabled = false;

        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.isKinematic = true;
        }
    }

    public bool IsDead() => isDead;

    /// <summary>Disabilita solo le action di gameplay, lasciando attiva la pauseAction.</summary>
    public void DisableGameplayInput()
    {
        moveAction.Disable();
        lookAction.Disable();
        flyAction.Disable();
        switchMovementAction.Disable();
        jumpAction.Disable();
        diveAction.Disable();
        fireAction.Disable();
        secondaryFireAction.Disable();
        reloadAction.Disable();
        slot1Action.Disable();
        slot2Action.Disable();
        slot3Action.Disable();
        slot4Action.Disable();
        // pauseAction rimane SEMPRE abilitata
    }

    /// <summary>Riabilita tutte le action di gameplay.</summary>
    public void EnableGameplayInput()
    {
        moveAction.Enable();
        lookAction.Enable();
        flyAction.Enable();
        switchMovementAction.Enable();
        jumpAction.Enable();
        diveAction.Enable();
        fireAction.Enable();
        secondaryFireAction.Enable();
        reloadAction.Enable();
        slot1Action.Enable();
        slot2Action.Enable();
        slot3Action.Enable();
        slot4Action.Enable();
    }

    /// <summary>Resetta lo stato di morte. Chiamato da SceneResetManager dopo il reset.</summary>
    public void Revive()
    {
        if (!isDead) return;

        isDead = false;

        if (body != null)
        {
            body.isKinematic = false;
            body.linearVelocity = Vector3.zero;
        }

        if (weaponManager != null)
            weaponManager.enabled = true;

        EnableGameplayInput();
    }

    public HealthSystem GetHealthSystem() => healthSystem;
}