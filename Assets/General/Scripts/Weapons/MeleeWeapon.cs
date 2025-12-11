using UnityEngine;
using System.Collections;

public class MeleeWeapon : BaseWeapon
{
    [Header("Melee Settings")]
    [SerializeField] protected Transform meleeAttackPoint;
    [SerializeField] protected LayerMask meleeHitLayers;
    [SerializeField] protected float meleeRadius = 1f;
    [SerializeField] protected bool canInstakill = true;

    protected override void FireWeapon()
    {
        PerformMeleeAttack();
    }

    protected override void FireSecondaryWeapon()
    {
        // Secondary fire for melee could be a heavy attack
        PerformHeavyMeleeAttack();
    }

    protected virtual void PerformMeleeAttack()
    {
        if (isFiring) return;

        StartCoroutine(MeleeAttackRoutine(weaponData.meleeDamage, weaponData.meleeCooldown));
    }

    protected virtual void PerformHeavyMeleeAttack()
    {
        if (isFiring) return;

        StartCoroutine(MeleeAttackRoutine(weaponData.meleeDamage * 1.5f, weaponData.meleeCooldown * 1.5f));
    }

    protected virtual IEnumerator MeleeAttackRoutine(float damage, float cooldown)
    {
        isFiring = true;

        // Play animation
        animator.SetTrigger(weaponData.meleeAnimationTrigger);

        // Wait for animation to reach the hit point
        yield return new WaitForSeconds(0.2f);

        // Perform the actual attack
        PerformMeleeHit(damage);

        // Wait for cooldown
        yield return new WaitForSeconds(cooldown);

        isFiring = false;
    }

    protected virtual void PerformMeleeHit(float damage)
    {
        Vector3 attackPoint = meleeAttackPoint != null ? meleeAttackPoint.position : transform.position;

        // Check for enemies in range
        Collider[] hitColliders = Physics.OverlapSphere(attackPoint, weaponData.meleeRange, meleeHitLayers);

        foreach (Collider hitCollider in hitColliders)
        {
            // Check if enemy is in front of player (within melee angle)
            Vector3 directionToEnemy = hitCollider.transform.position - transform.position;
            float angle = Vector3.Angle(directionToEnemy, owner.GetCameraTransform().forward);

            if (angle <= weaponData.meleeAngle)
            {
                IDamageable damageable = hitCollider.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    float finalDamage = damage;

                    // Check for instant kill
                    if (canInstakill && damageable.GetHealth() <= weaponData.meleeInstaKillThreshold)
                    {
                        finalDamage = damageable.GetHealth(); // Deal exactly remaining health for instant kill
                    }

                    damageable.TakeDamage(finalDamage, DamageType.Melee);

                    // Apply knockback
                    ApplyKnockback(hitCollider.transform);

                    // Create hit effect
                    CreateMeleeHitEffect(hitCollider.transform.position);
                }
            }
        }
    }

    protected virtual void ApplyKnockback(Transform target)
    {
        Rigidbody targetRb = target.GetComponent<Rigidbody>();
        if (targetRb != null)
        {
            Vector3 knockbackDirection = (target.position - transform.position).normalized;
            knockbackDirection.y = 0.5f;

            targetRb.AddForce(knockbackDirection * 500f, ForceMode.Impulse);
        }
    }

    protected virtual void CreateMeleeHitEffect(Vector3 position)
    {
        GameObject effect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        effect.transform.position = position;
        effect.transform.localScale = Vector3.one * 0.3f;

        Renderer renderer = effect.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.red;
        }

        Destroy(effect, 0.3f);
    }

    public override bool CanFire()
    {
        return !isFiring;
    }

    public override bool CanSecondaryFire()
    {
        return !isFiring && weaponData.secondaryFireType != SecondaryFireType.None;
    }

    // Melee weapons don't use ammo
    public override bool CanReload()
    {
        return false;
    }

    public override void Reload()
    {
        // Melee weapons don't reload
    }

    protected override void PlayFireEffects()
    {
        // Play melee attack sound if available
        if (weaponData.shootSound != null && audioSource != null)
            audioSource.PlayOneShot(weaponData.shootSound);
    }

    // Draw scene gizmos for debugging
    protected virtual void OnDrawGizmosSelected()
    {
        if (meleeAttackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(meleeAttackPoint.position, weaponData.meleeRange);
        }

        // Draw melee angle cone
        Vector3 cameraForward = owner != null ? owner.GetCameraTransform().forward : transform.forward;
        Gizmos.color = Color.yellow;
        DrawMeleeCone(transform.position, cameraForward, weaponData.meleeRange, weaponData.meleeAngle);
    }

    protected virtual void DrawMeleeCone(Vector3 position, Vector3 direction, float range, float angle)
    {
        float halfAngle = angle * 0.5f;
        int steps = 10;

        for (int i = 0; i <= steps; i++)
        {
            float currentAngle = -halfAngle + (angle / steps) * i;
            Quaternion rotation = Quaternion.Euler(0, currentAngle, 0);
            Vector3 rotatedDirection = rotation * direction;

            Vector3 endPoint = position + rotatedDirection * range;
            Gizmos.DrawLine(position, endPoint);
        }
    }
}