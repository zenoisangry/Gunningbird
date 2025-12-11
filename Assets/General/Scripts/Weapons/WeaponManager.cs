using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [Header("Weapon Slots")]
    [SerializeField] protected List<GameObject> weaponSlots = new List<GameObject>();
    [SerializeField] protected List<WeaponData> availableWeapons = new List<WeaponData>();
    [SerializeField] protected List<WeaponData> startingWeapons = new List<WeaponData>();

    [Header("References")]
    [SerializeField] protected Transform weaponHolder;
    [SerializeField] protected Camera playerCamera;
    [SerializeField] protected Animator playerAnimator;
    [SerializeField] protected AudioSource weaponAudioSource;

    [Header("Weapon Switching")]
    [SerializeField] protected float weaponSwitchTime = 0.3f;
    [SerializeField] protected bool autoSwitchOnEmpty = true;

    [Header("Melee")]
    [SerializeField] protected WeaponData defaultMeleeWeapon;

    protected List<IWeapon> weapons = new List<IWeapon>();
    protected int currentWeaponIndex = -1;
    protected IWeapon currentWeapon;
    protected WeaponOwner weaponOwner;
    protected bool isSwitching = false;

    public System.Action<IWeapon> OnWeaponChanged;
    public System.Action<int, int> OnAmmoChanged;

    protected virtual void Awake()
    {
        InitializeWeaponOwner();
        InitializeWeaponSlots();
    }

    protected virtual void Start()
    {
        EquipStartingWeapons();
    }

    protected virtual void InitializeWeaponOwner()
    {
        WeaponController weaponController = GetComponent<WeaponController>();
        if (weaponController = null)
        {
            weaponOwner = new WeaponOwner();
            weaponOwner.Initialize(weaponHolder, playerCamera, playerAnimator, weaponAudioSource, this);
        }
    }

    protected virtual void InitializeWeaponSlots()
    {
        // Initialize weapon slots if they're empty
        for (int i = 0; i < 4; i++) // Primary, Secondary, Tertiary, Melee
        {
            if (weaponSlots.Count <= i)
            {
                GameObject slot = new GameObject($"WeaponSlot_{i}");
                slot.transform.SetParent(weaponHolder);
                slot.SetActive(false);
                weaponSlots.Add(slot);
            }
        }
    }

    protected virtual void EquipStartingWeapons()
    {
        foreach (WeaponData weaponData in startingWeapons)
        {
            AddWeapon(weaponData);
        }

        // Equip default melee weapon if none assigned
        if (GetMeleeWeapon() == null && defaultMeleeWeapon != null)
        {
            AddWeapon(defaultMeleeWeapon, 3); // Melee slot
        }

        // Switch to first available weapon
        if (weapons.Count > 0)
        {
            SwitchToWeapon(0);
        }
    }

    public virtual void AddWeapon(WeaponData weaponData, int slotIndex = -1)
    {
        if (weaponData == null) return;

        GameObject weaponObject = Instantiate(weaponData.weaponPrefab);
        weaponObject.transform.SetParent(weaponHolder);
        weaponObject.SetActive(false);

        IWeapon weapon = weaponObject.GetComponent<IWeapon>();
        if (weapon == null)
        {
            Debug.LogError($"Weapon prefab {weaponData.name} doesn't have an IWeapon component!");
            Destroy(weaponObject);
            return;
        }

        weapon.Initialize(weaponData, weaponOwner);

        if (slotIndex == -1)
        {
            slotIndex = GetNextAvailableSlot(weaponData.weaponType);
        }

        if (slotIndex >= 0 && slotIndex < weaponSlots.Count)
        {
            // Move weapon to appropriate slot
            if (weaponSlots[slotIndex] != null && weaponSlots[slotIndex].transform.childCount > 0)
            {
                Destroy(weaponSlots[slotIndex].transform.GetChild(0).gameObject);
            }

            weaponObject.transform.SetParent(weaponSlots[slotIndex].transform);
            weaponObject.transform.localPosition = Vector3.zero;
            weaponObject.transform.localRotation = Quaternion.identity;

            // Add to weapons list
            int existingIndex = GetWeaponIndexInSlot(slotIndex);
            if (existingIndex >= 0)
            {
                weapons[existingIndex] = weapon;
            }
            else
            {
                weapons.Add(weapon);
            }
        }
    }

    protected virtual int GetNextAvailableSlot(WeaponType weaponType)
    {
        switch (weaponType)
        {
            case WeaponType.Pistol:
            case WeaponType.SMG:
                return 0; // Primary slot
            case WeaponType.Rifle:
            case WeaponType.Shotgun:
                return 1; // Secondary slot
            case WeaponType.Sniper:
            case WeaponType.Heavy:
                return 2; // Tertiary slot
            case WeaponType.Melee:
                return 3; // Melee slot
            default:
                return 0;
        }
    }

    protected virtual int GetWeaponIndexInSlot(int slotIndex)
    {
        for (int i = 0; i < weapons.Count; i++)
        {
            if (weapons[i] != null && weapons[i].GetWeaponData().weaponType != WeaponType.Melee)
            {
                WeaponType type = weapons[i].GetWeaponData().weaponType;
                int expectedSlot = GetNextAvailableSlot(type);
                if (expectedSlot == slotIndex)
                    return i;
            }
        }
        return -1;
    }

    public virtual void SwitchToWeapon(int index)
    {
        if (isSwitching || index < 0 || index >= weapons.Count) return;

        StartCoroutine(SwitchWeaponRoutine(index));
    }

    protected virtual IEnumerator SwitchWeaponRoutine(int newIndex)
    {
        isSwitching = true;

        // Holster current weapon
        if (currentWeapon != null)
        {
            currentWeapon.Holster();
            weaponSlots[currentWeaponIndex].SetActive(false);
        }

        yield return new WaitForSeconds(weaponSwitchTime * 0.5f);

        // Equip new weapon
        currentWeaponIndex = newIndex;
        currentWeapon = weapons[currentWeaponIndex];
        weaponSlots[currentWeaponIndex].SetActive(true);
        currentWeapon.Draw();

        yield return new WaitForSeconds(weaponSwitchTime * 0.5f);

        isSwitching = false;
        OnWeaponChanged?.Invoke(currentWeapon);
        UpdateAmmoUI();
    }

    #region Public Input Methods (Call these from your input system)
    public virtual void SelectWeaponSlot(int slotNumber)
    {
        int index = slotNumber - 1; // Convert to 0-based index
        if (index >= 0 && index < weapons.Count)
        {
            SwitchToWeapon(index);
        }
    }

    public virtual void NextWeapon()
    {
        if (weapons.Count <= 1) return;

        int nextIndex = (currentWeaponIndex + 1) % weapons.Count;
        SwitchToWeapon(nextIndex);
    }

    public virtual void PreviousWeapon()
    {
        if (weapons.Count <= 1) return;

        int prevIndex = (currentWeaponIndex - 1 + weapons.Count) % weapons.Count;
        SwitchToWeapon(prevIndex);
    }

    public virtual void SwitchWeapon()
    {
        // Cycle through weapons (for prototype - single key weapon switching)
        if (weapons.Count > 0)
        {
            int nextIndex = (currentWeaponIndex + 1) % weapons.Count;
            SwitchToWeapon(nextIndex);
        }
    }

    public virtual void StartFiring()
    {
        if (currentWeapon != null && !isSwitching)
        {
            // Start continuous firing for automatic weapons
            StartCoroutine(ContinuousFireRoutine());
        }
    }

    public virtual void StopFiring()
    {
        // Stop continuous firing
        StopAllCoroutines(); // This will stop the fire routine
    }

    public virtual void SecondaryFire()
    {
        if (currentWeapon != null && !isSwitching)
        {
            currentWeapon.SecondaryFire();
        }
    }

    public virtual void Reload()
    {
        if (currentWeapon != null && !isSwitching)
        {
            currentWeapon.Reload();
        }
    }

    public virtual void MeleeAttack()
    {
        SwitchToMelee();
    }
    #endregion

    protected virtual IEnumerator ContinuousFireRoutine()
    {
        while (currentWeapon != null && currentWeapon.CanFire())
        {
            currentWeapon.PrimaryFire();
            UpdateAmmoUI();

            // Wait for fire rate
            WeaponData data = currentWeapon.GetWeaponData();
            float fireDelay = 60f / data.fireRate;
            yield return new WaitForSeconds(fireDelay);
        }
    }

    public virtual void SwitchToMelee()
    {
        IWeapon meleeWeapon = GetMeleeWeapon();
        if (meleeWeapon != null)
        {
            int meleeIndex = weapons.IndexOf(meleeWeapon);
            SwitchToWeapon(meleeIndex);
        }
    }

    protected virtual IWeapon GetMeleeWeapon()
    {
        foreach (IWeapon weapon in weapons)
        {
            if (weapon != null && weapon.GetWeaponData().weaponType == WeaponType.Melee)
                return weapon;
        }
        return null;
    }

    protected virtual void Update()
    {
        // This method is now handled by external input calls
        // Keeping for compatibility with old input system
    }

    protected virtual void UpdateAmmoUI()
    {
        if (currentWeapon is RangedWeapon rangedWeapon)
        {
            OnAmmoChanged?.Invoke(rangedWeapon.GetCurrentAmmo(), rangedWeapon.GetReserveAmmo());
        }
        else
        {
            OnAmmoChanged?.Invoke(-1, -1); // Melee weapon
        }
    }

    public virtual void AddAmmo(AmmoType ammoType, int amount)
    {
        foreach (IWeapon weapon in weapons)
        {
            if (weapon is RangedWeapon rangedWeapon)
            {
                if (rangedWeapon.GetWeaponData().ammoType == ammoType)
                {
                    rangedWeapon.AddAmmo(amount);
                }
            }
        }
    }

    // Getters
    public virtual IWeapon GetCurrentWeapon() => currentWeapon;
    public virtual int GetCurrentWeaponIndex() => currentWeaponIndex;
    public virtual List<IWeapon> GetAllWeapons() => new List<IWeapon>(weapons);
    public virtual bool IsSwitching() => isSwitching;
}

// Fallback WeaponOwner class if no WeaponController is available
public class WeaponOwner : IWeaponOwner
{
    protected Transform weaponHolder;
    protected Camera playerCamera;
    protected Animator animator;
    protected AudioSource audioSource;
    protected WeaponManager weaponManager;

    public void Initialize(Transform holder, Camera camera, Animator anim, AudioSource audio, WeaponManager manager)
    {
        weaponHolder = holder;
        playerCamera = camera;
        animator = anim;
        audioSource = audio;
        weaponManager = manager;
    }

    public Transform GetFireTransform() => weaponHolder;
    public Transform GetCameraTransform() => playerCamera.transform;
    public Animator GetAnimator() => animator;
    public virtual void AddRecoil(Vector2 recoil) {}
    public virtual void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}