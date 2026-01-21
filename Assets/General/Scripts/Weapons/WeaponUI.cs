using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private Image crosshair;
    [SerializeField] private Image weaponIconImage;

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

    public void SetWeaponIcon(Sprite icon)
    {
        if (weaponIconImage != null)
            weaponIconImage.sprite = icon;
    }
}