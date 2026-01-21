using UnityEngine;
using System;
using System.Collections;

public class HealthSystem : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField] protected float maxHealth = 100f;
    [SerializeField] protected float currentHealth;
    [SerializeField] protected bool isDead = false;

    [Header("Regeneration")]
    [SerializeField] protected bool enableRegeneration = true;
    [SerializeField] protected float regenerationDelay = 3f;
    [SerializeField] protected float regenerationRate = 5f;
    [SerializeField] protected float regenerationPercentage = 0.5f;

    [Header("Damage Resistance")]
    [SerializeField] protected float bulletResistance = 0f;
    [SerializeField] protected float meleeResistance = 0f;
    [SerializeField] protected float explosionResistance = 0f;
    [SerializeField] protected float fireResistance = 0f;
    [SerializeField] protected float genericResistance = 0f;

    public event Action<float, float> HealthChanged; // current, max
    public event Action Died;
    public event Action<float> DamageTaken; // final damage applied
    public event Action<float> Healed; // heal amount applied

    protected Coroutine regenerationCoroutine;
    protected float lastDamageTime;

    protected virtual void Awake()
    {
        if (maxHealth <= 0f)
        {
            Debug.LogWarning($"[HealthSystem] MaxHealth is {maxHealth}. Setting to 100.", this);
            maxHealth = 100f;
        }
        currentHealth = maxHealth;
    }

    public virtual void TakeDamage(float damage, DamageType damageType)
    {
        if (isDead || damage <= 0f) return;

        float finalDamage = CalculateDamage(damage, damageType);

        currentHealth = Mathf.Max(0, currentHealth - finalDamage);
        lastDamageTime = Time.time;

        if (regenerationCoroutine != null)
            StopCoroutine(regenerationCoroutine);

        if (enableRegeneration && currentHealth > 0)
            regenerationCoroutine = StartCoroutine(RegenerationRoutine());

        HealthChanged?.Invoke(currentHealth, maxHealth);
        DamageTaken?.Invoke(finalDamage);

        if (currentHealth <= 0 && !isDead)
            Die();
    }

    protected virtual float CalculateDamage(float damage, DamageType damageType)
    {
        float resistance = 0f;

        switch (damageType)
        {
            case DamageType.Bullet: resistance = bulletResistance; break;
            case DamageType.Melee: resistance = meleeResistance; break;
            case DamageType.Explosion: resistance = explosionResistance; break;
            case DamageType.Fire: resistance = fireResistance; break;
            case DamageType.ArmorPiercing: resistance = 0f; break;
            default: resistance = genericResistance; break;
        }

        // Clamp resistance between 0 and 1
        resistance = Mathf.Clamp01(resistance);
        return damage * (1f - resistance);
    }

    protected virtual IEnumerator RegenerationRoutine()
    {
        yield return new WaitForSeconds(regenerationDelay);

        float maxRegenAmount = maxHealth * regenerationPercentage;
        float targetHealth = Mathf.Min(maxHealth, currentHealth + maxRegenAmount);

        while (currentHealth < targetHealth && !isDead)
        {
            float healthToRegen = Mathf.Min(regenerationRate * Time.deltaTime, targetHealth - currentHealth);
            currentHealth += healthToRegen;

            HealthChanged?.Invoke(currentHealth, maxHealth);
            Healed?.Invoke(healthToRegen);

            yield return null;
        }

        currentHealth = Mathf.Min(currentHealth, maxHealth);
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public virtual void Heal(float amount)
    {
        if (isDead || amount <= 0f) return;
        float before = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        float applied = Mathf.Max(0f, currentHealth - before);
        HealthChanged?.Invoke(currentHealth, maxHealth);
        if (applied > 0f)
            Healed?.Invoke(applied);
    }

    public virtual void SetHealth(float health)
    {
        currentHealth = Mathf.Clamp(health, 0f, maxHealth);
        HealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0 && !isDead)
            Die();
    }

    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;
        Died?.Invoke();
        if (regenerationCoroutine != null)
            StopCoroutine(regenerationCoroutine);
    }

    public virtual void Revive(float healthPercentage = 1f)
    {
        isDead = false;
        currentHealth = maxHealth * Mathf.Clamp01(healthPercentage);
        HealthChanged?.Invoke(currentHealth, maxHealth);

        if (enableRegeneration)
            regenerationCoroutine = StartCoroutine(RegenerationRoutine());
    }

    public virtual float GetHealth() => currentHealth;
    public virtual float GetMaxHealth() => maxHealth;
    public virtual bool IsDead() => isDead;
    public virtual float GetHealthPercentage() => maxHealth > 0f ? currentHealth / maxHealth : 0f;
}