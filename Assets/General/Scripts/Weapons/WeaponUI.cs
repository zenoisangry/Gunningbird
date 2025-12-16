using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponUI : MonoBehaviour
{
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private Image crosshair;

    private void Update()
    {
        BaseWeapon weapon = weaponManager.GetCurrentWeapon();
        if (!weapon) return;

        WeaponData data = weapon.GetWeaponData();

        ammoText.text = data.hasInfiniteAmmo
            ? "∞"
            : $"{weapon.GetCurrentAmmo()} / {weapon.GetReserveAmmo()}";

        crosshair.enabled = weapon.CanFire();
    }
}