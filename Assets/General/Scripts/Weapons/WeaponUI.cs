using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class WeaponUI : MonoBehaviour
{
    [Header("Crosshair")]
    [SerializeField] protected Image crosshairImage;
    [SerializeField] protected Color defaultCrosshairColor = Color.white;
    [SerializeField] protected Color aimingCrosshairColor = Color.red;
    [SerializeField] protected float crosshairSize = 20f;
    [SerializeField] protected bool dynamicCrosshair = true;

    [Header("Ammo Display")]
    [SerializeField] protected TextMeshProUGUI currentAmmoText;
    [SerializeField] protected TextMeshProUGUI reserveAmmoText;
    [SerializeField] protected Image ammoBar;
    [SerializeField] protected Color fullAmmoColor = Color.green;
    [SerializeField] protected Color lowAmmoColor = Color.yellow;
    [SerializeField] protected Color emptyAmmoColor = Color.red;
    [SerializeField] protected float lowAmmoThreshold = 0.3f;

    [Header("Weapon Info")]
    [SerializeField] protected TextMeshProUGUI weaponNameText;
    [SerializeField] protected Image weaponIcon;
    [SerializeField] protected GameObject[] weaponSlotIndicators;

    [Header("Reload Indicator")]
    [SerializeField] protected Image reloadBar;
    [SerializeField] protected GameObject reloadPanel;

    [Header("Damage Indicators")]
    [SerializeField] protected GameObject damageNumberPrefab;
    [SerializeField] protected Transform damageNumberParent;

    protected WeaponManager weaponManager;
    protected IWeapon currentWeapon;
    protected Coroutine reloadRoutine;

    protected virtual void Awake()
    {
        weaponManager = Object.FindAnyObjectByType<WeaponManager>();
        InitializeUI();
    }

    protected virtual void Start()
    {
        if (weaponManager != null)
        {
            weaponManager.OnWeaponChanged += OnWeaponChanged;
            weaponManager.OnAmmoChanged += OnAmmoChanged;
        }
    }

    protected virtual void InitializeUI()
    {
        // Initialize crosshair
        if (crosshairImage != null)
        {
            crosshairImage.color = defaultCrosshairColor;
            UpdateCrosshairSize(crosshairSize);
        }

        // Initialize ammo display
        UpdateAmmoDisplay(0, 0, false);

        // Hide reload panel
        if (reloadPanel != null)
            reloadPanel.SetActive(false);

        // Initialize weapon slots
        UpdateWeaponSlotIndicators(-1);
    }

    protected virtual void OnWeaponChanged(IWeapon newWeapon)
    {
        currentWeapon = newWeapon;

        UpdateWeaponInfo();
        UpdateAmmoUI();
        UpdateCrosshairForWeapon();
    }

    protected virtual void OnAmmoChanged(int currentAmmo, int reserveAmmo)
    {
        UpdateAmmoDisplay(currentAmmo, reserveAmmo, currentWeapon != null);
    }

    protected virtual void Update()
    {
        UpdateCrosshair();
    }

    protected virtual void UpdateCrosshair()
    {
        if (!dynamicCrosshair || crosshairImage == null || currentWeapon == null) return;

        if (currentWeapon is RangedWeapon rangedWeapon)
        {
            WeaponData weaponData = rangedWeapon.GetWeaponData();

            // Adjust crosshair based on weapon state
            float currentSpread = GetWeaponSpread();
            float sizeFactor = 1f + (currentSpread / weaponData.maxSpread) * 0.5f;

            UpdateCrosshairSize(crosshairSize * sizeFactor);

            // Change color based on state
            if (rangedWeapon.IsReloading())
            {
                crosshairImage.color = Color.gray;
            }
            else if (rangedWeapon.CanFire())
            {
                crosshairImage.color = defaultCrosshairColor;
            }
            else
            {
                crosshairImage.color = emptyAmmoColor;
            }
        }
        else
        {
            // Melee weapon - show different crosshair
            crosshairImage.color = Color.white;
            UpdateCrosshairSize(crosshairSize * 1.2f);
        }
    }

    protected virtual float GetWeaponSpread()
    {
        // This would need to be exposed from the weapon
        if (currentWeapon is RangedWeapon rangedWeapon)
        {
            // For now, return a calculated spread based on weapon state
            WeaponData data = rangedWeapon.GetWeaponData();
            return data.baseSpread;
        }
        return 0f;
    }

    protected virtual void UpdateCrosshairSize(float size)
    {
        if (crosshairImage != null)
        {
            crosshairImage.rectTransform.sizeDelta = new Vector2(size, size);
        }
    }

    protected virtual void UpdateCrosshairForWeapon()
    {
        if (currentWeapon == null) return;

        WeaponData weaponData = currentWeapon.GetWeaponData();

        // Different crosshair styles for different weapons
        switch (weaponData.weaponType)
        {
            case WeaponType.Shotgun:
                // Wider crosshair for shotgun
                crosshairSize = 30f;
                break;
            case WeaponType.Sniper:
                // Smaller, more precise crosshair for sniper
                crosshairSize = 15f;
                defaultCrosshairColor = Color.green;
                break;
            case WeaponType.Pistol:
                crosshairSize = 20f;
                defaultCrosshairColor = Color.white;
                break;
            default:
                crosshairSize = 25f;
                defaultCrosshairColor = Color.white;
                break;
        }
    }

    protected virtual void UpdateAmmoUI()
    {
        if (currentWeapon is RangedWeapon rangedWeapon)
        {
            UpdateAmmoDisplay(rangedWeapon.GetCurrentAmmo(), rangedWeapon.GetReserveAmmo(), true);
        }
        else
        {
            UpdateAmmoDisplay(-1, -1, false);
        }
    }

    protected virtual void UpdateAmmoDisplay(int currentAmmo, int reserveAmmo, bool showAmmo)
    {
        if (currentAmmoText != null)
        {
            if (showAmmo)
            {
                currentAmmoText.text = currentAmmo.ToString();
                currentAmmoText.gameObject.SetActive(true);
            }
            else
            {
                currentAmmoText.gameObject.SetActive(false);
            }
        }

        if (reserveAmmoText != null)
        {
            if (showAmmo && reserveAmmo >= 0)
            {
                reserveAmmoText.text = reserveAmmo.ToString();
                reserveAmmoText.gameObject.SetActive(true);
            }
            else
            {
                reserveAmmoText.gameObject.SetActive(false);
            }
        }

        if (ammoBar != null)
        {
            if (showAmmo)
            {
                WeaponData weaponData = currentWeapon?.GetWeaponData();
                if (weaponData != null)
                {
                    float fillAmount = (float)currentAmmo / weaponData.magazineSize;
                    ammoBar.fillAmount = fillAmount;

                    // Change color based on ammo level
                    if (fillAmount <= lowAmmoThreshold)
                    {
                        ammoBar.color = lowAmmoColor;
                    }
                    else if (currentAmmo == 0)
                    {
                        ammoBar.color = emptyAmmoColor;
                    }
                    else
                    {
                        ammoBar.color = fullAmmoColor;
                    }
                }
                ammoBar.gameObject.SetActive(true);
            }
            else
            {
                ammoBar.gameObject.SetActive(false);
            }
        }
    }

    protected virtual void UpdateWeaponInfo()
    {
        if (currentWeapon == null) return;

        WeaponData weaponData = currentWeapon.GetWeaponData();

        if (weaponNameText != null)
        {
            weaponNameText.text = weaponData.weaponName;
        }

        if (weaponIcon != null && weaponData.weaponIcon != null)
        {
            weaponIcon.sprite = weaponData.weaponIcon;
            weaponIcon.gameObject.SetActive(true);
        }
    }

    protected virtual void UpdateWeaponSlotIndicators(int activeSlot)
    {
        for (int i = 0; i < weaponSlotIndicators.Length; i++)
        {
            if (weaponSlotIndicators[i] != null)
            {
                bool isActive = (i == activeSlot);
                weaponSlotIndicators[i].SetActive(isActive);
            }
        }
    }

    public virtual void ShowReloadBar(float reloadTime)
    {
        if (reloadRoutine != null)
        {
            StopCoroutine(reloadRoutine);
        }

        reloadRoutine = StartCoroutine(ReloadBarRoutine(reloadTime));
    }

    protected virtual IEnumerator ReloadBarRoutine(float reloadTime)
    {
        if (reloadPanel != null)
            reloadPanel.SetActive(true);

        if (reloadBar != null)
        {
            float elapsed = 0f;

            while (elapsed < reloadTime)
            {
                elapsed += Time.deltaTime;
                reloadBar.fillAmount = elapsed / reloadTime;
                yield return null;
            }

            reloadBar.fillAmount = 1f;
        }

        yield return new WaitForSeconds(0.5f);

        if (reloadPanel != null)
            reloadPanel.SetActive(false);

        reloadRoutine = null;
    }

    public virtual void ShowDamageNumber(float damage, Vector3 worldPosition, bool isCritical = false)
    {
        if (damageNumberPrefab == null || damageNumberParent == null) return;

        GameObject damageObj = Instantiate(damageNumberPrefab, damageNumberParent);
        TextMeshProUGUI damageText = damageObj.GetComponent<TextMeshProUGUI>();

        if (damageText != null)
        {
            damageText.text = Mathf.Ceil(damage).ToString();
            damageText.color = isCritical ? Color.red : Color.yellow;
            damageText.fontSize = isCritical ? 36 : 24;

            // Start animation coroutine
            StartCoroutine(AnimateDamageNumber(damageObj, worldPosition));
        }
    }

    protected virtual IEnumerator AnimateDamageNumber(GameObject damageObj, Vector3 worldPosition)
    {
        Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
        damageObj.transform.position = screenPosition;

        Vector3 startPosition = screenPosition;
        Vector3 endPosition = screenPosition + Vector3.up * 50f;

        float duration = 1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Move up and fade out
            damageObj.transform.position = Vector3.Lerp(startPosition, endPosition, t);
            CanvasGroup canvasGroup = damageObj.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f - t;
            }

            yield return null;
        }

        Destroy(damageObj);
    }

    protected virtual void OnDestroy()
    {
        if (weaponManager != null)
        {
            weaponManager.OnWeaponChanged -= OnWeaponChanged;
            weaponManager.OnAmmoChanged -= OnAmmoChanged;
        }
    }
}