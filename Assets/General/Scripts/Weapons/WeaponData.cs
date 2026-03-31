using UnityEngine;
using System;
using System.Collections.Generic;
using NUnit.Framework;

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

    [Header("Shotgun Settings")]
    [Tooltip("Use horizontal spread pattern for shotguns instead of spherical")]
    public bool useHorizontalSpread = false;

    [Tooltip("Horizontal spread angle for shotguns (degrees)")]
    public float horizontalSpreadAngle = 15f;

    [Tooltip("Vertical spread angle for shotguns (degrees) - usually much smaller")]
    public float verticalSpreadAngle = 3f;

    [Header("View Model Transform (First Person)")]
    [Tooltip("Position offset in first person view. Leave at zero to use prefab defaults.")]
    public Vector3 weaponViewPosition = Vector3.zero;

    [Tooltip("Rotation offset in first person view (Euler angles). Leave at zero to use prefab defaults.")]
    public Vector3 weaponViewRotation = Vector3.zero;

    [Tooltip("Scale in first person view. Use (1,1,1) to use prefab defaults, or adjust per weapon.")]
    public Vector3 weaponViewScale = Vector3.one;

    [Header("Pattern fire settings")]
    public float projectileNumber;
    public List<Vector2> projectileAngles;
    public float fanDelay;
}

public enum WeaponType
{
    Pistol,
    Rifle,
    Shotgun,
    SMG,
    Sniper,
    Heavy,
    Melee,
    Spread,
    Fan
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