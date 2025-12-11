using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float damage, DamageType damageType);
    float GetHealth();
    float GetMaxHealth();
    void Heal(float amount);
    bool IsDead();
    void SetHealth(float health);
}

public enum DamageType
{
    Bullet,
    Melee,
    Explosion,
    Fire,
    Poison,
    Fall,
    ArmorPiercing,
    Generic
}