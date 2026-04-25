using UnityEngine;
using System.Collections;

/// <summary>
/// Aggiungi questo script sullo stesso GameObject (o parent) che ha il tag superficie.
/// Si occupa lui di salvare i materiali originali al primo hit e ripristinarli,
/// così colpi multipli ravvicinati non corrompono mai i materiali.
/// </summary>
public class HitMaterialEffect : MonoBehaviour
{
    // I materiali originali vengono salvati in Awake, una volta sola
    // → assicurati che i MeshRenderer siano già configurati nel prefab prima del runtime
    private MeshRenderer[] _renderers;
    private Material[][] _originalMaterials;

    private Coroutine _restoreCoroutine;
    private bool _isHit = false;

    private void Awake()
    {
        _renderers = GetComponentsInChildren<MeshRenderer>();
        _originalMaterials = new Material[_renderers.Length][];

        // Salva sharedMaterials in Awake: a questo punto nessun colpo è ancora avvenuto,
        // quindi i materiali sono sicuramente quelli originali dell'asset
        for (int i = 0; i < _renderers.Length; i++)
            _originalMaterials[i] = _renderers[i].sharedMaterials;
    }

    /// <summary>
    /// Chiamato da Projectile.cs al momento dell'impatto.
    /// </summary>
    public void PlayHit(Material impactMaterial, float duration)
    {
        // Se un restore è già in attesa, lo cancella e riparte dal materiale di impatto
        // (colpi multipli ravvicinati: il timer si resetta, non si corrompono i materiali)
        if (_restoreCoroutine != null)
            StopCoroutine(_restoreCoroutine);

        ApplyImpactMaterial(impactMaterial);
        _restoreCoroutine = StartCoroutine(RestoreAfterDelay(duration));
    }

    private void ApplyImpactMaterial(Material mat)
    {
        _isHit = true;
        foreach (MeshRenderer r in _renderers)
        {
            if (r == null) continue;

            // Costruisce un array con il materiale di impatto su tutti gli slot del renderer
            Material[] impactMats = new Material[r.sharedMaterials.Length];
            for (int i = 0; i < impactMats.Length; i++)
                impactMats[i] = mat;

            // sharedMaterials qui è sicuro: stiamo sostituendo con un asset esterno,
            // non con un'istanza clonata. Il restore usa _originalMaterials salvati in Awake.
            r.sharedMaterials = impactMats;
        }
    }

    private IEnumerator RestoreAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null) continue;
            _renderers[i].sharedMaterials = _originalMaterials[i];
        }

        _isHit = false;
        _restoreCoroutine = null;
    }
}
