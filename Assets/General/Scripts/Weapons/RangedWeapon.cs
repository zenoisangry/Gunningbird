using UnityEngine;
using System.Collections;

public class RangedWeapon : BaseWeapon
{
    [Header("Ranged Weapon Settings")]
    [SerializeField] protected Transform firePoint;
    [SerializeField] protected LayerMask hitLayers;
    [SerializeField] protected float maxDistance = 100f;
    [SerializeField] protected float bulletForce = 1000f;

    protected override void FireWeapon()
    {
        switch (weaponData.weaponType)
        {
            case WeaponType.Shotgun:
                FireShotgun();
                break;
            case WeaponType.Sniper:
                FireSniper();
                break;
            default:
                FireSingleBullet();
                break;
        }
    }

    protected virtual void FireSingleBullet()
    {
        Transform cameraTransform = owner.GetCameraTransform();
        Vector3 fireDirection = CalculateSpreadDirection(cameraTransform.forward);

        if (weaponData.bulletPrefab != null)
        {
            GameObject projectileGO = Instantiate(weaponData.bulletPrefab, firePoint.position, Quaternion.LookRotation(fireDirection));
            Projectile projectile = projectileGO.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.Initialize(weaponData.damage, owner, fireDirection);
            }
        }

        if (weaponData.bulletTrailPrefab != null)
        {
            CreateBulletTrail(firePoint.position, firePoint.position + fireDirection * maxDistance);
        }

