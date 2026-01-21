using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private WeaponController weaponOwner;
    [SerializeField] private WeaponData[] startingWeapons;
    [SerializeField] private WeaponUI weaponUI;

    private BaseWeapon[] slots;
    private int currentIndex = -1;

    private void Start()
    {
        if (weaponOwner == null)
        {
            Debug.LogError("[WeaponManager] WeaponOwner is missing! Please assign a WeaponController.", this);
            return;
        }

        if (weaponHolder == null)
        {
            Debug.LogError("[WeaponManager] WeaponHolder is missing! Please assign a transform for weapon parent.", this);
            return;
        }

        if (startingWeapons == null || startingWeapons.Length == 0)
        {
            Debug.LogWarning("[WeaponManager] No starting weapons assigned.", this);
            slots = new BaseWeapon[0];
            return;
        }

        slots = new BaseWeapon[startingWeapons.Length];
        EquipStartingWeapons();
    }

    private void EquipStartingWeapons()
    {
        if (startingWeapons == null) return;

        for (int i = 0; i < startingWeapons.Length; i++)
        {
            if (startingWeapons[i] != null)
                AddWeapon(startingWeapons[i], i);
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
            {
                EquipWeapon(i);
                break;
            }
        }
    }

    public void AddWeapon(WeaponData data, int slot)
    {
        if (data == null)
        {
            Debug.LogError("[WeaponManager] WeaponData is null!", this);
            return;
        }

        if (data.weaponPrefab == null)
        {
            Debug.LogError($"[WeaponManager] Weapon prefab missing for {data.weaponName}!", this);
            return;
        }

        if (weaponHolder == null)
        {
            Debug.LogError("[WeaponManager] WeaponHolder is null! Cannot add weapon.", this);
            return;
        }

        if (weaponOwner == null)
        {
            Debug.LogError("[WeaponManager] WeaponOwner is null! Cannot initialize weapon.", this);
            return;
        }

        if (slot < 0 || slot >= slots.Length)
        {
            Debug.LogError($"[WeaponManager] Invalid slot index: {slot}. Valid range: 0-{slots.Length - 1}", this);
            return;
        }

        GameObject weaponGO = Instantiate(data.weaponPrefab, weaponHolder);
        weaponGO.transform.localPosition = Vector3.zero;
        weaponGO.transform.localRotation = Quaternion.identity;
        weaponGO.transform.localScale = Vector3.one;

        BaseWeapon weapon = weaponGO.GetComponent<BaseWeapon>();
        if (weapon == null)
        {
            Debug.LogError($"[WeaponManager] BaseWeapon component missing on prefab: {data.weaponName}", this);
            Destroy(weaponGO);
            return;
        }

        weapon.Initialize(data, weaponOwner);
        weaponGO.SetActive(false);
        slots[slot] = weapon;
    }

    public void EquipWeapon(int index)
    {
        if (slots == null || index < 0 || index >= slots.Length || slots[index] == null)
        {
            Debug.LogWarning($"[WeaponManager] Cannot equip weapon at index {index}. Slot is empty or invalid.", this);
            return;
        }

        if (currentIndex >= 0 && currentIndex < slots.Length && slots[currentIndex] != null)
        {
            slots[currentIndex].Holster();
            slots[currentIndex].gameObject.SetActive(false);
        }

        currentIndex = index;
        slots[currentIndex].gameObject.SetActive(true);
        slots[currentIndex].Draw();

        if (weaponUI != null)
        {
            WeaponData data = slots[currentIndex].GetWeaponData();
            if (data != null)
                weaponUI.SetWeaponIcon(data.weaponIcon);
        }
    }

    public void NextWeapon()
    {
        if (slots == null || slots.Length == 0) return;
        
        int nextIndex = (currentIndex + 1) % slots.Length;
        int attempts = 0;
        
        while (slots[nextIndex] == null && attempts < slots.Length)
        {
            nextIndex = (nextIndex + 1) % slots.Length;
            attempts++;
        }
        
        if (slots[nextIndex] != null)
            EquipWeapon(nextIndex);
    }

    public void PreviousWeapon()
    {
        if (slots == null || slots.Length == 0) return;
        
        int prevIndex = (currentIndex - 1 + slots.Length) % slots.Length;
        int attempts = 0;
        
        while (slots[prevIndex] == null && attempts < slots.Length)
        {
            prevIndex = (prevIndex - 1 + slots.Length) % slots.Length;
            attempts++;
        }
        
        if (slots[prevIndex] != null)
            EquipWeapon(prevIndex);
    }

    public BaseWeapon GetCurrentWeapon() => currentIndex < 0 ? null : slots[currentIndex];
}