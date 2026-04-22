using UnityEngine;

public class LandingParticles : MonoBehaviour
{
    public ParticleSystem dustPrefab;
    public PlayerInput movement;

    public float minFallHeight = 2f;

    private bool wasGrounded;
    private float highestY;

    void Update()
    {
        bool isGrounded = movement.grounded;
        float currentY = movement.transform.position.y;

        if (!isGrounded)
        {
            if (currentY > highestY)
                highestY = currentY;
        }

        if (!wasGrounded && isGrounded)
        {
            float fallDistance = highestY - currentY;

            if (fallDistance > minFallHeight)
            {
                SpawnDust();
            }

            highestY = currentY;
        }

        wasGrounded = isGrounded;
    }

    void SpawnDust()
    {
        Vector3 spawnPos = movement.transform.position;

        // leggermente sotto i piedi
        spawnPos += Vector3.down * .9f;

        Instantiate(dustPrefab, spawnPos, Quaternion.identity);
    }
}