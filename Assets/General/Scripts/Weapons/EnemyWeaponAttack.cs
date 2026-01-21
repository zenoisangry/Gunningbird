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

        WeaponData data = weapon.GetWeaponData();
        if (data == null) return false;

        float cooldown = data.meleeCooldown;
        bool ready = Time.time >= lastAttackTime + cooldown && weapon.CanFire();

        return ready;
    }

    public void Attack(Transform t)
    {
        if (!CanAttack())
        {
            return;
        }

        if (t != null) target = t;
        if (target == null || weapon == null) return;

        lastAttackTime = Time.time;

        Vector3 dir = (target.position - transform.position).normalized;
        dir.y = 0f; // Keep rotation on horizontal plane
        if (dir != Vector3.zero)
            transform.forward = dir.normalized;

        weapon.FireWeapon();
    }

    public WeaponData GetWeaponData()
    {
        return weapon != null ? weapon.GetWeaponData() : null;
    }

    public Transform GetTarget()
    {
        return target;
    }
}