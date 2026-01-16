using UnityEngine;
using System;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Weapon System/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Basic Info")]
    public string weaponName;
    public string description;
    public WeaponType weaponType;
    public Sprite weaponIcon;
    public GameObject weaponPrefab;
    public GameObject pickupPrefab;
    public GameObject bulletPrefab;

    [Header("Visual & Audio")]
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public AudioClip emptySound;
    public ParticleSystem muzzleFlash;
    public GameObject bulletHolePrefab;
    public GameObject bulletTrailPrefab;

    [Header("Animation")]
    public string shootAnimationTrigger = "Shoot";
    public string reloadAnimationTrigger = "Reload";
    public string drawAnimationTrigger = "Draw";
    public string meleeAnimationTrigger = "Melee";

    [Header("Damage")]
    public float damage = 25f;
    public float headshotMultiplier = 2f;
    public float meleeDamage = 50f;
    public float meleeInstaKillThreshold = 30f;

    [Header("Fire Rate")]
    public float fireRate = 600f;
    public float bulletSpeed = 100f;
    public float secondaryFireRate = 300f;
    public bool isFullAuto = true;
    public bool isFullAutoSecondary = false;

    [Header("Ammo")]
    public bool usesAmmo = true;
    public int magazineSize = 30;
    public int totalAmmo = 120;
    public float reloadTime = 2.5f;
    public AmmoType ammoType;
    public bool hasInfiniteAmmo = false;

    [Header("Spread & Recoil")]
    public float baseSpread = 0f;
    public float maxSpread = 5f;
    public float spreadIncreasePerShot = 0.5f;
    public float spreadDecreaseSpeed = 2f;
    public Vector2 recoilPattern = new Vector2(1f, 1f);
    public float recoilRecoverySpeed = 5f;

    [Header("Secondary Fire")]
    public SecondaryFireType secondaryFireType;
    public float secondaryFireDamage = 50f;
    public float secondaryFireCooldown = 1f;
    public int secondaryFireAmmoCost = 1;

    [Header("Melee")]
    public float meleeRange = 2f;
    public float meleeCooldown = 0.5f;
    public float meleeAngle = 90f;
    public float meleeHitDelay = 0.2f;
}

public enum WeaponType
{
    Pistol,
    Rifle,
    Shotgun,
    SMG,
    Sniper,
    Heavy,
    Melee
}

public enum AmmoType
{
    Pistol,
    Rifle,
    Shotgun,
    Sniper,
    Heavy,
    None
}

public enum SecondaryFireType
{
    None,
    Burst,
    Zoom,
    Grenade,
    Bayonet,
    Explosive,
    ArmorPiercing
}