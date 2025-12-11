using UnityEngine;

public interface IWeapon
{
    void Initialize(WeaponData data, WeaponOwner owner);
    void PrimaryFire();
    void SecondaryFire();
    void Reload();
    void Draw();
    void Holster();
    bool CanFire();
    bool IsReloading();
    WeaponData GetWeaponData();
}

public interface IWeaponOwner
{
    Transform GetFireTransform();
    Transform GetCameraTransform();
    Animator GetAnimator();
    void AddRecoil(Vector2 recoil);
    void PlaySound(AudioClip clip);
}