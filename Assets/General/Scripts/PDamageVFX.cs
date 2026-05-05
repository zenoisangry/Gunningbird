using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class PDamageVFX : MonoBehaviour
{
    [Header("References")]
    public PlayerInput player;
    public Image damageOverlay;
    public Transform cameraTransform;
    public AudioSource audioSource;

    private HealthSystem health;

    [Header("Flash")]
    public float maxAlpha = 0.6f;
    public float fadeSpeed = 5f;

    [Header("Camera Shake")]
    public float shakeAmount = 0.1f;
    public float shakeDuration = 0.2f;

    [Header("Audio")]
    public AudioClip lightHit;
    public AudioClip heavyHit;

    private float currentAlpha;

    // Shake variables
    private Vector3 originalCamPos;
    private float shakeTimer;

    void Start()
    {
        // Cache HealthSystem safely
        if (player != null)
        {
            health = player.GetHealthSystem();

            if (health != null)
                health.DamageTaken += OnDamageTaken;
        }

        // Store original camera position
        if (cameraTransform != null)
            originalCamPos = cameraTransform.localPosition;
    }

    void OnDestroy()
    {
        if (health != null)
            health.DamageTaken -= OnDamageTaken;
    }

    void Update()
    {
        // ======================
        // FLASH FADE
        // ======================
        currentAlpha = Mathf.Lerp(currentAlpha, 0f, Time.deltaTime * fadeSpeed);

        if (damageOverlay != null)
        {
            Color c = damageOverlay.color;
            c.a = currentAlpha;
            damageOverlay.color = c;
        }

        // ======================
        // CAMERA SHAKE
        // ======================
        if (cameraTransform != null)
        {
            if (shakeTimer > 0)
            {
                shakeTimer -= Time.deltaTime;

                Vector3 shakeOffset = Random.insideUnitSphere * shakeAmount;
                shakeOffset.z = 0f; // evita effetto strano in profondità

                cameraTransform.localPosition = originalCamPos + shakeOffset;
            }
            else
            {
                cameraTransform.localPosition = originalCamPos;
            }
        }
    }

    void OnDamageTaken(float damage)
    {
        float intensity = Mathf.Clamp01(damage / 50f);

        // ======================
        // FLASH
        // ======================
        currentAlpha = Mathf.Max(currentAlpha, intensity * maxAlpha);

        // ======================
        // CAMERA SHAKE TRIGGER
        // ======================
        shakeTimer = shakeDuration * intensity;

        // ======================
        // AUDIO
        // ======================
        if (audioSource != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);

            AudioClip clip = intensity > 0.5f ? heavyHit : lightHit;

            if (clip != null)
                audioSource.PlayOneShot(clip, Mathf.Lerp(0.5f, 1f, intensity));
        }
    }
}
