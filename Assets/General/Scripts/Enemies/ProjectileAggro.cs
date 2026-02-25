using UnityEngine;

public class ProjectileAggro : MonoBehaviour
{
    bool active = true;
    private SphereCollider sCollider;
    public bool awake = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        sCollider = GetComponent<SphereCollider>();
    }
    public void Activate()
    {
        active = true;
        sCollider.enabled = true;
        awake = false;
    }

    public void Deactivate()
    {
        active = false;
        sCollider.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == 9)
        {
            awake = true;
            Deactivate();
        }
    }
}
