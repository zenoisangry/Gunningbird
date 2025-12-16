using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] protected float speed = 50f;
    [SerializeField] protected float lifetime = 5f;
    [SerializeField] protected float damage = 25f;
    [SerializeField] protected bool destroyOnImpact = true;
    [SerializeField] protected LayerMask hitLayers;
    [SerializeField] protected GameObject impactEffect;
    [SerializeField] protected GameObject trailEffect;

    [Header("Physics")]
    [SerializeField] protected bool useGravity = false;
    [SerializeField] protected float explosionRadius = 0f;
    [SerializeField] protected float penetrationCount = 0f;

    protected IWeaponOwner owner;
    protected Rigidbody rb;
    protected Vector3 lastPosition;
    protected int currentPenetrations;

    public virtual void Initialize(float projectileDamage, IWeaponOwner projectileOwner, Vector3 direction = default)
    {
        damage = projectileDamage;
        owner = projectileOwner;

        rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();

        rb.useGravity = useGravity;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        if (direction != default)
        {
            rb.linearVelocity = direction * speed;
        }
        else
        {
            rb.linearVelocity = transform.forward * speed;
        }

        lastPosition = transform.position;

        // Create trail effect
        if (trailEffect != null)
        {
            GameObject trail = Instantiate(trailEffect, transform.position, Quaternion.identity);
            trail.transform.parent = transform;
        }

        Destroy(gameObject, lifetime);
    }

    protected virtual void Update()
    {
        // Check for hit between frames
        Vector3 currentPosition = transform.position;
        Vector3 direction = (currentPosition - lastPosition).normalized;
        float distance = Vector3.Distance(lastPosition, currentPosition);

        if (Physics.Raycast(lastPosition, direction, out RaycastHit hit, distance, hitLayers))
        {
            ProcessHit(hit);
            if (destroyOnImpact && currentPenetrations >= penetrationCount)
            {
                Destroy(gameObject);
            }
            else
            {
                currentPenetrations++;
                // Reduce damage after penetration
                damage *= 0.7f;
            }
        }

        lastPosition = currentPosition;
    }

    protected virtual void ProcessHit(RaycastHit hit)
    {
        IDamageable damageable = hit.collider.GetComponent<IDamageable>();

        if (damageable != null)
        {
            damageable.TakeDamage(damage, DamageType.Bullet);
        }

        // Create impact effect
        if (impactEffect != null)
        {
            GameObject impact = Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
            Destroy(impact, 2f);
        }

        // Handle explosion
        if (explosionRadius > 0f)
        {
            CreateExplosion(hit.point);
        }
    }

    protected virtual void CreateExplosion(Vector3 position)
    {
        Collider[] colliders = Physics.OverlapSphere(position, explosionRadius, hitLayers);

        foreach (Collider collider in colliders)
        {
            IDamageable damageable = collider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                float distance = Vector3.Distance(position, collider.transform.position);
                float explosionDamage = damage * (1f - distance / explosionRadius);
                damageable.TakeDamage(explosionDamage, DamageType.Explosion);
            }

            // Apply explosion force
            Rigidbody rb = collider.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(500f, position, explosionRadius);
            }
        }
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (hitLayers == (hitLayers | (1 << collision.gameObject.layer)))
        {
            RaycastHit hit = new RaycastHit
            {
                point = collision.contacts[0].point,
                normal = collision.contacts[0].normal
            };

            ProcessHitWithCollider(hit, collision.collider);

            if (destroyOnImpact && currentPenetrations >= penetrationCount)
            {
                Destroy(gameObject);
            }
            else
            {
                currentPenetrations++;
                damage *= 0.7f;
            }
        }
    }

    protected virtual void ProcessHitWithCollider(RaycastHit hit, Collider collider)
    {
        IDamageable damageable = collider.GetComponent<IDamageable>();

        if (damageable != null)
        {
            damageable.TakeDamage(damage, DamageType.Bullet);
        }

        if (impactEffect != null)
        {
            GameObject impact = Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
            Destroy(impact, 2f);
        }

        if (explosionRadius > 0f)
        {
            CreateExplosion(hit.point);
        }
    }
}