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

    [Header("Flash")]
    public float maxAlpha = 0.6f;
    public float fadeSpeed = 5f;

    [Header("Camera Shake")]
    public float shakeAmount = 0.1f;

    [Header("Audio")]
    public AudioClip lightHit;
    public AudioClip heavyHit;

    private float currentAlpha;

    void Start()
    {
        player.GetHealthSystem().DamageTaken += OnDamageTaken;
    }

    void OnDestroy()
    {
        if (player != null && player.GetHealthSystem() != null)
            player.GetHealthSystem().DamageTaken -= OnDamageTaken;
    }

    void Update()
    {
        // fade del flash rosso
        currentAlpha = Mathf.Lerp(currentAlpha, 0f, Time.deltaTime * fadeSpeed);

        if (damageOverlay != null)
        {
            Color c = damageOverlay.color;
            c.a = currentAlpha;
            damageOverlay.color = c;
        }
    }

    void OnDamageTaken(float damage)
    {
        float intensity = Mathf.Clamp01(damage / 50f);

        //FLASH
        currentAlpha = Mathf.Max(currentAlpha, intensity * maxAlpha);

        //CAMERA SHAKE
        if (cameraTransform != null)
        {
            cameraTransform.localPosition += Random.insideUnitSphere * shakeAmount * intensity;
        }

        //AUDIO
        if (audioSource != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);

            AudioClip clip = intensity > 0.5f ? heavyHit : lightHit;

            if (clip != null)
                audioSource.PlayOneShot(clip, Mathf.Lerp(0.5f, 1f, intensity));
        }
    }
}
