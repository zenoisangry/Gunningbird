using UnityEngine;

public class EnemyWeaponAttack : MonoBehaviour
{
    [Header("Weapon")]
    [SerializeField] private BaseWeapon weapon;

    [Header("Attack References")]
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private LayerMask hitLayers;

    private Transform target;
    private float lastAttackTime;

    public void SetTarget(Transform t)
    {
        target = t;
    }

    public bool CanAttack()
    {
        if (weapon == null || target == null) return false;

        float cooldown = weapon.GetWeaponData().weaponType == WeaponType.Melee
            ? weapon.GetWeaponData().meleeCooldown
            : 60f / weapon.GetWeaponData().fireRate;

        return Time.time >= lastAttackTime + cooldown && weapon.CanFire();
    }

    public void Attack(Transform t)
    {
        if (!CanAttack()) return;

        if (t != null)
            target = t;

        if (target == null) return;

        lastAttackTime = Time.time;

        Vector3 dir = (target.position - transform.position).normalized;
        transform.forward = new Vector3(dir.x, 0, dir.z);

        weapon.FireWeapon();
    }

    public WeaponData GetWeaponData()
    {
        return weapon != null ? weapon.GetWeaponData() : null;
    }
}