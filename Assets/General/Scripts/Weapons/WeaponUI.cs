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
    [SerializeField] private RectTransform shakeTarget; // es. il root HUD o la healthbar
    [SerializeField] private float shakeBaseMagnitude = 8f;   // shake max a 0 HP
    [SerializeField] private float shakeDuration      = 0.3f;
    [SerializeField] private float shakeFrequency     = 25f;
    private bool isShaking = false;

    [Header("Reload Animation")]
    [SerializeField] private float reloadIconInterval = 0.1f; // delay tra un'icona e la successiva
    private bool  wasReloading    = false;
    private float reloadStartTime = 0f;
    private int   reloadTargetAmmo = 0;
    private Coroutine reloadCoroutine;

    private WeaponManager weaponManager;
    private HealthSystem  healthSystem;

    // ─── Bind ────────────────────────────────────────────────────────────────
    public void Bind(WeaponManager wm, HealthSystem hs)
    {
        weaponManager = wm;
        healthSystem  = hs;

        if (crosshair != null)
        {
            crosshairRect     = crosshair.GetComponent<RectTransform>();
            crosshairBaseSize = Mathf.Max(crosshairBaseSize, 16f);
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
                    {
                        Debug.Log($"[WeaponUI] Bind — ammo:{weapon.GetCurrentAmmo()}/{data.magazineSize}");
                        UpdateAmmoIcons(weapon.GetCurrentAmmo(), data.magazineSize);
                    }
                    else
                        Debug.Log($"[WeaponUI] Bind — no icons: usesAmmo:{data.usesAmmo}");
                }
            }
        }

        if (healthSystem != null)
        {
            healthSystem.HealthChanged -= OnHealthChanged;
            healthSystem.HealthChanged += OnHealthChanged;
            UpdateHealthBar(healthSystem.GetHealth(), healthSystem.GetMaxHealth());
        }

        if (SceneResetManager.Instance != null)
            UpdateEnemyCounter(SceneResetManager.Instance.GetAliveEnemyCount());
    }

    private void OnEnable()
    {
        if (SceneResetManager.Instance != null)
            SceneResetManager.Instance.EnemyDied += OnEnemyDied;

        // Forza refresh icone quando la HUD viene riattivata
        lastAmmo         = -1;
        lastMagazineSize = -1;
    }

    private void OnDisable()
    {
        if (healthSystem != null)
            healthSystem.HealthChanged -= OnHealthChanged;
        if (SceneResetManager.Instance != null)
            SceneResetManager.Instance.EnemyDied -= OnEnemyDied;
    }

    // ─── Update ──────────────────────────────────────────────────────────────
    private void Update()
    {
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
        {
            SetAllIconsVisible(false);
        }
        else
        {
            // Le icone mostrano sempre il magazine — anche con munizioni infinite
            // hasInfiniteAmmo significa riserva infinita, non magazine infinito
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

    // ─── Reload Animation ────────────────────────────────────────────────────
    private void HandleReloadAnimation(BaseWeapon weapon, WeaponData data)
    {
        bool isReloading = weapon.IsReloading();

        if (isReloading && !wasReloading)
        {
            // Reload appena iniziato
            wasReloading    = true;
            reloadStartTime = Time.time;
            reloadTargetAmmo = data.magazineSize;

            // Spegni tutte le icone
            for (int i = 0; i < ammoIcons.Length; i++)
                if (ammoIcons[i] != null && i < data.magazineSize)
                    ammoIcons[i].color = ammoEmptyColor;

            if (reloadCoroutine != null) StopCoroutine(reloadCoroutine);
            reloadCoroutine = StartCoroutine(AnimateReload(weapon, data));
        }
        else if (!isReloading && wasReloading)
        {
            // Reload finito — aggiorna forzatamente
            wasReloading     = false;
            lastAmmo         = -1;
            lastMagazineSize = -1;
            if (reloadCoroutine != null) StopCoroutine(reloadCoroutine);
        }
    }

    private IEnumerator AnimateReload(BaseWeapon weapon, WeaponData data)
    {
        int magazineSize = data.magazineSize;
        float totalTime  = data.reloadTime > 0f ? data.reloadTime : 1f;

        // Intervallo tra un'icona e la successiva
        float interval = Mathf.Min(reloadIconInterval, totalTime / Mathf.Max(magazineSize, 1));

        for (int i = 0; i < magazineSize && i < ammoIcons.Length; i++)
        {
            yield return new WaitForSeconds(interval);

            // Controlla che il reload sia ancora in corso
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
        if (wasReloading) return; // non sovrascrivere durante l'animazione

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

        float currentSize = crosshairRect.sizeDelta.x;
        float newSize     = Mathf.Lerp(currentSize, targetCrosshairSize, Time.deltaTime * crosshairLerpSpeed);
        newSize = Mathf.Max(newSize, crosshairBaseSize);
        crosshairRect.sizeDelta = new Vector2(newSize, newSize);
    }

    // ─── Health ──────────────────────────────────────────────────────────────
    private void OnHealthChanged(float current, float max)
    {
        float previousFill = healthBarFill != null ? healthBarFill.fillAmount : 1f;
        float newFill      = current / max;

        UpdateHealthBar(current, max);

        // Shake proporzionale al danno ricevuto e alla vita rimasta
        if (newFill < previousFill && shakeTarget != null && !isShaking)
        {
            float damageRatio  = (previousFill - newFill);           // quanto danno % ricevuto
            float healthRatio  = 1f - newFill;                       // quanto siamo vicini alla morte
            float magnitude    = shakeBaseMagnitude * Mathf.Lerp(0.3f, 1f, healthRatio) * Mathf.Clamp01(damageRatio * 5f);
            StartCoroutine(ShakeCoroutine(magnitude));
        }
    }

    private void UpdateHealthBar(float current, float max)
    {
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
            float t         = elapsed / shakeDuration;
            float dampened  = magnitude * (1f - t); // fade out progressivo
            float offsetX   = Mathf.Sin(elapsed * shakeFrequency) * dampened;
            float offsetY   = Mathf.Cos(elapsed * shakeFrequency * 1.3f) * dampened;

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
        if (SceneResetManager.Instance != null)
            UpdateEnemyCounter(SceneResetManager.Instance.GetAliveEnemyCount());
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
