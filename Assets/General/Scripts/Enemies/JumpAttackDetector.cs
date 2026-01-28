using UnityEngine;

public class JumpAttackDetector : MonoBehaviour
{
    public MeleeWeapon referenceWeapon;

    private CapsuleCollider hitBox;
    private bool hasAttacked = false;

    private void Start()
    {
        hitBox = GetComponent<CapsuleCollider>();
        if (hitBox != null)
        {
            hitBox.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == 3 && !hasAttacked)
        {
            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null && referenceWeapon != null)
            {
                float damage = referenceWeapon.GetWeaponData().meleeDamage;
                damageable.TakeDamage(damage, DamageType.Melee);

                Debug.Log($"[JumpAttack] Hit player! Damage: {damage}");

                hasAttacked = true;
            }
        }
    }

    public void ResetAttack()
    {
        hasAttacked = false;
    }

    public void EnableHitBox()
    {
        if (hitBox != null)
        {
            hitBox.enabled = true;
            hasAttacked = false;
        }
    }

    public void DisableHitBox()
    {
        if (hitBox != null)
        {
            hitBox.enabled = false;
        }
    }
}