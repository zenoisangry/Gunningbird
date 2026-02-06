using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using static FeralColonistNav;

public class CorruptedGunslingerNav : MonoBehaviour
{
    [Header("Links to other objects")]
    public PlayerInput player;
    public GameObject body;
    public GameObject weaponHolder;
    private EscapeManager escManager;
    private BoxCollider bodyCollider;
    private FeralColonistMovement movementScript;
    private NavMeshAgent navigation;
    private float playerDistance;
    private HealthSystem healthSystem;
    private Animator animator;

    [Header("Enemy Attack Logic")]
    [SerializeField] private EnemyRangedAttack enemyRangedAttack;
    [SerializeField] private Transform attackTarget;

    [Header("AI variables")]
    public float aggroRange;
    public float losAggroRange;
    public float escapeRange;
    public float shootingMinRange;
    public float shootingMaxRange;
    public float escapeRangePriority;
    public float escapeCheckCooldown;
    public float escapeSafetyPriority;
    private bool canEscape;
    public float baseSpeed;
    public float formChangeCooldown;
    public float formChangeSpeed;
    public float formChangeDistance;
    private bool canChangeForm = true;
    private bool climbing = false;

    [Header("Damage / Death Reactions")]
    [SerializeField] private bool playHitReaction = true;
    [SerializeField] private float hitStunTime = 0.15f;
    [SerializeField] private string hitAnimationTrigger = "Hit";
    [SerializeField] private string deathAnimationTrigger = "Die";
    [SerializeField] private bool disableBodyCollidersOnDeath = true;
    [SerializeField] private bool disableNavOnDeath = true;
    [SerializeField] private bool disableWeaponAttackOnDeath = true;
    [SerializeField] private bool destroyEnemyRootOnDeath = true;
    [SerializeField] private float destroyDelay = 2f;

    //Attack variables
    private float attackSpeed;
    private float reloadTime;

    public GunslingerBehavior currentBehavior = GunslingerBehavior.Idle;
    private NavMeshHit hit;
    private bool isDead = false;

    private Coroutine hitReactCoroutine;
    private Coroutine attackCoroutine;
    private Coroutine jumpCoroutine;

    // Start is called once before the first execution of Update
    void Start()
    {
        player = FindFirstObjectByType<PlayerInput>();
        escManager = FindFirstObjectByType<EscapeManager>();
        navigation = GetComponent<NavMeshAgent>();
        bodyCollider = body.GetComponent<BoxCollider>();
        movementScript = body.GetComponent<FeralColonistMovement>();
        animator = body != null ? body.GetComponentInChildren<Animator>() : GetComponentInChildren<Animator>();

        // Resolve HealthSystem even if it's not on this GameObject (e.g. on Body).
        if (healthSystem == null)
        {
            if (body != null)
                healthSystem = body.GetComponentInChildren<HealthSystem>();

            if (healthSystem == null)
                healthSystem = GetComponentInChildren<HealthSystem>();
        }

        // Subscribe to HealthSystem events (C# events; no Inspector wiring required)
        if (healthSystem != null)
        {
            healthSystem.DamageTaken += HandleDamageTaken;
            healthSystem.Died += HandleDeath;
        }
        else
        {
            Debug.LogWarning($"[FeralColonistNav] HealthSystem not found.", this);
        }

        if (enemyRangedAttack != null)
        {
            if (player != null)
            {
                enemyRangedAttack.SetTarget(player.transform);
                attackTarget = player.transform;
            }
        }
        else
        {
            Debug.LogWarning("[CorruptedGunslingerNav] EnemyRangedAttack not assigned!", this);
        }

        //Set speed
        navigation.speed = baseSpeed;
    }

    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (healthSystem != null)
        {
            healthSystem.DamageTaken -= HandleDamageTaken;
            healthSystem.Died -= HandleDeath;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isDead) return;

        climbing = false;

        if (currentBehavior == GunslingerBehavior.Closing)
        {
            if (player != null)
            {
                //Check current surface;
                navigation.SamplePathPosition(NavMesh.AllAreas, 0.1f, out hit);
                if ((1 << NavMesh.GetAreaFromName("Climb") & hit.mask) == 0)
                {
                    navigation.SetDestination(player.projectedPosition);
                }
                else
                {
                    climbing = true;
                }
            }
        }

