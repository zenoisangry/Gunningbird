using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class WeaponUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image    crosshair;
    [SerializeField] private Image    weaponIconImage;
    [SerializeField] private Image    healthBarFill;

    [Header("Enemy Counter")]
    [SerializeField] private TMP_Text enemyCounterText;
    [SerializeField] private string   enemyCounterFormat = "{0} nemici rimanenti";

    [Header("Crosshair Spread")]
    [SerializeField] private float crosshairBaseSize  = 32f;
    [SerializeField] private float crosshairMaxSize   = 96f;
    [SerializeField] private float crosshairLerpSpeed = 10f;
    private RectTransform crosshairRect;
    private float         targetCrosshairSize;

    [Header("Ammo Icons")]
    [SerializeField] private Image[] ammoIcons      = new Image[6];
    [SerializeField] private Color   ammoActiveColor = Color.white;
    [SerializeField] private Color   ammoEmptyColor  = new Color(1f, 1f, 1f, 0.15f);
    private int lastAmmo         = -1;
    private int lastMagazineSize = -1;

    [Header("Health Shake")]
    [SerializeField] private RectTransform shakeTarget;
    [SerializeField] private float shakeBaseMagnitude = 8f;
    [SerializeField] private float shakeDuration      = 0.3f;
    [SerializeField] private float shakeFrequency     = 25f;
    private bool isShaking = false;

    [Header("Reload Animation")]
    [SerializeField] private float reloadIconInterval = 0.1f;
    private bool  wasReloading   = false;
    private Coroutine reloadCoroutine;

    [Header("Damage VFX")]
    [SerializeField] private Image      damageOverlay;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip  lightHit;
    [SerializeField] private AudioClip  heavyHit;
    [SerializeField] private float      maxOverlayAlpha  = 0.6f;
    [SerializeField] private float      overlayFadeSpeed = 5f;
    [SerializeField] private float      camShakeAmount   = 0.1f;
    [SerializeField] private float      camShakeDuration = 0.2f;
    [SerializeField] private float      lowHealthThreshold = 0.25f;  // sotto il 25% pulsa
    [SerializeField] private float      pulseSpeed         = 2f;
    [SerializeField] private float      pulseMinAlpha      = 0.05f;
    [SerializeField] private float      pulseMaxAlpha      = 0.3f;
    private float     currentOverlayAlpha;
    private float     currentHealth;
    private float     maxHealth;
    private Transform cameraTransform;
    private Vector3   originalCamPos;
    private float     camShakeTimer;

    private WeaponManager weaponManager;
    private HealthSystem  healthSystem;

    // ─── Bind ────────────────────────────────────────────────────────────────
    public void Bind(WeaponManager wm, HealthSystem hs)
    {
        weaponManager = wm;
        healthSystem  = hs;

        if (crosshair != null)
        {
            crosshairRect       = crosshair.GetComponent<RectTransform>();
            crosshairBaseSize   = Mathf.Max(crosshairBaseSize, 16f);
            targetCrosshairSize = crosshairBaseSize;
            if (crosshairRect != null)
                crosshairRect.sizeDelta = new Vector2(crosshairBaseSize, crosshairBaseSize);
        }

        lastAmmo         = -1;
        lastMagazineSize = -1;

        if (weaponManager != null)
        {
            BaseWeapon weapon = weaponManager.GetCurrentWeapon();
            if (weapon != null)
            {
                WeaponData data = weapon.GetWeaponData();
                if (data != null)
                {
                    SetWeaponIcon(data.weaponIcon);
                    if (data.usesAmmo)
                        UpdateAmmoIcons(weapon.GetCurrentAmmo(), data.magazineSize);
                }
            }
        }

        if (healthSystem != null)
        {
            healthSystem.HealthChanged -= OnHealthChanged;
            healthSystem.HealthChanged += OnHealthChanged;
            healthSystem.DamageTaken   -= OnDamageTaken;
            healthSystem.DamageTaken   += OnDamageTaken;
            UpdateHealthBar(healthSystem.GetHealth(), healthSystem.GetMaxHealth());
        }

        // Camera per damage VFX - auto-find, nessun riferimento serializzato
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            cameraTransform = mainCam.transform;
            originalCamPos  = cameraTransform.localPosition;
        }

        HookEnemyCounter();
    }

    private void HookEnemyCounter()
    {
        if (WinConditionTracker.Instance != null)
        {
            WinConditionTracker.Instance.EnemyDied -= OnEnemyDied;
            WinConditionTracker.Instance.EnemyDied += OnEnemyDied;
            UpdateEnemyCounter(WinConditionTracker.Instance.GetAliveEnemyCount());
        }
        else
        {
            StartCoroutine(HookCounterNextFrame());
        }
    }

    private IEnumerator HookCounterNextFrame()
    {
        yield return null;
        if (WinConditionTracker.Instance != null)
        {
            WinConditionTracker.Instance.EnemyDied -= OnEnemyDied;
            WinConditionTracker.Instance.EnemyDied += OnEnemyDied;
            UpdateEnemyCounter(WinConditionTracker.Instance.GetAliveEnemyCount());
        }
    }

    private void OnEnable()
    {
        lastAmmo         = -1;
        lastMagazineSize = -1;

        if (WinConditionTracker.Instance != null)
        {
            WinConditionTracker.Instance.EnemyDied -= OnEnemyDied;
            WinConditionTracker.Instance.EnemyDied += OnEnemyDied;
        }
    }

    private void OnDisable()
    {
        if (healthSystem != null)
        {
            healthSystem.HealthChanged -= OnHealthChanged;
            healthSystem.DamageTaken   -= OnDamageTaken;
        }
        if (WinConditionTracker.Instance != null)
            WinConditionTracker.Instance.EnemyDied -= OnEnemyDied;
    }

    // ─── Update ──────────────────────────────────────────────────────────────
    private void Update()
    {
        UpdateDamageVFX();

        if (weaponManager == null) return;

        BaseWeapon weapon = weaponManager.GetCurrentWeapon();
        if (weapon == null)
        {
            SetAllIconsVisible(false);
            if (crosshair) crosshair.enabled = false;
            return;
        }

        WeaponData data = weapon.GetWeaponData();
        if (data == null) return;

        if (!data.usesAmmo)
            SetAllIconsVisible(false);
        else
        {
            HandleReloadAnimation(weapon, data);
            UpdateAmmoIcons(weapon.GetCurrentAmmo(), data.magazineSize);
        }

        if (crosshair)
        {
            bool hasAmmo      = weapon.GetCurrentAmmo() > 0 || data.hasInfiniteAmmo || !data.usesAmmo;
            bool notReloading = !weapon.IsReloading();
            crosshair.enabled = hasAmmo && notReloading;
        }

        UpdateCrosshairSize(weapon, data);
    }

    // ─── Damage VFX ──────────────────────────────────────────────────────────
    private void UpdateDamageVFX()
    {
        bool isLowHealth = maxHealth > 0f && (currentHealth / maxHealth) <= lowHealthThreshold;

        if (isLowHealth && damageOverlay != null)
        {
            // Pulsazione continua sotto il 25%
            float pulse = (Mathf.Sin(Time.time * pulseSpeed * Mathf.PI) + 1f) * 0.5f;
            float pulseAlpha = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha, pulse);
            // Prende il massimo tra danno ricevuto e pulsazione
            currentOverlayAlpha = Mathf.Lerp(currentOverlayAlpha, 0f, Time.deltaTime * overlayFadeSpeed);
            float finalAlpha = Mathf.Max(currentOverlayAlpha, pulseAlpha);
            Color c = damageOverlay.color;
            c.a = finalAlpha;
            damageOverlay.color = c;
        }
        else
        {
            currentOverlayAlpha = Mathf.Lerp(currentOverlayAlpha, 0f, Time.deltaTime * overlayFadeSpeed);
            if (damageOverlay != null)
            {
                Color c = damageOverlay.color;
                c.a = currentOverlayAlpha;
                damageOverlay.color = c;
            }
        }

        if (cameraTransform != null)
        {
            if (camShakeTimer > 0f)
            {
                camShakeTimer -= Time.deltaTime;
                Vector3 offset = Random.insideUnitSphere * camShakeAmount;
                offset.z = 0f;
                cameraTransform.localPosition = originalCamPos + offset;
            }
            else
            {
                cameraTransform.localPosition = originalCamPos;
            }
        }
    }

    private void OnDamageTaken(float damage)
    {
        float intensity     = Mathf.Clamp01(damage / 50f);
        currentOverlayAlpha = Mathf.Max(currentOverlayAlpha, intensity * maxOverlayAlpha);
        camShakeTimer       = camShakeDuration * intensity;

        if (audioSource != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            AudioClip clip = intensity > 0.5f ? heavyHit : lightHit;
            if (clip != null)
                audioSource.PlayOneShot(clip, Mathf.Lerp(0.5f, 1f, intensity));
        }
    }

    // ─── Reload Animation ────────────────────────────────────────────────────
    private void HandleReloadAnimation(BaseWeapon weapon, WeaponData data)
    {
        bool isReloading = weapon.IsReloading();

        if (isReloading && !wasReloading)
        {
            wasReloading = true;
            for (int i = 0; i < ammoIcons.Length; i++)
                if (ammoIcons[i] != null && i < data.magazineSize)
                    ammoIcons[i].color = ammoEmptyColor;

            if (reloadCoroutine != null) StopCoroutine(reloadCoroutine);
            reloadCoroutine = StartCoroutine(AnimateReload(weapon, data));
        }
        else if (!isReloading && wasReloading)
        {
            wasReloading     = false;
            lastAmmo         = -1;
            lastMagazineSize = -1;
            if (reloadCoroutine != null) StopCoroutine(reloadCoroutine);
        }
    }

    private IEnumerator AnimateReload(BaseWeapon weapon, WeaponData data)
    {
        int   magazineSize = data.magazineSize;
        float totalTime    = data.reloadTime > 0f ? data.reloadTime : 1f;
        float interval     = Mathf.Min(reloadIconInterval, totalTime / Mathf.Max(magazineSize, 1));

        for (int i = 0; i < magazineSize && i < ammoIcons.Length; i++)
        {
            yield return new WaitForSeconds(interval);
            if (weapon == null || !weapon.IsReloading()) break;
            if (ammoIcons[i] != null)
                ammoIcons[i].color = ammoActiveColor;
        }

        reloadCoroutine = null;
    }

    // ─── Ammo Icons ──────────────────────────────────────────────────────────
    private void UpdateAmmoIcons(int currentAmmo, int magazineSize)
    {
        if (ammoIcons == null || ammoIcons.Length == 0) return;
        if (wasReloading) return;

        if (magazineSize != lastMagazineSize)
        {
            lastMagazineSize = magazineSize;
            lastAmmo         = -1;
            for (int i = 0; i < ammoIcons.Length; i++)
                if (ammoIcons[i] != null)
                    ammoIcons[i].gameObject.SetActive(i < magazineSize);
        }

        if (currentAmmo == lastAmmo) return;
        lastAmmo = currentAmmo;

        for (int i = 0; i < magazineSize && i < ammoIcons.Length; i++)
        {
            if (ammoIcons[i] == null) continue;
            ammoIcons[i].color = i < currentAmmo ? ammoActiveColor : ammoEmptyColor;
        }
    }

    private void SetAllIconsVisible(bool visible)
    {
        if (ammoIcons == null) return;
        foreach (Image icon in ammoIcons)
            if (icon != null) icon.gameObject.SetActive(visible);
    }

    // ─── Crosshair ───────────────────────────────────────────────────────────
    private void UpdateCrosshairSize(BaseWeapon weapon, WeaponData data)
    {
        if (crosshairRect == null) return;

        if (data.maxSpread <= 0f)
            targetCrosshairSize = crosshairBaseSize;
        else
        {
            float spreadRatio   = Mathf.Clamp01(weapon.GetCurrentSpread() / data.maxSpread);
            targetCrosshairSize = Mathf.Lerp(crosshairBaseSize, crosshairMaxSize, spreadRatio);
        }

        targetCrosshairSize = Mathf.Max(targetCrosshairSize, crosshairBaseSize);
        float currentSize   = crosshairRect.sizeDelta.x;
        float newSize       = Mathf.Lerp(currentSize, targetCrosshairSize, Time.deltaTime * crosshairLerpSpeed);
        newSize = Mathf.Max(newSize, crosshairBaseSize);
        crosshairRect.sizeDelta = new Vector2(newSize, newSize);
    }

    // ─── Health ──────────────────────────────────────────────────────────────
    private void OnHealthChanged(float current, float max)
    {
        float previousFill = healthBarFill != null ? healthBarFill.fillAmount : 1f;
        float newFill      = current / max;

        currentHealth = current;
        maxHealth     = max;

        UpdateHealthBar(current, max);

        if (newFill < previousFill && shakeTarget != null && !isShaking)
        {
            float damageRatio = previousFill - newFill;
            float healthRatio = 1f - newFill;
            float magnitude   = shakeBaseMagnitude * Mathf.Lerp(0.3f, 1f, healthRatio) * Mathf.Clamp01(damageRatio * 5f);
            StartCoroutine(ShakeCoroutine(magnitude));
        }
    }

    private void UpdateHealthBar(float current, float max)
    {
        currentHealth = current;
        maxHealth     = max;
        if (healthBarFill == null) return;
        healthBarFill.fillAmount = current / max;
    }

    private IEnumerator ShakeCoroutine(float magnitude)
    {
        isShaking = true;
        Vector3 originalPos = shakeTarget.localPosition;
        float   elapsed     = 0f;

        while (elapsed < shakeDuration)
        {
            float t        = elapsed / shakeDuration;
            float dampened = magnitude * (1f - t);
            float offsetX  = Mathf.Sin(elapsed * shakeFrequency) * dampened;
            float offsetY  = Mathf.Cos(elapsed * shakeFrequency * 1.3f) * dampened;
            shakeTarget.localPosition = originalPos + new Vector3(offsetX, offsetY, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        shakeTarget.localPosition = originalPos;
        isShaking = false;
    }

    // ─── Enemy Counter ───────────────────────────────────────────────────────
    private void OnEnemyDied()
    {
        if (WinConditionTracker.Instance != null)
            UpdateEnemyCounter(WinConditionTracker.Instance.GetAliveEnemyCount());
    }

    public void UpdateEnemyCounter(int remaining)
    {
        if (enemyCounterText == null) return;
        enemyCounterText.text = string.Format(enemyCounterFormat, remaining);
    }

    // ─── Weapon Icon ─────────────────────────────────────────────────────────
    public void SetWeaponIcon(Sprite icon)
    {
        if (weaponIconImage != null)
            weaponIconImage.sprite = icon;
    }
}
