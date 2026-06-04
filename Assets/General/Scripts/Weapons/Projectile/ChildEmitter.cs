using System.Collections;
using UnityEngine;

/// <summary>
/// Componente aggiunto runtime sul target colpito.
/// Gestisce autonomamente la coroutine di stop del ParticleSystem,
/// così non dipende dal ciclo di vita del Projectile (che viene distrutto subito dopo l'impatto).
/// </summary>
[DisallowMultipleComponent]
public class ChildEmitter : MonoBehaviour
{
    /// <summary>
    /// Trova (o crea) il ChildEmitter sul target, poi avvia l'emitter.
    /// Chiamato da Projectile invece di StartCoroutine locale.
    /// </summary>
    public static void Activate(GameObject target, string emitterName, float duration)
    {
        // Trova il ParticleSystem
        Transform emitterTransform = FindChildByName(target.transform, emitterName);

        if (emitterTransform == null)
        {
            Debug.LogWarning($"[ChildEmitter] Emitter '{emitterName}' non trovato su {target.name}");
            return;
        }

        ParticleSystem ps = emitterTransform.GetComponent<ParticleSystem>();
        if (ps == null)
        {
            Debug.LogWarning($"[ChildEmitter] Nessun ParticleSystem su '{emitterName}'");
            return;
        }

        // Ottieni o aggiungi il componente sul target (non sul proiettile)
        ChildEmitter ce = target.GetComponent<ChildEmitter>();
        if (ce == null)
            ce = target.AddComponent<ChildEmitter>();

        ce.Run(ps, emitterTransform.gameObject, duration);
    }

    /// <summary>Avvia l'emitter e schedula lo stop. Se era già in esecuzione lo ferma prima.</summary>
    public void Run(ParticleSystem ps, GameObject emitterGO, float duration)
    {
        StopAllCoroutines();

        emitterGO.SetActive(true);
        ps.Play();

        if (duration > 0f)
            StartCoroutine(StopAfterDelay(ps, emitterGO, duration));
    }

    private IEnumerator StopAfterDelay(ParticleSystem ps, GameObject emitterGO, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (ps == null) yield break;
        ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);

        // Aspetta che le particelle già emesse finiscano il loro lifetime
        yield return new WaitForSeconds(ps.main.startLifetime.constantMax);

        if (emitterGO != null)
            emitterGO.SetActive(false);

        // Rimuovi il componente dal target quando ha finito
        Destroy(this);
    }

    private static Transform FindChildByName(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform found = FindChildByName(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
