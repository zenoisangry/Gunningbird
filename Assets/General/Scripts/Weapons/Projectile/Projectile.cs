using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

    
    [SerializeField] private ImpactData defaultImpactData; //ฅ^•ﻌ•^ฅ

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

        if ((ignoreLayers.value & (1 << collision.gameObject.layer)) != 0 || collision.gameObject.layer == 10 || collision.gameObject.layer == 9)
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

        // ฅ^•ﻌ•^ฅ
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

                if (data.changeMaterial && data.impactMaterial != null) 
                    StartCoroutine(ApplyMaterialTemporary(collision.gameObject, data)); 

                if (data.activateChildEmitter && !string.IsNullOrEmpty(data.childEmitterName)) 
                    ActivateChildEmitter(collision.gameObject, data); 
            } 

            collision.gameObject.GetComponent<FractureOnImpact>()?.Fracture(contact.point); 
        } 
        //ฅ^•ﻌ•^ฅ

        if (trailEffect != null)
        {
            transform.DetachChildren();
            trailEffect.GetComponent<TrailScript>().fadeOut(0.35f);
        }
        Destroy(gameObject);
    }

    // ฅ^•ﻌ•^ฅ
    private IEnumerator ApplyMaterialTemporary(GameObject target, ImpactData data)
    {
        MeshRenderer[] renderers = target.GetComponentsInChildren<MeshRenderer>();

        Dictionary<MeshRenderer, Material[]> originalMaterials = new Dictionary<MeshRenderer, Material[]>();

        foreach (MeshRenderer r in renderers)
        {
            originalMaterials[r] = r.sharedMaterials;

            Material[] impactMats = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < impactMats.Length; i++)
                impactMats[i] = data.impactMaterial;

            r.materials = impactMats;
        }

        yield return new WaitForSeconds(data.materialDuration);

        foreach (MeshRenderer r in renderers)
        {
            if (r == null) continue;
            r.sharedMaterials = originalMaterials[r];
        }
    }

    // ฅ^•ﻌ•^ฅ
    private void ActivateChildEmitter(GameObject target, ImpactData data)
    {
        Transform emitterTransform = target.transform.Find(data.childEmitterName);

        if (emitterTransform == null)
            emitterTransform = FindChildByName(target.transform, data.childEmitterName);

        if (emitterTransform == null)
        {
            Debug.LogWarning($"[Projectile] Emitter '{data.childEmitterName}' non trovato su {target.name}");
            return;
        }

        ParticleSystem ps = emitterTransform.GetComponent<ParticleSystem>();
        if (ps == null)
        {
            Debug.LogWarning($"[Projectile] Nessun ParticleSystem su '{data.childEmitterName}'");
            return;
        }

        emitterTransform.gameObject.SetActive(true);
        ps.Play();

        if (data.childEmitterDuration > 0f)
            StartCoroutine(StopEmitterAfterDelay(ps, emitterTransform.gameObject, data.childEmitterDuration));
    }

    // ฅ^•ﻌ•^ฅ
    private Transform FindChildByName(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform found = FindChildByName(child, name);
            if (found != null) return found;
        }
        return null;
    }

    // ฅ^•ﻌ•^ฅ
    private IEnumerator StopEmitterAfterDelay(ParticleSystem ps, GameObject emitterGO, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (ps == null) yield break;
        ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        yield return new WaitForSeconds(ps.main.startLifetime.constantMax);
        if (emitterGO != null) emitterGO.SetActive(false);
    }

    // ฅ^•ﻌ•^ฅ
    private void SpawnImpactParticle(ImpactData data, Vector3 point, Vector3 normal)
    {
        if (data.impactParticlePrefab == null) return;

        GameObject vfx = Instantiate(data.impactParticlePrefab, point, Quaternion.LookRotation(normal));

        ParticleSystem ps = vfx.GetComponent<ParticleSystem>();
        float dur = ps != null ? ps.main.duration + ps.main.startLifetime.constantMax : 3f;

        Destroy(vfx, dur);
    }

    // ฅ^•ﻌ•^ฅ
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
