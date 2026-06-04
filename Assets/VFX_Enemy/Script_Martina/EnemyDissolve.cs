using System.Collections;
using UnityEngine;

public class EnemyDissolve : MonoBehaviour
{
    [SerializeField] private float fadeDuration = 5f;

    private static readonly int FadeAmount = Shader.PropertyToID("_FadeAmount");

    private void Start()
    {
        StartCoroutine(FadeAndDestroy());
    }

    private IEnumerator FadeAndDestroy()
    {
        SkinnedMeshRenderer meshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        if (meshRenderer == null)
        {
            Debug.LogError($"[EnemyFadeDestroy] Nessun SkinnedMeshRenderer trovato nei children di {gameObject.name}");
            yield break;
        }

        Material mat = meshRenderer.materials[1];
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            mat.SetFloat(FadeAmount, t);
            yield return null;
        }

        mat.SetFloat(FadeAmount, 1f);
        Destroy(gameObject);
    }
}