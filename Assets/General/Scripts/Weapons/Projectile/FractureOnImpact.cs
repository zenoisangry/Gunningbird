using UnityEngine;


/// Aggiungi questo script agli oggetti distruttibili che hanno figli
/// con MeshCollider convex (generati da Convex Decomposition).
/// Viene chiamato automaticamente da Projectile.cs al momento dell'impatto.
/// // ฅ^•ﻌ•^ฅ

public class FractureOnImpact : MonoBehaviour
{
    
    [SerializeField] private float explosionForce = 300f;
    
    [SerializeField] private float explosionRadius = 1.5f;
    
    [SerializeField] private float upwardsModifier = 0.4f;
    
    [SerializeField] private float torqueAmount = 4f;
    
    [SerializeField] private float fragmentLifetime = 5f;

    private bool _fractured = false;

    public void Fracture(Vector3 impactPoint)
    {
        if (_fractured) return;
        _fractured = true;

        MeshRenderer[] fragments = GetComponentsInChildren<MeshRenderer>();

        foreach (MeshRenderer frag in fragments)
        {
            frag.transform.SetParent(null);

            Rigidbody fragRb = frag.GetComponent<Rigidbody>();
            if (fragRb == null)
                fragRb = frag.gameObject.AddComponent<Rigidbody>();

            MeshCollider mc = frag.GetComponent<MeshCollider>();
            if (mc != null) mc.convex = true;

            fragRb.AddExplosionForce(
                explosionForce,
                impactPoint,
                explosionRadius,
                upwardsModifier,
                ForceMode.Impulse
            );

            fragRb.AddTorque(Random.insideUnitSphere * torqueAmount, ForceMode.Impulse);

            Destroy(frag.gameObject, fragmentLifetime);
        }

        Destroy(gameObject);
    }
}
