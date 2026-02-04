using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private Image crosshair;
    [SerializeField] private Image weaponIconImage;
    [SerializeField] private TMP_Text healthText;

    private WeaponManager weaponManager;
    private HealthSystem healthSystem;

    public void Bind(WeaponManager wm, HealthSystem hs)
    {
        weaponManager = wm;
        healthSystem = hs;

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
            if (ammoText) ammoText.gameObject.SetActive(false);
            if (crosshair) crosshair.enabled = false;
            return;
        }

        WeaponData data = weapon.GetWeaponData();
        if (data == null) return;

        if (ammoText)
        {
            if (!data.usesAmmo)
            {
                ammoText.gameObject.SetActive(false);
            }
            else
            {
                ammoText.gameObject.SetActive(true);
                ammoText.text = data.hasInfiniteAmmo
                    ? "∞"
                    : $"{weapon.GetCurrentAmmo()} / {weapon.GetReserveAmmo()}";
            }
        }

        if (crosshair)
            crosshair.enabled = weapon.CanFire();
    }

    private void UpdateHealthUI(float current, float max)
    {
        if (healthText == null) return;
        healthText.text = $"HP: {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }

    public void SetWeaponIcon(Sprite icon)
    {
        if (weaponIconImage != null)
            weaponIconImage.sprite = icon;
    }
}