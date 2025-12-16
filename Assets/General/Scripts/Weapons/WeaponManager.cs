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
        slots = new BaseWeapon[startingWeapons.Length];
        EquipStartingWeapons();
    }

    private void EquipStartingWeapons()
    {
        for (int i = 0; i < startingWeapons.Length; i++)
            AddWeapon(startingWeapons[i], i);

        EquipWeapon(0);
    }

    public void AddWeapon(WeaponData data, int slot)
    {
        if (!data || !data.weaponPrefab)
        {
            Debug.LogError("[WeaponManager] WeaponData or prefab missing");
            return;
        }

        GameObject weaponGO = Instantiate(data.weaponPrefab, weaponHolder);
        weaponGO.transform.localPosition = Vector3.zero;
        weaponGO.transform.localRotation = Quaternion.identity;
        weaponGO.transform.localScale = Vector3.one;

        BaseWeapon weapon = weaponGO.GetComponent<BaseWeapon>();
        if (!weapon)
        {
            Debug.LogError("[WeaponManager] BaseWeapon missing on prefab");
            Destroy(weaponGO);
            return;
        }

        weapon.Initialize(data, weaponOwner);
        weaponGO.SetActive(false);
        slots[slot] = weapon;
    }

    public void EquipWeapon(int index)
    {
        if (index < 0 || index >= slots.Length || slots[index] == null) return;

        if (currentIndex >= 0)
            slots[currentIndex].gameObject.SetActive(false);

        currentIndex = index;
        slots[currentIndex].gameObject.SetActive(true);
        slots[currentIndex].Draw();

        if (weaponUI != null)
            weaponUI.SetWeaponIcon(slots[currentIndex].GetWeaponData().weaponIcon);
    }

    public void NextWeapon() => EquipWeapon((currentIndex + 1) % slots.Length);
    public void PreviousWeapon() => EquipWeapon((currentIndex - 1 + slots.Length) % slots.Length);

    public BaseWeapon GetCurrentWeapon() => currentIndex < 0 ? null : slots[currentIndex];
}