        if (currentBehavior == GunslingerBehavior.Shooting)
        {
            if (enemyRangedAttack != null && enemyRangedAttack.CanFire())
            {
                if (enemyRangedAttack.IsAimedAtTarget(10f))
                {
                    enemyRangedAttack.Fire();
                }
            }
        }

        if (!climbing)
        {
            BehaviorSwitchCheck();
        }
    }

    private void BehaviorSwitchCheck()
    {
        if (player == null) return;

        playerDistance = (player.transform.position - transform.position).magnitude;

        switch (currentBehavior)
        {
            case GunslingerBehavior.Idle:
                if ((CheckLOS() && playerDistance <= losAggroRange) || playerDistance <= aggroRange)
                {
                    currentBehavior = GunslingerBehavior.Closing;
                }
                break;

            case GunslingerBehavior.Closing:
                if (CheckLOS() && playerDistance > shootingMinRange && playerDistance <= shootingMaxRange){
                    currentBehavior = GunslingerBehavior.Shooting;
                    navigation.SetDestination(transform.position);
                }
                if (playerDistance <= shootingMinRange && canEscape)
                {
                    FindEscapeZone();
                    currentBehavior = GunslingerBehavior.Escaping;
                }
                break;

            case GunslingerBehavior.Shooting:
                if (!CheckLOS())
                {
                    Debug.Log("[Gunslinger] Lost line of sight, reloading if possible");
                    enemyRangedAttack.Reload();
                    currentBehavior = GunslingerBehavior.Reloading;
                    break;
                }
                else
                {
                    if (enemyRangedAttack != null && enemyRangedAttack.GetWeapon().GetCurrentAmmo() == 0)
                    {
                        Debug.Log("[Gunslinger] Weapon empty, escaping to reload");
                        FindEscapeZone();
                        currentBehavior = GunslingerBehavior.Escaping;
                        break;
                    }
                    else
                    if (playerDistance <= shootingMinRange && canEscape)
                    {
                        Debug.Log("[Gunslinger] Player too close, escaping");
                        FindEscapeZone();
                        currentBehavior = GunslingerBehavior.Escaping;
                        break;
                    }
                    else
                    if (playerDistance > shootingMaxRange)
                    {
                        Debug.Log("[Gunslinger] Player too far, closing distance");
                        currentBehavior = GunslingerBehavior.Closing;
                        break;
                    }
                }
                break;

            case GunslingerBehavior.Reloading:
                if (enemyRangedAttack != null && !enemyRangedAttack.IsReloading())
                {
                    if (CheckLOS() && playerDistance > shootingMinRange && playerDistance <= shootingMaxRange)
                    {
                        Debug.Log("[Gunslinger] Reload complete, resuming shooting");
                        navigation.SetDestination(transform.position);
                        currentBehavior = GunslingerBehavior.Shooting;
                        break;
                    }
                    else
                    {
                        Debug.Log("[Gunslinger] Reload complete, closing distance");
                        currentBehavior = GunslingerBehavior.Closing;
                        break;
                    }
                }
            break;

            case GunslingerBehavior.Escaping:
                if (navigation.remainingDistance <= navigation.stoppingDistance && !navigation.pathPending)
                {
                    Debug.Log("Escape complete. [Gunslinger] Starting reload");
                    navigation.SetDestination(transform.position);
                    enemyRangedAttack.Reload();
                    currentBehavior = GunslingerBehavior.Reloading;
                    navigation.speed = baseSpeed;
                }
                break;
        }
    }

    private void FindEscapeZone()
    {
        Vector3 targetZone = Vector3.zero;
        float targetQuality = 0;
        foreach (KeyValuePair<Vector3, float> zone in escManager.escapeAreas)
        {
            int coveredLines = escManager.CheckZoneLOS(player.gameObject, zone.Key + new Vector3(0, 1, 0), zone.Value);
            float tempQuality = (-(transform.position - zone.Key).magnitude * escapeRangePriority) + (coveredLines * (escapeRange / 5) * escapeSafetyPriority);
            if (tempQuality > targetQuality)
            {
                targetQuality = tempQuality;
                targetZone = zone.Key;
            }
        }
        if (targetZone == Vector3.zero)
        {
            enemyRangedAttack.Reload();
            currentBehavior = GunslingerBehavior.Reloading;
            navigation.SetDestination(transform.position);
        }
        else
        {
            //Cambia behavior in base a se hai la forma o meno
            if (canChangeForm && ((targetZone-transform.position).magnitude) > formChangeDistance)
            {
                navigation.speed = formChangeSpeed;
                StartCoroutine(FormChangeCD());
                canChangeForm = false;
            }
            float maxOffset = escManager.escapeAreas[targetZone];
            Vector3 finaloffset = new Vector3(UnityEngine.Random.Range(-maxOffset, maxOffset), 0, UnityEngine.Random.Range(-maxOffset, maxOffset));
            navigation.SetDestination(targetZone + finaloffset);
        }
        StartCoroutine(EscapeCD());
        canEscape = false;
    }

    private IEnumerator EscapeCD()
    {
        float timer = 0;
        while (timer < escapeCheckCooldown)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        canEscape = true;
    }

    private IEnumerator FormChangeCD()
    {
        float timer = 0;
        while (timer < formChangeCooldown)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        canChangeForm = true;
    }

    private bool CheckLOS()
    {
        if (player == null) return false;
        bool result = Physics.BoxCast(weaponHolder.transform.position, bodyCollider.size/5, player.transform.position - weaponHolder.transform.position,
                                Quaternion.identity, (player.transform.position - weaponHolder.transform.position).magnitude, LayerMask.GetMask("Default"));
        return !result;
    }

    private void HandleDamageTaken(float finalDamage)
    {
        if (isDead) return;

        // Force aggro when taking damage
        if (currentBehavior == GunslingerBehavior.Idle)
        {
            currentBehavior = GunslingerBehavior.Closing;
        }

        // Play hit reaction (brief stun)
        if (playHitReaction && hitStunTime > 0f)
        {
            if (hitReactCoroutine != null)
                StopCoroutine(hitReactCoroutine);
            hitReactCoroutine = StartCoroutine(HitReactRoutine());
        }

        // Trigger hit animation
        if (animator != null && !string.IsNullOrEmpty(hitAnimationTrigger))
        {
            animator.SetTrigger(hitAnimationTrigger);
        }
    }

    private IEnumerator HitReactRoutine()
    {

        yield return new WaitForSeconds(hitStunTime);
        hitReactCoroutine = null;
    }

    private void HandleDeath()
    {
        if (isDead) return;
        isDead = true;

        // Stop behavior
        currentBehavior = GunslingerBehavior.Idle;

        // Stop all coroutines
        if (hitReactCoroutine != null)
        {
            StopCoroutine(hitReactCoroutine);
            hitReactCoroutine = null;
        }

        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        // Stop navigation
        if (disableNavOnDeath && navigation != null)
        {
            navigation.SetDestination(transform.position);
            navigation.updatePosition = false;
            navigation.enabled = false;
        }

        // Stop movement follow
        if (movementScript != null)
        {
            movementScript.DisableNavmeshFollow();
        }

        // Disable weapon attack
        if (disableWeaponAttackOnDeath && enemyRangedAttack != null)
        {
            enemyRangedAttack.enabled = false;
        }

        // Disable colliders to prevent corpse from blocking or taking more hits
        if (disableBodyCollidersOnDeath)
        {
            if (bodyCollider != null)
                bodyCollider.enabled = false;

            // Disable all colliders on body and children
            if (body != null)
            {
                Collider[] colliders = body.GetComponentsInChildren<Collider>();
                foreach (Collider col in colliders)
                {
                    col.enabled = false;
                }
            }
        }

        // Trigger death animation
        if (animator != null && !string.IsNullOrEmpty(deathAnimationTrigger))
        {
            animator.SetTrigger(deathAnimationTrigger);
        }

        if (destroyEnemyRootOnDeath)
        {
            GameObject root = transform.root != null ? transform.root.gameObject : gameObject;
            Destroy(root, Mathf.Max(0f, destroyDelay));
        }
    }

    public enum GunslingerBehavior
    {
        Idle,
        Closing,
        Shooting,
        Reloading,
        Escaping
    }
}