using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float speed = 50f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private float damage = 25f;
    [SerializeField] private LayerMask hitLayers;
    [SerializeField] private GameObject impactEffect;
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
        Debug.Log($"[Projectile] Hit object: {collision.gameObject.name}");

        if ((hitLayers.value & (1 << collision.gameObject.layer)) == 0)
        {
            Debug.Log("[Projectile] Hit layer ignored");
            return;
        }

        IDamageable damageable = collision.collider.GetComponentInParent<IDamageable>();
        if (damageable == null)
        {
            Debug.Log("[Projectile] No IDamageable found on hit object");
        }
        else
        {
            bool isHeadshot = collision.collider.CompareTag("Head");
            float finalDamage = damage * (isHeadshot ? headshotMultiplier : 1f);

            Debug.Log(
                $"[Projectile] DAMAGE APPLIED" +
                $"Target: {collision.collider.name}" +
                $"Headshot: {isHeadshot}" +
                $"Damage: {finalDamage}"
            );

            damageable.TakeDamage(finalDamage, DamageType.Bullet);
        }

        if (impactEffect != null)
        {
            GameObject impact = Instantiate(
                impactEffect,
                collision.contacts[0].point,
                Quaternion.LookRotation(collision.contacts[0].normal)
            );
            Destroy(impact, 2f);
        }

        Destroy(gameObject);
    }
}