using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float speed = 50f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private float damage = 25f;
    [SerializeField] private LayerMask ignoreLayers;
    [SerializeField] private GameObject impactEffect;
    [SerializeField] private GameObject trailEffect;
    
    private IWeaponOwner owner;
    private float headshotMultiplier = 2f;
    private Rigidbody rb;

    // <(= O . O =)> fat cat!
    [Header("Impact Effects")]
    [SerializeField] private ImpactData defaultImpactData;


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
        if (collision == null || collision.gameObject == null) return;

        if ((ignoreLayers.value & (1 << collision.gameObject.layer)) != 0 ||
            collision.gameObject.layer == 10 ||
            collision.gameObject.layer == 9)
        {
            return;
        }
        
        IDamageable damageable = collision.collider.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            Debug.Log(collision.collider.gameObject);
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

    // <(= O . O =)> fat cat!
        if (collision.contactCount > 0)
        {
            ContactPoint contact = collision.contacts[0];

            ImpactData data = ImpactManager.Instance != null
                ? ImpactManager.Instance.Get(collision.gameObject.tag)
                : defaultImpactData;

            if (data != null)
            {
                SpawnImpactParticle(data, contact.point, contact.normal);
                SpawnDecal(data, contact.point, contact.normal);

                if (data.impactSound != null)
                    AudioSource.PlayClipAtPoint(data.impactSound, contact.point, data.volume);
            }

    // <(= O . O =)> fat cat!
            collision.gameObject.GetComponent<FractureOnImpact>()?.Fracture(contact.point);
        }

        if (trailEffect != null)
        {
            transform.DetachChildren();
            trailEffect.GetComponent<TrailScript>().fadeOut(0.35f);
        }

        Destroy(gameObject);
    }

    // <(= O . O =)> fat cat!
    private void SpawnImpactParticle(ImpactData data, Vector3 point, Vector3 normal)
    {
        if (data.impactParticlePrefab == null) return;

        GameObject vfx = Instantiate(
            data.impactParticlePrefab,
            point,
            Quaternion.LookRotation(normal)
        );

        ParticleSystem ps = vfx.GetComponent<ParticleSystem>();
        float dur = ps != null
            ? ps.main.duration + ps.main.startLifetime.constantMax
            : 3f;

        Destroy(vfx, dur);
    }

    // <(= O . O =)> fat cat!
    private void SpawnDecal(ImpactData data, Vector3 point, Vector3 normal)
    {
        if (data.impactDecalSprite == null) return;

        GameObject decal = new GameObject("ImpactDecal");
        decal.transform.position = point + normal * 0.01f; 
        decal.transform.rotation = Quaternion.LookRotation(-normal);
        decal.transform.Rotate(Vector3.forward, Random.Range(0f, 360f));

        SpriteRenderer sr = decal.AddComponent<SpriteRenderer>();
        sr.sprite = data.impactDecalSprite;
        sr.sortingOrder = 1;

        float size = Random.Range(data.decalSizeRange.x, data.decalSizeRange.y);
        decal.transform.localScale = Vector3.one * size;

        Destroy(decal, data.decalDuration);
    }
}
