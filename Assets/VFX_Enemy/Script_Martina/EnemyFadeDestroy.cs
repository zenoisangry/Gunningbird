using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

public class EnemyFadeDestroy : MonoBehaviour
{
    [SerializeField] private float fadeDuration = 5f;
    [SerializeField] private float deathVFXDuration = 1.5f;

    [Header("VFX Prefabs")]
    [SerializeField] private VisualEffectAsset bloodVFX;
    [SerializeField] private VisualEffectAsset damageVFX;
    [SerializeField] private VisualEffectAsset deathVFX;

    private HealthSystem healthSystem;
    private EnemyDissolve enemyDissolve;

    private void Awake()
    {
        healthSystem = GetComponentInChildren<HealthSystem>();
        enemyDissolve = GetComponent<EnemyDissolve>();

        Debug.Log($"[EnemyFadeDestroy] HealthSystem trovato: {healthSystem != null}");
        Debug.Log($"[EnemyFadeDestroy] EnemyDissolve trovato: {enemyDissolve != null}");
    }

    private void Start()
    {
        if (healthSystem != null)
        {
            healthSystem.Died += OnDied;
            healthSystem.DamageTaken += OnDamageTaken;
        }
        else
        {
            Debug.LogWarning($"[EnemyFadeDestroy] No HealthSystem found on {gameObject.name}");
        }
    }

    private void OnDestroy()
    {
        if (healthSystem != null)
        {
            healthSystem.Died -= OnDied;
            healthSystem.DamageTaken -= OnDamageTaken;
        }
    }

    private void SpawnVFX(VisualEffectAsset asset, float destroyAfter)
    {
        if (asset == null) return;

        GameObject go = new GameObject($"VFX_{asset.name}");
        go.transform.SetPositionAndRotation(transform.position, transform.rotation);

        VisualEffect vfx = go.AddComponent<VisualEffect>();
        vfx.visualEffectAsset = asset;
        vfx.Play();

        Destroy(go, destroyAfter);
    }

    private void OnDamageTaken(float damage)
    {
        SpawnVFX(damageVFX, 3f);
        SpawnVFX(bloodVFX, 3f);
    }

    private void OnDied()
    {
        StartCoroutine(FadeAndDestroy());
    }

    private IEnumerator FadeAndDestroy()
    {

        Rigidbody rb = GetComponentInChildren<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        // Fase 1: spawna il VFX e aspetta
        SpawnVFX(deathVFX, deathVFXDuration + fadeDuration);
        yield return new WaitForSeconds(deathVFXDuration);

        Debug.Log("[EnemyFadeDestroy] Dopo delay VFX!");

        // Fase 2: avvia il dissolve
        if (enemyDissolve != null)
        {
            Debug.Log("[EnemyFadeDestroy] Chiamo StartDissolve!");
            enemyDissolve.StartDissolve();
        }
        else
        {
            Debug.LogWarning("[EnemyFadeDestroy] Nessun EnemyDissolve trovato!");
        }

        // Fase 3: aspetta la fine del dissolve e distruggi
        yield return new WaitForSeconds(fadeDuration);
        Debug.Log("[EnemyFadeDestroy] Distruggo il nemico!");
        Destroy(gameObject);
    }
}