using UnityEngine;

public class JumpAttackDetector : MonoBehaviour
{
    public MeleeWeapon referenceWeapon;
    private CapsuleCollider hitBox;

    private void Start()
    {
        hitBox = GetComponent<CapsuleCollider>();
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == 3)
        {
            IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
            if (damageable != null){
                damageable.TakeDamage(referenceWeapon.GetWeaponData().damage, DamageType.Melee);
            }
        }
        hitBox.enabled = false;
    }
}
