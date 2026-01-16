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

        float cooldown = weapon.GetWeaponData().meleeCooldown;

        bool ready = Time.time >= lastAttackTime + cooldown && weapon.CanFire();

        if (!ready)
            Debug.Log($"{name} non pronto ad attaccare. Tempo rimanente: {Mathf.Max(0, lastAttackTime + cooldown - Time.time):F2}s");

        return ready;
    }

    public void Attack(Transform t)
    {
        if (!CanAttack())
        {
            Debug.LogWarning($"{name} non può attaccare in questo momento!");
            return;
        }

        if (t != null) target = t;
        if (target == null) return;

        lastAttackTime = Time.time;

        Debug.Log($"{name} sta attaccando {target.name} con {weapon.GetWeaponData().weaponName}");

        Vector3 dir = (target.position - transform.position).normalized;
        transform.forward = new Vector3(dir.x, 0, dir.z);

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