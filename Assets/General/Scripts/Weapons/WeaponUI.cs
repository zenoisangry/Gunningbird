using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image    crosshair;
    [SerializeField] private Image    weaponIconImage;
    [SerializeField] private Image    healthBarFill;

    [Header("Enemy Counter")] // ฅ^•ﻌ•^ฅ
    [SerializeField] private TMP_Text enemyCounterText;
    [SerializeField] private string   enemyCounterFormat = "{0} nemici rimanenti";

    [Header("Crosshair Spread")] // ฅ^•ﻌ•^ฅ
    [SerializeField] private float crosshairBaseSize  = 32f;
    [SerializeField] private float crosshairMaxSize   = 96f;
    [SerializeField] private float crosshairLerpSpeed = 10f;
    private RectTransform crosshairRect;
    private float         targetCrosshairSize;

    [Header("Ammo Icons")] // ฅ^•ﻌ•^ฅ
    [SerializeField] private Image[] ammoIcons      = new Image[6];
    [SerializeField] private Color   ammoActiveColor = Color.white;
    [SerializeField] private Color   ammoEmptyColor  = new Color(1f, 1f, 1f, 0.15f);
    private int lastAmmo        = -1;
    private int lastMagazineSize = -1;

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
            targetCrosshairSize = crosshairBaseSize;
        }

        if (healthSystem != null)
        {
            healthSystem.HealthChanged -= UpdateHealthUI;
            healthSystem.HealthChanged += UpdateHealthUI;
            UpdateHealthUI(healthSystem.GetHealth(), healthSystem.GetMaxHealth());
        }

        // Inizializza il counter con il valore corrente dal SceneResetManager
        if (SceneResetManager.Instance != null)
            UpdateEnemyCounter(SceneResetManager.Instance.GetAliveEnemyCount());
    }

    private void OnEnable()
    {
        // Agganciati all'evento ogni volta che la HUD viene riattivata (es. dopo pausa)
        if (SceneResetManager.Instance != null)
            SceneResetManager.Instance.OnEnemyDied += OnEnemyDied;
    }

    private void OnDisable()
    {
        if (healthSystem != null)
            healthSystem.HealthChanged -= UpdateHealthUI;

        if (SceneResetManager.Instance != null)
            SceneResetManager.Instance.OnEnemyDied -= OnEnemyDied;
    }

    // ─── Enemy Counter ───────────────────────────────────────────────────────

    // ฅ^•ﻌ•^ฅ — chiamato dall'evento OnEnemyDied di SceneResetManager
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
        else if (data.hasInfiniteAmmo)
        {
            SetAllIconsVisible(false);
        }
        else
        {
            UpdateAmmoIcons(weapon.GetCurrentAmmo(), data.magazineSize);
        }

        if (crosshair)
            crosshair.enabled = weapon.CanFire();

        UpdateCrosshairSize(weapon, data);
    }

    // ─── Ammo Icons ──────────────────────────────────────────────────────────

    // ฅ^•ﻌ•^ฅ
    private void UpdateAmmoIcons(int currentAmmo, int magazineSize)
    {
        if (ammoIcons == null || ammoIcons.Length == 0) return;

        if (magazineSize != lastMagazineSize)
        {
            lastMagazineSize = magazineSize;
            lastAmmo         = -1;

            for (int i = 0; i < ammoIcons.Length; i++)
            {
                if (ammoIcons[i] == null) continue;
                ammoIcons[i].gameObject.SetActive(i < magazineSize);
            }
        }

        if (currentAmmo == lastAmmo) return;
        lastAmmo = currentAmmo;

        for (int i = 0; i < magazineSize && i < ammoIcons.Length; i++)
        {
            if (ammoIcons[i] == null) continue;
            ammoIcons[i].color = i < currentAmmo ? ammoActiveColor : ammoEmptyColor;
        }
    }

    // ฅ^•ﻌ•^ฅ
    private void SetAllIconsVisible(bool visible)
    {
        if (ammoIcons == null) return;
        foreach (Image icon in ammoIcons)
            if (icon != null) icon.gameObject.SetActive(visible);
    }

    // ─── Crosshair ───────────────────────────────────────────────────────────

    // ฅ^•ﻌ•^ฅ
    private void UpdateCrosshairSize(BaseWeapon weapon, WeaponData data)
    {
        if (crosshairRect == null) return;

        if (data.maxSpread <= 0f)
        {
            targetCrosshairSize = crosshairBaseSize;
        }
        else
        {
            float spreadRatio   = Mathf.Clamp01(weapon.GetCurrentSpread() / data.maxSpread);
            targetCrosshairSize = Mathf.Lerp(crosshairBaseSize, crosshairMaxSize, spreadRatio);
        }

        float currentSize = crosshairRect.sizeDelta.x;
        float newSize     = Mathf.Lerp(currentSize, targetCrosshairSize, Time.deltaTime * crosshairLerpSpeed);
        crosshairRect.sizeDelta = new Vector2(newSize, newSize);
    }

    // ─── Health ──────────────────────────────────────────────────────────────
    private void UpdateHealthUI(float current, float max)
    {
        if (healthBarFill == null) return;
        healthBarFill.fillAmount = current / max;
    }

    // ─── Weapon Icon ─────────────────────────────────────────────────────────
    public void SetWeaponIcon(Sprite icon)
    {
        if (weaponIconImage != null)
            weaponIconImage.sprite = icon;
    }
}
