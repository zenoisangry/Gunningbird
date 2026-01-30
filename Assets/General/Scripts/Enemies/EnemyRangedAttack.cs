using UnityEngine;

public class EnemyRangedAttack : MonoBehaviour
{
    [Header("Weapon Setup")]
    [SerializeField] private WeaponData weaponData;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Transform weaponHolder;

    [Header("Aiming Settings")]
    [SerializeField] private float aimSpeed = 5f;
    [SerializeField] private float aimOffset = 1f;
    [SerializeField] private bool predictTargetMovement = true;
    [SerializeField] private float predictionMultiplier = 0.5f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    private RangedWeapon weapon;
    private EnemyWeaponOwner weaponOwner;

    private Transform target;
    private Vector3 lastTargetPosition;
    private Vector3 targetVelocity;

    private bool isInitialized = false;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        weaponOwner = gameObject.AddComponent<EnemyWeaponOwner>();
        weaponOwner.Initialize(firePoint, transform, audioSource);
    }

    private void Start()
    {
        if (weaponData == null)
        {
            Debug.LogError("[EnemyRangedAttack] WeaponData not assigned!", this);
            enabled = false;
            return;
        }

        if (firePoint == null)
        {
            Debug.LogWarning("[EnemyRangedAttack] FirePoint not assigned, using transform position", this);
            firePoint = transform;
        }

        SetupWeapon();
    }

    private void SetupWeapon()
    {
        if (weaponData.weaponPrefab == null)
        {
            Debug.LogError($"[EnemyRangedAttack] Weapon prefab missing in {weaponData.weaponName}!", this);
            enabled = false;
            return;
        }

        Transform parent = weaponHolder != null ? weaponHolder : transform;
        GameObject weaponGO = Instantiate(weaponData.weaponPrefab, parent);
        weaponGO.transform.localPosition = Vector3.zero;
        weaponGO.transform.localRotation = Quaternion.identity;

        weapon = weaponGO.GetComponent<RangedWeapon>();
        if (weapon == null)
        {
            Debug.LogError($"[EnemyRangedAttack] RangedWeapon component not found on {weaponData.weaponName}!", this);
            Destroy(weaponGO);
            enabled = false;
            return;
        }

        weapon.Initialize(weaponData, weaponOwner);
        isInitialized = true;

        Debug.Log($"[EnemyRangedAttack] Weapon {weaponData.weaponName} initialized successfully");
    }

    private void Update()
    {
        if (!isInitialized || weapon == null) return;

        if (target != null)
        {
            if (predictTargetMovement)
            {
                UpdateTargetVelocity();
            }

            AimAtTarget();
        }
    }
    public void SetTarget(Transform newTarget)
    {
        if (newTarget == target) return;

        target = newTarget;

        if (target != null)
        {
            lastTargetPosition = target.position;
            targetVelocity = Vector3.zero;
            Debug.Log($"[EnemyRangedAttack] Target set to: {target.name}");
        }
        else
        {
            Debug.Log("[EnemyRangedAttack] Target cleared");
        }
    }

    public Transform GetTarget() => target;

    public void ClearTarget()
    {
        target = null;
        targetVelocity = Vector3.zero;
    }

    private void UpdateTargetVelocity()
    {
        if (target == null) return;

        Vector3 currentPosition = target.position;
        targetVelocity = (currentPosition - lastTargetPosition) / Time.deltaTime;
        lastTargetPosition = currentPosition;
    }

    private void AimAtTarget()
    {
        if (target == null) return;

        Vector3 aimPosition = target.position;

        if (aimOffset != 0f)
        {
            Collider targetCollider = target.GetComponent<Collider>();
            float targetHeight = targetCollider != null ? targetCollider.bounds.size.y : 2f;
            aimPosition.y += targetHeight * aimOffset;
        }

        if (predictTargetMovement && targetVelocity.magnitude > 0.1f)
        {
            float distance = Vector3.Distance(firePoint.position, aimPosition);
            float timeToHit = distance / weaponData.bulletSpeed;
            aimPosition += targetVelocity * timeToHit * predictionMultiplier;
        }

        Vector3 directionToTarget = (aimPosition - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, aimSpeed * Time.deltaTime);
    }

    public bool IsAimedAtTarget(float maxAngle = 5f)
    {
        if (target == null) return false;

        Vector3 directionToTarget = (target.position - transform.position).normalized;
        float angleToTarget = Vector3.Angle(transform.forward, new Vector3(directionToTarget.x, 0, directionToTarget.z));

        return angleToTarget <= maxAngle;
    }
    public bool CanFire()
    {
        if (!isInitialized || weapon == null) return false;
        if (target == null) return false;

        return weapon.CanFire();
    }

    public void Fire()
    {
        if (!CanFire())
        {
            Debug.LogWarning("[EnemyRangedAttack] Cannot fire!");
            return;
        }

        weapon.PrimaryFire();
    }

    public bool IsReloading()
    {
        if (weapon == null) return false;
        return weapon.IsReloading();
    }
    public void Reload()
    {
        if (weapon == null) return;

        if (weapon.CanReload())
        {
            weapon.Reload();
            Debug.Log("[EnemyRangedAttack] Started reloading");
        }
    }
    public bool CanReload()
    {
        if (weapon == null) return false;
        return weapon.CanReload();
    }

    public bool IsWeaponEmpty()
    {
        if (weapon == null) return true;
        return weapon.GetCurrentAmmo() <= 0;
    }

    public int GetCurrentAmmo()
    {
        if (weapon == null) return 0;
        return weapon.GetCurrentAmmo();
    }

    public int GetReserveAmmo()
    {
        if (weapon == null) return 0;
        return weapon.GetReserveAmmo();
    }

    public bool HasLineOfSight()
    {
        if (target == null || firePoint == null) return false;

        Vector3 directionToTarget = target.position - firePoint.position;
        float distance = directionToTarget.magnitude;

        if (Physics.Raycast(firePoint.position, directionToTarget.normalized, out RaycastHit hit, distance))
        {
            return hit.transform == target || hit.transform.IsChildOf(target);
        }

        return true;
    }

    public bool HasLineOfSightTo(Vector3 position)
    {
        if (firePoint == null) return false;

        Vector3 directionToPosition = position - firePoint.position;
        float distance = directionToPosition.magnitude;

        return !Physics.Raycast(firePoint.position, directionToPosition.normalized, distance, LayerMask.GetMask("Default"));
    }

    public WeaponData GetWeaponData() => weaponData;
    public RangedWeapon GetWeapon() => weapon;


    private void OnDrawGizmosSelected()
    {
        if (target != null && firePoint != null)
        {
            Gizmos.color = HasLineOfSight() ? Color.green : Color.red;
            Gizmos.DrawLine(firePoint.position, target.position);

            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(firePoint.position, transform.forward * 5f);
        }

        if (firePoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(firePoint.position, 0.2f);
        }
    }
}

public class EnemyWeaponOwner : MonoBehaviour, IWeaponOwner
{
    private Transform fireTransform;
    private Transform cameraTransform;
    private AudioSource audioSource;

    public void Initialize(Transform fire, Transform camera, AudioSource audio)
    {
        fireTransform = fire;
        cameraTransform = camera;
        audioSource = audio;
    }

    public Transform GetFireTransform() => fireTransform;
    public Transform GetCameraTransform() => cameraTransform;
    public Animator GetAnimator() => GetComponent<Animator>();

    public void AddRecoil(Vector2 recoil){}

    public void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}