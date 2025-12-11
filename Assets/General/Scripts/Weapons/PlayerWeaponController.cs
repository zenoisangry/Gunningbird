using UnityEngine;
using UnityEngine.Events;
using System.Collections;

[RequireComponent(typeof(WeaponManager))]
[RequireComponent(typeof(WeaponUI))]
public class WeaponController : MonoBehaviour
{
    [Header("Weapon References")]
    [SerializeField] protected Camera weaponCamera;
    [SerializeField] protected Transform weaponHolder;
    [SerializeField] protected Animator weaponAnimator;
    [SerializeField] protected AudioSource weaponAudioSource;

    [Header("Camera & Aiming")]
    [SerializeField] protected Vector3 normalCameraPosition;
    [SerializeField] protected Vector3 aimingCameraPosition;
    [SerializeField] protected float aimSpeed = 10f;
    [SerializeField] protected float fieldOfViewNormal = 60f;
    [SerializeField] protected float fieldOfViewAiming = 40f;
    [SerializeField] protected bool enableCameraShake = true;

    [Header("Recoil")]
    [SerializeField] protected Vector2 recoilReduction = new Vector2(0.5f, 0.3f);
    [SerializeField] protected float recoilRecoverySpeed = 8f;

    protected WeaponManager weaponManager;
    protected WeaponUI weaponUI;

    protected Vector2 currentRecoil;
    protected Vector2 targetRecoil;
    protected bool isAiming;
    protected bool isSprinting;

    protected virtual void Awake()
    {
        InitializeComponents();
    }

    protected virtual void Start()
    {
        SetupEventListeners();
        InitializeCamera();
    }

    protected virtual void InitializeComponents()
    {
        weaponManager = GetComponent<WeaponManager>();
        weaponUI = GetComponent<WeaponUI>();

        if (weaponCamera == null)
            weaponCamera = GetComponentInChildren<Camera>();

        if (weaponAnimator == null)
            weaponAnimator = GetComponent<Animator>();

        if (weaponAudioSource == null)
            weaponAudioSource = GetComponent<AudioSource>();

        if (weaponHolder == null)
            weaponHolder = transform.Find("WeaponHolder");
    }

    protected virtual void SetupEventListeners()
    {
        if (weaponManager != null)
        {
            weaponManager.OnWeaponChanged += OnWeaponChanged;
        }
    }

    protected virtual void InitializeCamera()
    {
        if (weaponCamera != null)
        {
            weaponCamera.fieldOfView = fieldOfViewNormal;
            normalCameraPosition = weaponCamera.transform.localPosition;
        }
    }

    protected virtual void Update()
    {
        HandleCamera();
        HandleRecoil();
    }

    #region Public Weapon Methods (Call these from your input system)
    public virtual void StartAiming()
    {
        if (!isSprinting)
        {
            isAiming = true;

            if (weaponAnimator != null)
            {
                weaponAnimator.SetLayerWeight(1, 1f); // Aiming layer
                weaponAnimator.SetBool("IsAiming", true);
            }
        }
    }

    public virtual void StopAiming()
    {
        isAiming = false;

        if (weaponAnimator != null)
        {
            weaponAnimator.SetLayerWeight(1, 0f); // Aiming layer
            weaponAnimator.SetBool("IsAiming", false);
        }
    }

    public virtual void SetSprinting(bool sprinting)
    {
        isSprinting = sprinting;

        if (isSprinting && isAiming)
        {
            StopAiming();
        }
    }

    public virtual void AddRecoil(Vector2 recoil)
    {
        targetRecoil += recoil;
        currentRecoil = targetRecoil;

        // Reduce recoil while aiming
        if (isAiming)
        {
            currentRecoil *= (1f - recoilReduction.x);
            targetRecoil *= (1f - recoilReduction.y);
        }
    }

    public virtual void OnWeaponDamaged()
    {
        // Camera shake effect when player takes damage
        if (enableCameraShake && weaponCamera != null)
        {
            StartCoroutine(CameraShakeRoutine(0.2f, 0.5f));
        }
    }

    public virtual void OnPlayerDeath()
    {
        // Holster current weapon
        if (weaponManager != null)
        {
            IWeapon currentWeapon = weaponManager.GetCurrentWeapon();
            if (currentWeapon != null)
            {
                currentWeapon.Holster();
            }
        }

        // Play death animation
        if (weaponAnimator != null)
        {
            weaponAnimator.SetTrigger("Death");
        }
    }
    #endregion

    protected virtual void HandleCamera()
    {
        if (weaponCamera == null) return;

        // Smooth camera position for aiming
        Vector3 targetCameraPosition = isAiming ? aimingCameraPosition : normalCameraPosition;
        weaponCamera.transform.localPosition = Vector3.Lerp(
            weaponCamera.transform.localPosition,
            targetCameraPosition,
            Time.deltaTime * aimSpeed
        );

        // Smooth field of view transition
        float targetFOV = isAiming ? fieldOfViewAiming : fieldOfViewNormal;
        weaponCamera.fieldOfView = Mathf.Lerp(
            weaponCamera.fieldOfView,
            targetFOV,
            Time.deltaTime * aimSpeed
        );

        // Apply recoil to camera rotation
        if (currentRecoil != Vector2.zero)
        {
            Vector2 recoilOffset = Vector2.Lerp(currentRecoil, Vector2.zero, Time.deltaTime * recoilRecoverySpeed);
            currentRecoil = recoilOffset;

            // Apply recoil to camera rotation (local rotation)
            weaponCamera.transform.localRotation = Quaternion.Euler(
                -recoilOffset.y,
                recoilOffset.x,
                0
            );
        }
    }

    protected virtual void HandleRecoil()
    {
        // Smoothly reduce target recoil
        targetRecoil = Vector2.Lerp(targetRecoil, Vector2.zero, Time.deltaTime * recoilRecoverySpeed);

        // Reduce recoil when not firing
        currentRecoil = Vector2.Lerp(currentRecoil, targetRecoil, Time.deltaTime * recoilRecoverySpeed * 2f);
    }

    protected virtual void OnWeaponChanged(IWeapon newWeapon)
    {
        // Update camera settings based on weapon type
        if (newWeapon != null)
        {
            WeaponData weaponData = newWeapon.GetWeaponData();

            switch (weaponData.weaponType)
            {
                case WeaponType.Sniper:
                    fieldOfViewAiming = 25f;
                    break;
                case WeaponType.Shotgun:
                    fieldOfViewAiming = 50f;
                    break;
                default:
                    fieldOfViewAiming = 40f;
                    break;
            }
        }
    }

    protected virtual IEnumerator CameraShakeRoutine(float duration, float magnitude)
    {
        Vector3 originalPosition = weaponCamera.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            weaponCamera.transform.localPosition = originalPosition + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        weaponCamera.transform.localPosition = originalPosition;
    }

    protected virtual void OnDestroy()
    {
        // Unsubscribe from events
        if (weaponManager != null)
        {
            weaponManager.OnWeaponChanged -= OnWeaponChanged;
        }
    }

    // Getters for weapon owner interface
    public virtual Transform GetFireTransform() => weaponHolder;
    public virtual Transform GetCameraTransform() => weaponCamera.transform;
    public virtual Animator GetAnimator() => weaponAnimator;
    public virtual void PlaySound(AudioClip clip)
    {
        if (weaponAudioSource != null && clip != null)
            weaponAudioSource.PlayOneShot(clip);
    }

    public virtual bool IsAiming() => isAiming;
    public virtual bool IsSprinting() => isSprinting;
}