using System.Collections;
using UnityEngine;

public class EnemyDissolve : MonoBehaviour
{
    [SerializeField] private float fadeDuration = 5f;

    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    public void StartDissolve()
    {
         Debug.Log("[EnemyDissolve] StartDissolve chiamato!");
        StartCoroutine(DissolveCoroutine());
    }

    private IEnumerator DissolveCoroutine()
    {
        SkinnedMeshRenderer meshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        if (meshRenderer == null)
        {
            Debug.LogError($"[EnemyDissolve] Nessun SkinnedMeshRenderer trovato in {gameObject.name}");
            yield break;
        }

        Material mat = meshRenderer.materials[0];
        Color originalColor = mat.GetColor(BaseColor);
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            mat.SetColor(BaseColor, new Color(originalColor.r, originalColor.g, originalColor.b, alpha));
            yield return null;
        }

        mat.SetColor(BaseColor, new Color(originalColor.r, originalColor.g, originalColor.b, 0f));
    }
}