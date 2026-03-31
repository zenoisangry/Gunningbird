using UnityEngine;
using System.Collections;

public class RangedWeapon : BaseWeapon
{
    [Header("Ranged Weapon Settings")]
    [SerializeField] protected Transform firePoint;
    [SerializeField] protected LayerMask hitLayers;
    [SerializeField] protected float maxDistance = 100f;
    [SerializeField] protected float bulletForce = 1000f;

    public override void FireWeapon()
    {
        switch (weaponData.weaponType)
        {
            case WeaponType.Shotgun:
                FireShotgun();
                break;
            case WeaponType.Sniper:
                FireSniper();
                break;
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
        Vector2 startingAngle = weaponData.projectileAngles[0] * (projectileNumber - 1) / 2;

        if (weaponData.bulletPrefab != null)
        {
            Vector3 adjustedFD = Quaternion.Euler(startingAngle.y, startingAngle.x, 0) * fireDirection;
            StartCoroutine(FanPattern(projectileNumber, adjustedFD, firePosition));
        }
    }

    IEnumerator FanPattern(float projectiles, Vector3 adjustedFD, Vector3 firePosition)
    {
        transform.parent.parent.parent.GetComponentInChildren<CorruptedGunslingerNav>().StayStill(weaponData.fanDelay*weaponData.projectileNumber);
        float timer = 0;
        while (projectiles > 0)
        {
            if (timer > weaponData.fanDelay)
            {
                GameObject projectileGO = Instantiate(weaponData.bulletPrefab, firePosition, Quaternion.LookRotation(adjustedFD));
                Projectile projectile = projectileGO.GetComponent<Projectile>();
                if (projectile != null)
                {
                    projectile.Initialize(weaponData.damage, owner, adjustedFD, weaponData.headshotMultiplier);
                }

                projectiles--;
                adjustedFD = Quaternion.Euler(-weaponData.projectileAngles[0].y, -weaponData.projectileAngles[0].x, 0) * adjustedFD;
                timer = 0;
            }
            timer += Time.deltaTime;
            yield return null;
        }
    }
    protected virtual void FireSpread()
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
        Vector2 startingAngle = weaponData.projectileAngles[0] * (projectileNumber - 1) / 2;

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

    protected virtual void FireSniper()
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

        if (weaponData.bulletPrefab != null)
        {
            GameObject projGO = Instantiate(weaponData.bulletPrefab, firePosition, Quaternion.LookRotation(fireDirection));
            Projectile proj = projGO.GetComponent<Projectile>();
            if (proj != null)
                proj.Initialize(weaponData.damage, owner, fireDirection, weaponData.headshotMultiplier);
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
        if (weaponData == null) return;

        int burstCount = 3;
        float burstDelay = 60f / (weaponData.fireRate * 2f);

        StartCoroutine(BurstFireRoutine(burstCount, burstDelay));
    }

    protected virtual IEnumerator BurstFireRoutine(int count, float delay)
    {
        for (int i = 0; i < count; i++)
        {
            FireSingleBullet();
            if (i < count - 1)
                yield return new WaitForSeconds(delay);
        }
    }

    protected virtual void FireGrenade()
    {
        if (weaponData == null || owner == null) return;

        Transform cameraTransform = owner.GetCameraTransform();
        if (cameraTransform == null) return;

        if (weaponData.bulletPrefab != null)
        {
            Vector3 firePosition = firePoint != null ? firePoint.position : transform.position;
            GameObject grenade = Instantiate(weaponData.bulletPrefab, firePosition, Quaternion.LookRotation(cameraTransform.forward));
            Projectile proj = grenade.GetComponent<Projectile>();
            if (proj != null)
                proj.Initialize(weaponData.damage, owner, cameraTransform.forward, weaponData.headshotMultiplier);

            Rigidbody rb = grenade.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(cameraTransform.forward * bulletForce, ForceMode.Force);
            }
        }
    }

    protected virtual void FireExplosiveRound()
    {
        if (weaponData == null || owner == null) return;

        Transform cameraTransform = owner.GetCameraTransform();
        if (cameraTransform == null) return;

        Vector3 fireDirection = CalculateSpreadDirection(cameraTransform.forward);

        if (Physics.Raycast(cameraTransform.position, fireDirection, out RaycastHit hit, maxDistance, hitLayers))
        {
            float damageMultiplier = weaponData.secondaryFireDamage / weaponData.damage;
            CreateExplosion(hit.point);
        }
    }

    protected virtual void FireArmorPiercingRound()
    {
        if (weaponData == null || owner == null) return;

        Transform cameraTransform = owner.GetCameraTransform();
        if (cameraTransform == null) return;

        Vector3 fireDirection = cameraTransform.forward;

        RaycastHit[] hits = Physics.RaycastAll(cameraTransform.position, fireDirection, maxDistance, hitLayers);

        float damageMultiplier = (weaponData.secondaryFireDamage * 0.7f) / weaponData.damage;
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
        if (weaponData == null) return;

        float explosionRadius = 3f;
        Collider[] colliders = Physics.OverlapSphere(position, explosionRadius, hitLayers);
        foreach (var col in colliders)
        {
            IDamageable damageable = col.GetComponent<IDamageable>();
            if (damageable != null)
            {
                float distance = Vector3.Distance(position, col.transform.position);
                float damageFalloff = Mathf.Clamp01(1f - distance / explosionRadius);
                float damage = weaponData.secondaryFireDamage * damageFalloff;
                damageable.TakeDamage(damage, DamageType.Explosion);
            }
        }
    }
}