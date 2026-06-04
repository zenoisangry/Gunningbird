using System.Collections;
using UnityEngine;

public class EnemyDeath : MonoBehaviour
{
    [SerializeField] private float fadeDuration = 5f;

    private static readonly int FadeAmount = Shader.PropertyToID("_FadeAmount");

    public void Die()
    {
        StartCoroutine(FadeAndDestroy());
    }

    private IEnumerator FadeAndDestroy()
    {
        MeshRenderer meshRenderer = GetComponentInChildren<MeshRenderer>();
        if (meshRenderer == null)
        {
            Debug.LogError($"[EnemyDeath] Nessun MeshRenderer trovato nei children di {gameObject.name}");
            yield break;
        }

        Material mat = meshRenderer.materials[0];
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            mat.SetFloat(FadeAmount, Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }

        mat.SetFloat(FadeAmount, 1f);
        Destroy(gameObject);
    }
}