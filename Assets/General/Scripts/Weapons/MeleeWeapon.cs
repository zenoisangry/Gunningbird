using UnityEngine;
using System.Collections;

public class MeleeWeapon : BaseWeapon
{
    [Header("Melee Settings")]
    [SerializeField] protected Transform meleeAttackPoint;
    [SerializeField] protected LayerMask meleeHitLayers;

    public override void FireWeapon()
    {
        PerformMeleeAttack();
    }

    protected override void FireSecondaryWeapon()
    {
        PerformHeavyMeleeAttack();
    }

    protected virtual void PerformMeleeAttack()
    {
        if (isFiring || weaponData == null) return;

        StartCoroutine(
            MeleeAttackRoutine(
                weaponData.meleeDamage,
                weaponData.meleeCooldown
            )
        );
    }

    protected virtual void PerformHeavyMeleeAttack()
    {
        if (isFiring || weaponData == null) return;

        StartCoroutine(
            MeleeAttackRoutine(
                weaponData.meleeDamage * 1.5f,
                weaponData.meleeCooldown * 1.5f
            )
        );
    }

    protected virtual IEnumerator MeleeAttackRoutine(float damage, float cooldown)
    {
        if (weaponData == null) yield break;

        isFiring = true;

        if (animator != null && !string.IsNullOrEmpty(weaponData.meleeAnimationTrigger))
            animator.SetTrigger(weaponData.meleeAnimationTrigger);

        yield return new WaitForSeconds(weaponData.meleeHitDelay);

        PerformMeleeHit(damage);

        yield return new WaitForSeconds(cooldown);

        isFiring = false;
    }

    protected virtual void PerformMeleeHit(float damage)
    {
        if (weaponData == null) return;

        Vector3 attackPoint = meleeAttackPoint != null ? meleeAttackPoint.position : transform.position;
        Vector3 attackDirection = meleeAttackPoint != null && owner != null 
            ? (owner.GetFireTransform() != null ? owner.GetFireTransform().forward : transform.forward)
            : transform.forward;

        Collider[] hitColliders = Physics.OverlapSphere(attackPoint, weaponData.meleeRange, meleeHitLayers);

        foreach (Collider hitCollider in hitColliders)
        {
            IDamageable damageable = hitCollider.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                // Check angle from attack point/direction instead of camera
                Vector3 directionToTarget = (hitCollider.transform.position - attackPoint).normalized;
                float angle = Vector3.Angle(directionToTarget, attackDirection);

                if (angle > weaponData.meleeAngle)
                    continue;

                damageable.TakeDamage(damage, DamageType.Melee);
            }
        }
    }

    public override bool CanFire() => !isFiring && weaponData != null;
    public override bool CanSecondaryFire() =>
        !isFiring && weaponData != null && weaponData.secondaryFireType != SecondaryFireType.None;

    public override bool CanReload() => false;
    public override void Reload() { }
}