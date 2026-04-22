using UnityEngine;

public class LandingParticles : MonoBehaviour
{
    public ParticleSystem Dust;
    public PlayerInput movement;
    public bool count;
    void Update()
    {
        if (movement.grounded)
        {
            Dust.Play();
            count = true;
        }

        if (count = true)
        {
            Dust.Stop();
        }
    }

    
}
