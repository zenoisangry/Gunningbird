using UnityEngine;

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
        int pelletCount = 8; // Can be added to WeaponData
        Transform cameraTransform = owner.GetCameraTransform();

        for (int i = 0; i < pelletCount; i++)
        {
            Vector3 fireDirection = CalculateSpreadDirection(cameraTransform.forward);

            if (Physics.Raycast(cameraTransform.position, fireDirection, out RaycastHit hit, maxDistance, hitLayers))
            {
                ProcessHit(hit, fireDirection, weaponData.damage / pelletCount);
            }
        }
    }

    protected virtual void FireSniper()
    {
        Transform cameraTransform = owner.GetCameraTransform();
        Vector3 fireDirection = cameraTransform.forward; // No spread for sniper

        if (weaponData.bulletTrailPrefab != null)
        {
            CreateBulletTrail(firePoint.position, firePoint.position + fireDirection * maxDistance);
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
                // Handle zoom in player controller
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
        GameObject grenade = CreateProjectile(cameraTransform.position, cameraTransform.forward);

        Rigidbody rb = grenade.GetComponent<Rigidbody>();
        if (rb != null)
        {
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

        // Armor piercing goes through multiple targets
        RaycastHit[] hits = Physics.RaycastAll(cameraTransform.position, fireDirection, maxDistance, hitLayers);

        for (int i = 0; i < hits.Length; i++)
        {
            ProcessHit(hits[i], fireDirection, weaponData.secondaryFireDamage * 0.7f); // Reduced damage after penetration
        }
    }

    protected virtual void ProcessHit(RaycastHit hit, Vector3 fireDirection, float damageMultiplier = 1f, bool ignoreArmor = false)
    {
        IDamageable damageable = hit.collider.GetComponent<IDamageable>();

        if (damageable != null)
        {
            float finalDamage = weaponData.damage * damageMultiplier;

            // Check for headshot
            if (hit.collider.CompareTag("Head"))
            {
                finalDamage *= weaponData.headshotMultiplier;
            }

            if (ignoreArmor)
            {
                damageable.TakeDamage(finalDamage, DamageType.ArmorPiercing);
            }
            else
            {
                damageable.TakeDamage(finalDamage, DamageType.Bullet);
            }
        }

        // Create bullet hole
        if (weaponData.bulletHolePrefab != null)
        {
            GameObject bulletHole = Instantiate(weaponData.bulletHolePrefab, hit.point + hit.normal * 0.01f, Quaternion.LookRotation(hit.normal));
            bulletHole.transform.parent = hit.transform;

            Destroy(bulletHole, 30f); // Clean up after 30 seconds
        }
    }

    protected virtual void CreateBulletTrail(Vector3 start, Vector3 end)
    {
        GameObject trail = Instantiate(weaponData.bulletTrailPrefab, start, Quaternion.identity);
        TrailRenderer trailRenderer = trail.GetComponent<TrailRenderer>();

        if (trailRenderer != null)
        {
            // Configure trail renderer
            trailRenderer.time = 0.1f;
            trailRenderer.startWidth = 0.05f;
            trailRenderer.endWidth = 0.01f;
        }

        StartCoroutine(MoveBulletTrail(trail, start, end));
    }

    protected virtual System.Collections.IEnumerator MoveBulletTrail(GameObject trail, Vector3 start, Vector3 end)
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

    protected virtual GameObject CreateProjectile(Vector3 position, Vector3 direction)
    {
        // Create a simple projectile - can be enhanced with actual projectile prefabs
        GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectile.transform.position = position;
        projectile.transform.localScale = Vector3.one * 0.1f;

        Rigidbody rb = projectile.AddComponent<Rigidbody>();
        rb.useGravity = false;

        projectile.AddComponent<Projectile>().Initialize(weaponData.secondaryFireDamage, owner);

        Destroy(projectile, 5f);
        return projectile;
    }

    protected virtual void CreateExplosion(Vector3 position)
    {
        // Create explosion effect and deal area damage
        GameObject explosion = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        explosion.transform.position = position;
        explosion.transform.localScale = Vector3.one * 3f;

        // Deal area damage
        Collider[] colliders = Physics.OverlapSphere(position, 3f, hitLayers);
        foreach (Collider col in colliders)
        {
            IDamageable damageable = col.GetComponent<IDamageable>();
            if (damageable != null)
            {
                float distance = Vector3.Distance(position, col.transform.position);
                float damage = weaponData.secondaryFireDamage * (1f - distance / 3f);
                damageable.TakeDamage(damage, DamageType.Explosion);
            }
        }

        Destroy(explosion, 0.5f);
    }
}