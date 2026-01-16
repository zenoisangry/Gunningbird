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
        if (isFiring) return;

        StartCoroutine(
            MeleeAttackRoutine(
                weaponData.meleeDamage,
                weaponData.meleeCooldown
            )
        );
    }

    protected virtual void PerformHeavyMeleeAttack()
    {
        if (isFiring) return;

        StartCoroutine(
            MeleeAttackRoutine(
                weaponData.meleeDamage * 1.5f,
                weaponData.meleeCooldown * 1.5f
            )
        );
    }

    protected virtual IEnumerator MeleeAttackRoutine(float damage, float cooldown)
    {
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
        Vector3 attackPoint = meleeAttackPoint != null ? meleeAttackPoint.position : transform.position;

        Collider[] hitColliders = Physics.OverlapSphere(attackPoint, weaponData.meleeRange, meleeHitLayers);

        foreach (Collider hitCollider in hitColliders)
        {
            IDamageable damageable = hitCollider.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                float finalDamage = damage;

                if (owner != null)
                {
                    Vector3 directionToEnemy = hitCollider.transform.position - transform.position;
                    float angle = Vector3.Angle(directionToEnemy, owner.GetCameraTransform().forward);

                    if (angle > weaponData.meleeAngle)
                        continue;
                }

                damageable.TakeDamage(finalDamage, DamageType.Melee);
            }
        }
    }

    public override bool CanFire() => !isFiring;
    public override bool CanSecondaryFire() =>
        !isFiring && weaponData.secondaryFireType != SecondaryFireType.None;

    public override bool CanReload() => false;
    public override void Reload() { }
}