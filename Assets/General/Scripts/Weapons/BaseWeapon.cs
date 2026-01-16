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

    [Header("Audio")]
    [SerializeField] protected AudioSource audioSource;

    public virtual void Initialize(WeaponData data, IWeaponOwner weaponOwner)
    {
        weaponData = data;
        owner = weaponOwner;
        animator = weaponOwner.GetAnimator();

        currentAmmo = weaponData.magazineSize;
        currentReserveAmmo = weaponData.totalAmmo;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public virtual void PrimaryFire()
    {
        if (!CanFire() || isReloading) return;

        FireWeapon();
        lastFireTime = Time.time;
        currentAmmo--;

        UpdateSpread();
        ApplyRecoil();
        PlayFireEffects();

        if (currentAmmo <= 0 && !weaponData.hasInfiniteAmmo)
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

        if (currentAmmo <= 0 && !weaponData.hasInfiniteAmmo)
        {
            if (CanReload())
                Reload();
        }
    }

    public virtual void Reload()
    {
        if (!CanReload() || isReloading) return;

        StartCoroutine(ReloadRoutine());
    }

    protected virtual System.Collections.IEnumerator ReloadRoutine()
    {
        isReloading = true;
        animator.SetTrigger(weaponData.reloadAnimationTrigger);

        if (weaponData.reloadSound != null && audioSource != null)
            audioSource.PlayOneShot(weaponData.reloadSound);

        yield return new WaitForSeconds(weaponData.reloadTime);

        int ammoToReload = Mathf.Min(weaponData.magazineSize - currentAmmo, currentReserveAmmo);
        currentAmmo += ammoToReload;
        currentReserveAmmo -= ammoToReload;

        isReloading = false;
    }

    public virtual void Draw()
    {
        animator.SetTrigger(weaponData.drawAnimationTrigger);
    }

    public virtual void Holster()
    {
        isFiring = false;
        isSecondaryFiring = false;
    }

    public virtual bool CanFire()
    {
        if (isReloading) return false;
        if (weaponData.hasInfiniteAmmo) return true;
        return currentAmmo > 0 && Time.time >= lastFireTime + (60f / weaponData.fireRate);
    }

    public virtual bool CanSecondaryFire()
    {
        if (isReloading) return false;
        if (weaponData.secondaryFireType == SecondaryFireType.None) return false;
        if (currentAmmo < weaponData.secondaryFireAmmoCost && !weaponData.hasInfiniteAmmo) return false;
        return Time.time >= lastSecondaryFireTime + weaponData.secondaryFireCooldown;
    }

    public virtual bool CanReload()
    {
        if (weaponData.hasInfiniteAmmo) return false;
        return currentAmmo < weaponData.magazineSize && currentReserveAmmo > 0;
    }

    public virtual bool IsReloading() => isReloading;
    public virtual WeaponData GetWeaponData() => weaponData;

    public abstract void FireWeapon();
    protected virtual void FireSecondaryWeapon() { }

    protected virtual void UpdateSpread()
    {
        currentSpread = Mathf.Min(currentSpread + weaponData.spreadIncreasePerShot, weaponData.maxSpread);
    }

    protected virtual void ApplyRecoil()
    {
        currentRecoil += weaponData.recoilPattern;
        owner.AddRecoil(weaponData.recoilPattern);
    }

    protected virtual void PlayFireEffects()
    {
        if (weaponData.shootSound != null && audioSource != null)
            audioSource.PlayOneShot(weaponData.shootSound);

        if (weaponData.muzzleFlash != null)
        {
            weaponData.muzzleFlash.Play();
        }

        animator.SetTrigger(weaponData.shootAnimationTrigger);
    }

    protected virtual void PlaySecondaryFireEffects(){}

    protected virtual void Update()
    {
        if (currentSpread > 0)
        {
            currentSpread = Mathf.Max(0, currentSpread - weaponData.spreadDecreaseSpeed * Time.deltaTime);
        }
    }

    protected virtual Vector3 CalculateSpreadDirection(Vector3 baseDirection)
    {
        float spreadAngle = currentSpread;
        Vector3 spread = Vector3.zero;

        spread.x = Random.Range(-spreadAngle, spreadAngle);
        spread.y = Random.Range(-spreadAngle, spreadAngle);

        return baseDirection + spread;
    }

    public virtual void AddAmmo(int amount)
    {
        currentReserveAmmo += amount;
    }

    public virtual int GetCurrentAmmo() => currentAmmo;
    public virtual int GetReserveAmmo() => currentReserveAmmo;
}