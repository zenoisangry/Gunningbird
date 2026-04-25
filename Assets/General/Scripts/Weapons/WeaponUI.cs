using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponUI : MonoBehaviour
{
    [Header("UI")]
    //[SerializeField] private TMP_Text ammoText; //ฅ^•ﻌ•^ฅ sostituito dalle icone, tenuto per il simbolo ∞
    [SerializeField] private Image crosshair;
    [SerializeField] private Image weaponIconImage;
    //[SerializeField] private TMP_Text healthText; //ฅ^•ﻌ•^ฅ non serve nella modifica apportata, ma lascio per sicurezza!
    [SerializeField] private Image healthBarFill; //Meow Meow ฅ^•ﻌ•^ฅ

    [Header("Crosshair Spread")] //ฅ^•ﻌ•^ฅ
    [SerializeField] private float crosshairBaseSize = 32f;
    [SerializeField] private float crosshairMaxSize = 96f;
    [SerializeField] private float crosshairLerpSpeed = 10f;
    private RectTransform crosshairRect;
    private float targetCrosshairSize;

    [Header("Ammo Icons")] //ฅ^•ﻌ•^ฅ
    // sinistra a destra
    [SerializeField] private Image[] ammoIcons = new Image[6];
    [SerializeField] private Color ammoActiveColor = Color.white;
    [SerializeField] private Color ammoEmptyColor = new Color(1f, 1f, 1f, 0.15f);
    private int lastAmmo = -1;
    private int lastMagazineSize = -1;

    private WeaponManager weaponManager;
    private HealthSystem healthSystem;

    public void Bind(WeaponManager wm, HealthSystem hs)
    {
        weaponManager = wm;
        healthSystem = hs;

        // ฅ^•ﻌ•^ฅ 
        if (crosshair != null)
        {
            crosshairRect = crosshair.GetComponent<RectTransform>();
            targetCrosshairSize = crosshairBaseSize;
        }

        if (healthSystem != null)
        {
            healthSystem.HealthChanged += UpdateHealthUI;
            UpdateHealthUI(
                healthSystem.GetHealth(),
                healthSystem.GetMaxHealth()
            );
        }
    }

    private void OnDisable()
    {
        if (healthSystem != null)
            healthSystem.HealthChanged -= UpdateHealthUI;
    }

    private void Update()
    {
        if (weaponManager == null) return;

        BaseWeapon weapon = weaponManager.GetCurrentWeapon();
        if (weapon == null)
        {
            // if (ammoText) ammoText.gameObject.SetActive(false); //ฅ^•ﻌ•^ฅ
            SetAllIconsVisible(false); //ฅ^•ﻌ•^ฅ
            if (crosshair) crosshair.enabled = false;
            return;
        }

        WeaponData data = weapon.GetWeaponData();
        if (data == null) return;

        // if (ammoText) { if (!data.usesAmmo) { ammoText.gameObject.SetActive(false); } else { ammoText... } }
        //ฅ^•ﻌ•^ฅ
        if (!data.usesAmmo)
        {
            SetAllIconsVisible(false);
        }
        else if (data.hasInfiniteAmmo)
        {
            SetAllIconsVisible(false);
            if (ammoText)
            {
                ammoText.gameObject.SetActive(true);
                ammoText.text = "∞";
            }
        }
        else
        {
            UpdateAmmoIcons(weapon.GetCurrentAmmo(), data.magazineSize);
        }

        if (crosshair)
            crosshair.enabled = weapon.CanFire();

        UpdateCrosshairSize(weapon, data);
    }

    // ฅ^•ﻌ•^ฅ
    private void UpdateAmmoIcons(int currentAmmo, int magazineSize)
    {
        if (ammoIcons == null || ammoIcons.Length == 0) return;

        if (magazineSize != lastMagazineSize)
        {
            lastMagazineSize = magazineSize;
            lastAmmo = -1;

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
            float spreadRatio = Mathf.Clamp01(weapon.GetCurrentSpread() / data.maxSpread);
            targetCrosshairSize = Mathf.Lerp(crosshairBaseSize, crosshairMaxSize, spreadRatio);
        }

        float currentSize = crosshairRect.sizeDelta.x;
        float newSize = Mathf.Lerp(currentSize, targetCrosshairSize, Time.deltaTime * crosshairLerpSpeed);
        crosshairRect.sizeDelta = new Vector2(newSize, newSize);
    }

    private void UpdateHealthUI(float current, float max)
    {
        /* if (healthText == null) return;
         healthText.text = $"HP: {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";*/ //ฅ^•ﻌ•^ฅ lascio per sicurezza, ma non serve!

        if (healthBarFill == null) return;

        float fillValue = current / max;

        healthBarFill.fillAmount = fillValue;
        //ฅ^•ﻌ•^ฅ
    }

    public void SetWeaponIcon(Sprite icon)
    {
        if (weaponIconImage != null)
            weaponIconImage.sprite = icon;
    }
}
