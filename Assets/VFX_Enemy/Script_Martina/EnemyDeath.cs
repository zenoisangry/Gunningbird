using System.Collections;
using UnityEngine;

public class EnemyDeath : MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 5f;

    private static readonly int FadeAmount = Shader.PropertyToID("_FadeAmount");

    private HealthSystem healthSystem;
    private Renderer enemyRenderer;
    private Material material;
    private bool isDying;


    private void Awake()
    {
        // Cerca HealthSystem nel parent (Feral_Colonist_Rigged)
        healthSystem = GetComponentInParent<HealthSystem>();

        if (healthSystem == null)
        {
            Debug.LogError("[EnemyDeath] Nessun HealthSystem trovato nei parent di " + gameObject.name);
        }
        else
        {
            Debug.Log("[EnemyDeath] HealthSystem trovato su: " + healthSystem.gameObject.name);
        }


        // Cerca il renderer della mesh
        enemyRenderer = GetComponent<Renderer>();

        if (enemyRenderer == null)
        {
            enemyRenderer = GetComponentInChildren<Renderer>();
        }

        if (enemyRenderer == null)
        {
            Debug.LogError("[EnemyDeath] Nessun Renderer trovato su " + gameObject.name);
        }
        else
        {
            Debug.Log("[EnemyDeath] Renderer trovato: " + enemyRenderer.name);
        }
    }


    private void OnEnable()
    {
        if (healthSystem != null)
        {
            healthSystem.Died += Die;
            Debug.Log("[EnemyDeath] Collegato all'evento Died");
        }
    }


    private void OnDisable()
    {
        if (healthSystem != null)
        {
            healthSystem.Died -= Die;
        }
    }


    public void Die()
    {
        if (isDying)
            return;

        isDying = true;

        Debug.Log("[EnemyDeath] Avvio dissolvenza su " + gameObject.name);

        StartCoroutine(FadeAndDestroy());
    }


    private IEnumerator FadeAndDestroy()
    {
        if (enemyRenderer == null)
        {
            Debug.LogError("[EnemyDeath] Renderer mancante, distruzione immediata");
            Destroy(transform.root.gameObject);
            yield break;
        }


        // Crea un'istanza del materiale solo per questo nemico
        material = enemyRenderer.material;


        if (!material.HasProperty(FadeAmount))
        {
            Debug.LogError(
                "[EnemyDeath] Il materiale " + material.name +
                " non contiene _FadeAmount"
            );

            Destroy(transform.root.gameObject);
            yield break;
        }


        float elapsed = 0f;


        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            float value = Mathf.Clamp01(elapsed / fadeDuration);

            material.SetFloat(FadeAmount, value);

            yield return null;
        }


        material.SetFloat(FadeAmount, 1f);

        Debug.Log("[EnemyDeath] Dissolvenza completata");


        // Distrugge tutto il nemico, non solo la mesh
        Destroy(transform.root.gameObject);
    }
}