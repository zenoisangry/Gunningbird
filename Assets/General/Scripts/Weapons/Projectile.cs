using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float speed = 50f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private float damage = 25f;
    [SerializeField] private LayerMask hitLayers;
    [SerializeField] private GameObject impactEffect;
    [SerializeField] private GameObject trailEffect;
    private IWeaponOwner owner;
    private float headshotMultiplier = 2f;
    private Rigidbody rb;

    public void Initialize(float projectileDamage, IWeaponOwner projectileOwner, Vector3 direction, float projectileHeadshotMultiplier = 2f)
    {
        damage = projectileDamage;
        owner = projectileOwner;
        headshotMultiplier = projectileHeadshotMultiplier;

        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

        rb.useGravity = false;
        rb.linearVelocity = direction.normalized * speed;

        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("collision entered");
        if (collision == null || collision.gameObject == null) return;

        Debug.Log("check 1 passed");
        if ((hitLayers.value & (1 << collision.gameObject.layer)) == 0 && collision.gameObject.layer != 0)
        {
            return;
        }

        IDamageable damageable = collision.collider.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            bool isHeadshot = collision.collider.CompareTag("Head");
            float finalDamage = damage * (isHeadshot ? headshotMultiplier : 1f);

            damageable.TakeDamage(finalDamage, DamageType.Bullet);
        }

        if (impactEffect != null && collision.contactCount > 0)
        {
            ContactPoint contact = collision.contacts[0];
            GameObject impact = Instantiate(
                impactEffect,
                contact.point,
                Quaternion.LookRotation(contact.normal)
            );
            Destroy(impact, 2f);
        }
        if (trailEffect != null)
        {
            transform.DetachChildren();
            trailEffect.GetComponent<TrailScript>().fadeOut(0.35f);
        }
        Destroy(gameObject);
    }
}