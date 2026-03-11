using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

public class PatternWeapon : BaseWeapon
{
    [Header("Pattern Weapon Settings")]
    [SerializeField] protected Transform firePoint;
    [SerializeField] protected LayerMask hitLayers;
    [SerializeField] protected float maxDistance = 100f;
    [SerializeField] protected float bulletForce = 1000f;

    public override void FireWeapon()
    {
        switch (weaponData.weaponType)
        {
            case WeaponType.Spread:
                FireSpread();
                break;
            case WeaponType.Fan:
                FireFan();
                break;
            default:
                FireSingleBullet();
                break;
        }
    }

    public override void PrimaryFire()
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

    protected virtual void FireSpread()
    {

    }

    protected virtual void FireSingleBullet()
    {
        if (weaponData == null || owner == null) return;

        Transform cameraTransform = owner.GetCameraTransform();
        if (cameraTransform == null) return;

        Vector3 firePosition = firePoint != null ? firePoint.position : transform.position;

        Vector3 targetPoint;
        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, maxDistance, hitLayers))
        {
            targetPoint = hit.point;
            Debug.DrawLine(firePosition, targetPoint, Color.red, 1f);
        }
        else
        {
            targetPoint = cameraTransform.position + cameraTransform.forward * maxDistance;
        }

        Vector3 fireDirection = (targetPoint - firePosition).normalized;
        fireDirection = CalculateSpreadDirection(fireDirection);

        if (weaponData.bulletPrefab != null)
        {
            GameObject projectileGO = Instantiate(weaponData.bulletPrefab, firePosition, Quaternion.LookRotation(fireDirection));
            Projectile projectile = projectileGO.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.Initialize(weaponData.damage, owner, fireDirection, weaponData.headshotMultiplier);
            }

        }

        if (weaponData.bulletTrailPrefab != null && firePoint != null)
        {
            CreateBulletTrail(firePosition, targetPoint);
        }

        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hit, maxDistance, hitLayers))
        {
            ProcessHit(hit, fireDirection);
        }
    }

    protected virtual void FireFan()
    {
        if (weaponData == null || owner == null) return;

        Transform cameraTransform = owner.GetCameraTransform();
        if (cameraTransform == null) return;

        Vector3 firePosition = firePoint != null ? firePoint.position : transform.position;

        Vector3 targetPoint;
        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, maxDistance, hitLayers))
        {
            targetPoint = hit.point;
            Debug.DrawLine(firePosition, targetPoint, Color.red, 1f);
        }
        else
        {
            targetPoint = cameraTransform.position + cameraTransform.forward * maxDistance;
        }

        Vector3 fireDirection = (targetPoint - firePosition).normalized;

        //Calculate starting angle based on how many projectiles are there
        float projectileNumber = weaponData.projectileNumber;
        Vector2 startingAngle = weaponData.projectileAngles[0] * (projectileNumber -1) /2;

        if (weaponData.bulletPrefab != null)
        {
            Vector3 adjustedFD = Quaternion.Euler(startingAngle.y, startingAngle.x, 0) * fireDirection;
            while (projectileNumber > 0)
            {
                GameObject projectileGO = Instantiate(weaponData.bulletPrefab, firePosition, Quaternion.LookRotation(adjustedFD));
                Projectile projectile = projectileGO.GetComponent<Projectile>();
                if (projectile != null)
                {
                    projectile.Initialize(weaponData.damage, owner, adjustedFD, weaponData.headshotMultiplier);
                }

                if (weaponData.bulletTrailPrefab != null && firePoint != null)
                {
                    CreateBulletTrail(firePosition, targetPoint);
                }
                projectileNumber--;
            }
        }

        
    }

    protected virtual void FireShotgun()
    {
        if (weaponData == null || owner == null) return;

        int pelletCount = 8;
        Transform cameraTransform = owner.GetCameraTransform();
        if (cameraTransform == null) return;

        Vector3 firePosition = firePoint != null ? firePoint.position : transform.position;

        if (weaponData.useHorizontalSpread)
        {
            FireShotgunHorizontalPattern(pelletCount, cameraTransform, firePosition);
        }
        else
        {
            FireShotgunSphericalPattern(pelletCount, cameraTransform, firePosition);
        }
    }

    private void FireShotgunHorizontalPattern(int pelletCount, Transform cameraTransform, Vector3 firePosition)
    {
        Vector3 baseDirection = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;
        Vector3 cameraUp = cameraTransform.up;

        float horizontalSpread = weaponData.horizontalSpreadAngle;
        float verticalSpread = weaponData.verticalSpreadAngle;

        for (int i = 0; i < pelletCount; i++)
        {
            float horizontalOffset = Mathf.Lerp(-horizontalSpread / 2f, horizontalSpread / 2f, i / (float)(pelletCount - 1));
            float verticalOffset = Random.Range(-verticalSpread, verticalSpread);

            Vector3 spreadDirection = baseDirection;

            spreadDirection = Quaternion.AngleAxis(horizontalOffset, cameraUp) * spreadDirection;
            spreadDirection = Quaternion.AngleAxis(verticalOffset, cameraRight) * spreadDirection;

            spreadDirection = spreadDirection.normalized;

            Vector3 targetPoint;
            if (Physics.Raycast(cameraTransform.position, spreadDirection, out RaycastHit hit, maxDistance, hitLayers))
            {
                targetPoint = hit.point;
                ProcessHit(hit, spreadDirection, weaponData.damage / pelletCount);
                Debug.DrawLine(firePosition, targetPoint, Color.red, 1f);
            }
            else
            {
                targetPoint = cameraTransform.position + spreadDirection * maxDistance;
            }

            Vector3 fireDirection = (targetPoint - firePosition).normalized;

            if (weaponData.bulletPrefab != null)
            {
                GameObject projGO = Instantiate(weaponData.bulletPrefab, firePosition, Quaternion.LookRotation(fireDirection));
                Projectile proj = projGO.GetComponent<Projectile>();
                if (proj != null)
                    proj.Initialize(weaponData.damage / pelletCount, owner, fireDirection, weaponData.headshotMultiplier);
            }
        }
    }

    private void FireShotgunSphericalPattern(int pelletCount, Transform cameraTransform, Vector3 firePosition)
    {
        for (int i = 0; i < pelletCount; i++)
        {
            Vector3 spreadDirection = CalculateSpreadDirection(cameraTransform.forward);

            Vector3 targetPoint;
            if (Physics.Raycast(cameraTransform.position, spreadDirection, out RaycastHit hit, maxDistance, hitLayers))
            {
                targetPoint = hit.point;
                ProcessHit(hit, spreadDirection, weaponData.damage / pelletCount);
                Debug.DrawLine(firePosition, targetPoint, Color.red, 1f);
            }
            else
            {
                targetPoint = cameraTransform.position + spreadDirection * maxDistance;
            }

            Vector3 fireDirection = (targetPoint - firePosition).normalized;

            if (weaponData.bulletPrefab != null)
            {
                GameObject projGO = Instantiate(weaponData.bulletPrefab, firePosition, Quaternion.LookRotation(fireDirection));
                Projectile proj = projGO.GetComponent<Projectile>();
                if (proj != null)
                    proj.Initialize(weaponData.damage / pelletCount, owner, fireDirection, weaponData.headshotMultiplier);
            }
        }
    }

    protected virtual void ProcessHit(RaycastHit hit, Vector3 fireDirection, float damageMultiplier = 1f, bool ignoreArmor = false)
    {
        if (weaponData == null) return;

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
            if (hit.transform != null)
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
}