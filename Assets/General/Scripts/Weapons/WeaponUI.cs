using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponUI : MonoBehaviour
{
    [Header("Weapon References")]
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private Image crosshair;
    [SerializeField] private Image weaponIconImage;

    [Header("Player Health")]
    [SerializeField] private HealthSystem playerHealth;
    [SerializeField] private TMP_Text healthText;

    private void Awake()
    {
        if (playerHealth == null)
        {
            PlayerInput player = Object.FindAnyObjectByType<PlayerInput>();
            if (player != null)
                playerHealth = player.GetHealthSystem();
        }
    }

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.HealthChanged += UpdateHealthUI;
            UpdateHealthUI(playerHealth.GetHealth(), playerHealth.GetMaxHealth());
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.HealthChanged -= UpdateHealthUI;
    }

    private void Update()
    {
        if (weaponManager == null) return;

        BaseWeapon weapon = weaponManager.GetCurrentWeapon();
        if (weapon == null)
        {
            if (ammoText != null)
                ammoText.gameObject.SetActive(false);
            if (crosshair != null)
                crosshair.enabled = false;
            return;
        }

        WeaponData data = weapon.GetWeaponData();
        if (data == null) return;

        if (ammoText != null)
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

        if (crosshair != null)
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