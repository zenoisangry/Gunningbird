using UnityEngine;

public class FractureOnImpact : MonoBehaviour
{
    // <(= O . O =)> fat cat!
    [Header("Fracture Settings")]
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
            Transform fragTransform = frag.transform;

            
            fragTransform.SetParent(null);

            
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
