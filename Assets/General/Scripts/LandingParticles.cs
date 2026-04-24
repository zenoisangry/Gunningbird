using UnityEngine;

public class LandingParticles : MonoBehaviour
{
    public ParticleSystem dustPrefab;
    public PlayerInput movement;

    [Header("Fall Settings")]
    public float minFallHeight = 2f;
    public float minImpactSpeed = -8f;

    [Header("Dive Only (optional)")]
    public bool onlyFromDive = false;

    [Header("Spawn Settings")]
    public float groundOffset = 0.9f;

    private bool wasGrounded;
    private bool wasDiving;
    private float highestY;
    private float lastVerticalSpeed;

    void Start()
    {
        float startY = movement.transform.position.y;

        highestY = startY;
        wasGrounded = movement.grounded;
        wasDiving = movement.diving;
    }

    void Update()
    {
        bool isGrounded = movement.grounded;
        float currentY = movement.transform.position.y;

        // Quando è in aria
        if (!isGrounded)
        {
            lastVerticalSpeed = movement.GetComponent<Rigidbody>().linearVelocity.y;

            if (currentY > highestY)
                highestY = currentY;
        }

        // Atterraggio
        if (!wasGrounded && isGrounded)
        {
            float fallDistance = highestY - currentY;

            bool validFall = fallDistance > minFallHeight && lastVerticalSpeed < minImpactSpeed;
            bool validDive = !onlyFromDive || wasDiving;

            if (validFall && validDive)
            {
                SpawnDust();
            }

            highestY = currentY;
        }

        wasGrounded = isGrounded;
        wasDiving = movement.diving;
    }

    void SpawnDust()
    {
        Vector3 spawnPos = movement.transform.position;
        spawnPos += Vector3.down * groundOffset;

        Instantiate(dustPrefab, spawnPos, Quaternion.identity);
    }
}