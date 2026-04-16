using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseWeapon : MonoBehaviour, IWeapon
{
    [Header("Components")]
    [SerializeField] protected WeaponData weaponData;
    protected IWeaponOwner owner;
    protected Animator animator;

    [Header("State")]
    protected int currentAmmo;
    protected int currentReserveAmmo;
    protected bool isReloading;
    protected float lastFireTime;
    protected float lastSecondaryFireTime;
    protected float currentSpread;
    protected Vector2 currentRecoil;
    protected bool isFiring;
    protected bool isSecondaryFiring;
    protected Coroutine reloadCoroutine;
    protected float fireRateCooldown;
    protected List<Quaternion> spreadProjectiles = new List<Quaternion>();

    [Header("Audio")]
    [SerializeField] protected AudioSource audioSource;

    void Start()
    {
        foreach (Vector2 angle in weaponData.projectileAngles)
        {
            spreadProjectiles.Add(Quaternion.Euler(new Vector3(angle.x, angle.y, 0)));
        }
    }

    public virtual void Initialize(WeaponData data, IWeaponOwner weaponOwner)
    {
        if (data == null)
        {
            Debug.LogError("[BaseWeapon] WeaponData is null!");
            return;
        }

        if (weaponOwner == null)
        {
            Debug.LogError("[BaseWeapon] IWeaponOwner is null!");
            return;
        }

        weaponData = data;
        owner = weaponOwner;
        animator = weaponOwner.GetAnimator();

        currentAmmo = weaponData.magazineSize;
        currentReserveAmmo = weaponData.totalAmmo;
        fireRateCooldown = 60f / weaponData.fireRate;
        currentSpread = weaponData.maxSpread;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public virtual void PrimaryFire()
    {
        if (!CanFire() || isReloading) return;

        FireWeapon();
        lastFireTime = Time.time;
        currentAmmo--;

        ApplyRecoil();
        PlayFireEffects();

        if (currentAmmo <= 0)
        {
            if (CanReload())
                Reload();
        }
    }

    public virtual void SecondaryFire()
    {
        if (!CanSecondaryFire()) return;

        FireSecondaryWeapon();
        lastSecondaryFireTime = Time.time;
        currentAmmo -= weaponData.secondaryFireAmmoCost;

        PlaySecondaryFireEffects();

        if (currentAmmo <= 0)
        {
            if (CanReload())
                Reload();
        }
    }

    public virtual void Reload()
    {
        if (!CanReload() || isReloading) return;

        if (reloadCoroutine != null)
            StopCoroutine(reloadCoroutine);

        reloadCoroutine = StartCoroutine(ReloadRoutine());
    }

    protected virtual System.Collections.IEnumerator ReloadRoutine()
    {
        isReloading = true;

        if (animator != null && !string.IsNullOrEmpty(weaponData.reloadAnimationTrigger))
            animator.SetTrigger(weaponData.reloadAnimationTrigger);

        if (weaponData.reloadSound != null && audioSource != null)
            audioSource.PlayOneShot(weaponData.reloadSound);

        yield return new WaitForSeconds(weaponData.reloadTime);

        int ammoToReload = Mathf.Min(weaponData.magazineSize - currentAmmo, currentReserveAmmo);
        currentAmmo += ammoToReload;
        if (!weaponData.hasInfiniteAmmo)
            currentReserveAmmo -= ammoToReload;

        isReloading = false;
        reloadCoroutine = null;
    }

    public virtual void Draw()
    {
        if (animator != null && !string.IsNullOrEmpty(weaponData.drawAnimationTrigger))
            animator.SetTrigger(weaponData.drawAnimationTrigger);
        if (currentAmmo == 0)
        {
            reloadCoroutine = StartCoroutine(ReloadRoutine());
        }
    }

    public virtual void Holster()
    {
        isFiring = false;
        isSecondaryFiring = false;
        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            reloadCoroutine = null;
            isReloading = false;
        }
    }

    public virtual bool CanFire()
    {
        if (weaponData == null || isReloading) return false;
        return currentAmmo > 0 && Time.time >= lastFireTime + fireRateCooldown;
    }

    public virtual bool CanSecondaryFire()
    {
        if (isReloading) return false;
        if (weaponData.secondaryFireType == SecondaryFireType.None) return false;
        if (currentAmmo < weaponData.secondaryFireAmmoCost) return false;
        return Time.time >= lastSecondaryFireTime + weaponData.secondaryFireCooldown;
    }

    public virtual bool CanReload()
    {
        if (weaponData == null) return false;
        return currentAmmo < weaponData.magazineSize && currentReserveAmmo > 0;
    }

    public virtual bool IsReloading() => isReloading;
    public virtual WeaponData GetWeaponData() => weaponData;

    public abstract void FireWeapon();
    protected virtual void FireSecondaryWeapon() { }

    protected virtual void ApplyRecoil()
    {
        if (weaponData == null) return;

        currentRecoil += weaponData.recoilPattern;
        if (owner != null)
            owner.AddRecoil(weaponData.recoilPattern);
    }

    protected virtual void PlayFireEffects()
    {
        if (weaponData == null) return;

        if (weaponData.shootSound != null && audioSource != null)
        {
            audioSource.pitch = 1*(1 + Random.Range(-weaponData.pitchRandomInterval, weaponData.pitchRandomInterval));
            audioSource.PlayOneShot(weaponData.shootSound);
        }
            

        if (weaponData.muzzleFlash != null)
        {
            weaponData.muzzleFlash.Play();
        }

        if (animator != null && !string.IsNullOrEmpty(weaponData.shootAnimationTrigger))
            animator.SetTrigger(weaponData.shootAnimationTrigger);
    }

    protected virtual void PlaySecondaryFireEffects() { }

    protected virtual void Update()
    {
        if (weaponData == null) return;
    }

    protected virtual Vector3 CalculateSpreadDirection(Vector3 baseDirection)
    {
        if (currentSpread <= 0f)
            return baseDirection.normalized;

        // Convert spread angle from degrees to radians and apply proper spherical distribution
        float spreadAngleRad = currentSpread * Mathf.Deg2Rad;
        float randomAngle = Random.Range(0f, 2f * Mathf.PI);
        float randomSpread = Random.Range(0f, spreadAngleRad);

        // Calculate perpendicular vectors for spread
        Vector3 right = Vector3.Cross(baseDirection, Vector3.up).normalized;
        if (right == Vector3.zero)
            right = Vector3.Cross(baseDirection, Vector3.forward).normalized;

        Vector3 up = Vector3.Cross(right, baseDirection).normalized;

        // Apply spread using spherical coordinates
        Vector3 spreadDirection = baseDirection.normalized;
        spreadDirection += right * (Mathf.Sin(randomAngle) * Mathf.Sin(randomSpread));
        spreadDirection += up * (Mathf.Cos(randomAngle) * Mathf.Sin(randomSpread));
        spreadDirection = Vector3.Slerp(baseDirection.normalized, spreadDirection, Mathf.Sin(randomSpread));

        return spreadDirection.normalized;
    }

    public virtual void AddAmmo(int amount)
    {
        if (amount > 0)
            currentReserveAmmo += amount;
    }

    public virtual int GetCurrentAmmo() => currentAmmo;
    public virtual int GetReserveAmmo() => currentReserveAmmo;
}