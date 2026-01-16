using UnityEngine;
using System.Collections;

public class MeleeWeapon : BaseWeapon
{
    [Header("Melee Settings")]
    [SerializeField] protected Transform meleeAttackPoint;
    [SerializeField] protected LayerMask meleeHitLayers;

    protected override void FireWeapon()
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

        animator.SetTrigger(weaponData.meleeAnimationTrigger);

        yield return new WaitForSeconds(weaponData.meleeHitDelay);

        PerformMeleeHit(damage);

        yield return new WaitForSeconds(cooldown);

        isFiring = false;
    }

    protected virtual void PerformMeleeHit(float damage)
    {
        Vector3 attackPoint = meleeAttackPoint != null
            ? meleeAttackPoint.position
            : transform.position;

        Collider[] hitColliders = Physics.OverlapSphere(
            attackPoint,
            weaponData.meleeRange,
            meleeHitLayers
        );

        foreach (Collider hit in hitColliders)
        {
            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable == null) continue;

            bool isHeadshot = hit.CompareTag("Head");
            float finalDamage = damage * (isHeadshot ? weaponData.headshotMultiplier : 1f);

            damageable.TakeDamage(finalDamage, DamageType.Melee);
        }
    }

    public override bool CanFire() => !isFiring;
    public override bool CanSecondaryFire() =>
        !isFiring && weaponData.secondaryFireType != SecondaryFireType.None;

    public override bool CanReload() => false;
    public override void Reload() { }
}