        if (Physics.Raycast(cameraTransform.position, fireDirection, out RaycastHit hit, maxDistance, hitLayers))
        {
            ProcessHit(hit, fireDirection);
        }
    }

    protected virtual void FireShotgun()
    {
        int pelletCount = 8;
        Transform cameraTransform = owner.GetCameraTransform();

        for (int i = 0; i < pelletCount; i++)
        {
            Vector3 fireDirection = CalculateSpreadDirection(cameraTransform.forward);

            if (Physics.Raycast(cameraTransform.position, fireDirection, out RaycastHit hit, maxDistance, hitLayers))
            {
                ProcessHit(hit, fireDirection, weaponData.damage / pelletCount);
            }

            if (weaponData.bulletPrefab != null)
            {
                GameObject projGO = Instantiate(weaponData.bulletPrefab, firePoint.position, Quaternion.LookRotation(fireDirection));
                Projectile proj = projGO.GetComponent<Projectile>();
                if (proj != null)
                    proj.Initialize(weaponData.damage / pelletCount, owner, fireDirection);
            }
        }
    }

    protected virtual void FireSniper()
    {
        Transform cameraTransform = owner.GetCameraTransform();
        Vector3 fireDirection = cameraTransform.forward;

        if (weaponData.bulletPrefab != null)
        {
            GameObject projGO = Instantiate(weaponData.bulletPrefab, firePoint.position, Quaternion.LookRotation(fireDirection));
            Projectile proj = projGO.GetComponent<Projectile>();
            if (proj != null)
                proj.Initialize(weaponData.damage, owner, fireDirection);
        }

        if (Physics.Raycast(cameraTransform.position, fireDirection, out RaycastHit hit, maxDistance, hitLayers))
        {
            ProcessHit(hit, fireDirection, weaponData.damage, true);
        }
    }

    protected override void FireSecondaryWeapon()
    {
        switch (weaponData.secondaryFireType)
        {
            case SecondaryFireType.Burst:
                FireBurst();
                break;
            case SecondaryFireType.Zoom:
                break;
            case SecondaryFireType.Grenade:
                FireGrenade();
                break;
            case SecondaryFireType.Explosive:
                FireExplosiveRound();
                break;
            case SecondaryFireType.ArmorPiercing:
                FireArmorPiercingRound();
                break;
        }
    }

    protected virtual void FireBurst()
    {
        int burstCount = 3;
        float burstDelay = 60f / (weaponData.fireRate * 2f);

        for (int i = 0; i < burstCount; i++)
        {
            Invoke(nameof(FireSingleBullet), i * burstDelay);
        }
    }

    protected virtual void FireGrenade()
    {
        Transform cameraTransform = owner.GetCameraTransform();
        if (weaponData.bulletPrefab != null)
        {
            GameObject grenade = Instantiate(weaponData.bulletPrefab, firePoint.position, Quaternion.LookRotation(cameraTransform.forward));
            Projectile proj = grenade.GetComponent<Projectile>();
            if (proj != null)
                proj.Initialize(weaponData.damage, owner, cameraTransform.forward);

            Rigidbody rb = grenade.GetComponent<Rigidbody>();
            if (rb != null)
                rb.AddForce(cameraTransform.forward * bulletForce);
        }
    }

    protected virtual void FireExplosiveRound()
    {
        Transform cameraTransform = owner.GetCameraTransform();
        Vector3 fireDirection = CalculateSpreadDirection(cameraTransform.forward);

        if (Physics.Raycast(cameraTransform.position, fireDirection, out RaycastHit hit, maxDistance, hitLayers))
        {
            ProcessHit(hit, fireDirection, weaponData.secondaryFireDamage);
            CreateExplosion(hit.point);
        }
    }

    protected virtual void FireArmorPiercingRound()
    {
        Transform cameraTransform = owner.GetCameraTransform();
        Vector3 fireDirection = cameraTransform.forward;

        RaycastHit[] hits = Physics.RaycastAll(cameraTransform.position, fireDirection, maxDistance, hitLayers);

        foreach (var hit in hits)
        {
            ProcessHit(hit, fireDirection, weaponData.secondaryFireDamage * 0.7f);
        }
    }

    protected virtual void ProcessHit(RaycastHit hit, Vector3 fireDirection, float damageMultiplier = 1f, bool ignoreArmor = false)
    {
        IDamageable damageable = hit.collider.GetComponent<IDamageable>();
        if (damageable != null)
        {
            float finalDamage = weaponData.damage * damageMultiplier;
            if (hit.collider.CompareTag("Head"))
                finalDamage *= weaponData.headshotMultiplier;

            if (ignoreArmor)
                damageable.TakeDamage(finalDamage, DamageType.ArmorPiercing);
            else
                damageable.TakeDamage(finalDamage, DamageType.Bullet);
        }

        if (weaponData.bulletHolePrefab != null)
        {
            GameObject bulletHole = Instantiate(weaponData.bulletHolePrefab, hit.point + hit.normal * 0.01f, Quaternion.LookRotation(hit.normal));
            bulletHole.transform.parent = hit.transform;
            Destroy(bulletHole, 30f);
        }
    }

    protected virtual void CreateBulletTrail(Vector3 start, Vector3 end)
    {
        if (weaponData.bulletTrailPrefab == null) return;

        GameObject trail = Instantiate(weaponData.bulletTrailPrefab, start, Quaternion.identity);
        TrailRenderer trailRenderer = trail.GetComponent<TrailRenderer>();
        if (trailRenderer != null)
        {
            trailRenderer.time = 0.1f;
            trailRenderer.startWidth = 0.05f;
            trailRenderer.endWidth = 0.01f;
        }

        StartCoroutine(MoveBulletTrail(trail, start, end));
    }

    protected virtual IEnumerator MoveBulletTrail(GameObject trail, Vector3 start, Vector3 end)
    {
        float duration = 0.1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            trail.transform.position = Vector3.Lerp(start, end, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        trail.transform.position = end;
        Destroy(trail, 0.5f);
    }

    protected virtual void CreateExplosion(Vector3 position)
    {
        Collider[] colliders = Physics.OverlapSphere(position, 3f, hitLayers);
        foreach (var col in colliders)
        {
            IDamageable damageable = col.GetComponent<IDamageable>();
            if (damageable != null)
            {
                float distance = Vector3.Distance(position, col.transform.position);
                float damage = weaponData.secondaryFireDamage * (1f - distance / 3f);
                damageable.TakeDamage(damage, DamageType.Explosion);
            }
        }
    }
}