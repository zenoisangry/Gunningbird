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
    private MeshCollider bodyCollider;
    private FeralColonistMovement movementScript;
    private NavMeshAgent navigation;
    private float playerDistance;
    private HealthSystem healthSystem;
    private Animator animator;
    public ProjectileAggro bulletDetection;

    [Header("Enemy Attack Logic")]
    [SerializeField] private EnemyRangedAttack enemyRangedAttack;
    [SerializeField] private Transform attackTarget;

    [Header("AI variables")]
    public float aggroRange;
    public float losAggroRange;
    public float aggroSpreadRange;
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
    private float stunTimer = 0;

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

    private bool hasLOS;
    private int playerRange;
    private float playerHorizontalDistance;

    private Vector3 previousPlayerPosition;
    private Vector3 previousPlayerProjection;
    [SerializeField] private float rotationChangeAngle;
    [SerializeField] private float navChangeMaxDistance;
    [SerializeField] private float navChangeMinDistance;
    private float playerAngle;
    private bool updateRotation = true;
    private bool updateNavigation = true;

    public GameObject normalModel1;
    public GameObject normalModel2;
    public GameObject revolverModel;
    public GameObject smokeModel;

    private bool inSmokeForm = true;

    // Start is called once before the first execution of Update
    void Start()
    {
        player = FindAnyObjectByType<PlayerInput>();
        escManager = FindAnyObjectByType<EscapeManager>();
        navigation = GetComponent<NavMeshAgent>();
        bodyCollider = body.GetComponent<MeshCollider>();
        movementScript = body.GetComponent<FeralColonistMovement>();
        animator = body != null ? body.GetComponentInChildren<Animator>() : GetComponentInChildren<Animator>();

        climbing = false;

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
        movementScript.EnableNavmeshFollow();
        SwitchToFleshForm();
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

    private void CheckPlayerPosition()
    {
        playerDistance = (player.transform.position - transform.position).magnitude;
        playerHorizontalDistance = new Vector2(player.transform.position.x - transform.position.x, player.transform.position.z - transform.position.z).magnitude;
        if (playerDistance <= aggroRange)
        {
            playerRange = 0;
        }
        else if (playerDistance <= losAggroRange)
        {
            playerRange = 1;
        }
        else if (playerDistance > losAggroRange)
        {
            playerRange = 2;
        }

        playerAngle = Math.Abs(Vector3.SignedAngle(player.transform.position - transform.position, previousPlayerPosition - transform.position, Vector3.zero));
        if (playerAngle > rotationChangeAngle)
        {
            updateRotation = true;
        }
        if (playerDistance >= aggroRange)
        {
            if ((player.projectedPosition - previousPlayerProjection).magnitude >= navChangeMaxDistance)
            {
                updateNavigation = true;
            }
        }
        else
        {
            if ((player.projectedPosition - previousPlayerProjection).magnitude >= (playerDistance / aggroRange) * (navChangeMaxDistance - navChangeMinDistance) + navChangeMinDistance)
            {
                updateNavigation = true;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isDead) return;

        CheckPlayerPosition();
        hasLOS = CheckLOS();

        if (!climbing)
        {
            BehaviorSwitchCheck();
        }

        if (currentBehavior == GunslingerBehavior.Closing)
        {
            if (player != null)
            {
                //Check current surface;
                navigation.SamplePathPosition(NavMesh.AllAreas, 0.1f, out hit);
                if ((1 << NavMesh.GetAreaFromName("Climb") & hit.mask) == 0)
                {
                    climbing = false;
                    movementScript.climbing = false;
                    SwitchToFleshForm();
                    //AGGIUNGI switch back
                }
                else
                {
                    if (!climbing)
                    {
                        movementScript.StartClimbing();
                        climbing = true;
                        //AGGIUNGI switch
                        SwitchToSmokeForm();
                    }
                }
                if (updateNavigation)
                {
                    navigation.SetDestination(player.projectedPosition);
                    updateNavigation = false;
                    previousPlayerPosition = player.transform.position;
                    previousPlayerProjection = player.projectedPosition;
                }
            }
            if (!climbing)
            {
                if (updateRotation)
                {
                    movementScript.RotateTowardsTarget(navigation.steeringTarget);
                    updateRotation = false;
                }
            }
            else
            {
                if (navigation.steeringTarget.y > transform.position.y)
                {
                    movementScript.RotateTowardsTarget(navigation.steeringTarget);
                    updateRotation = false;
                }
                else
                {
                    movementScript.RotateTowardsTarget(transform.position - navigation.steeringTarget);
                    updateRotation = false;
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
            movementScript.RotateTowardsTarget(player.transform.position);
        }
    }

    private void CallOthers()
    {
        Collider[] hit = Physics.OverlapSphere(transform.position, aggroSpreadRange, LayerMask.GetMask("Enemy"));
        foreach (Collider collider in hit)
        {
            Debug.Log(collider.gameObject);
            if (collider.gameObject.GetComponentInChildren<ProjectileAggro>() != null)
            {
                collider.gameObject.GetComponentInChildren<ProjectileAggro>().awake = true;
            }
        }
    }

    private void SwitchToSmokeForm()
    {
        if (inSmokeForm == false)
        {
            normalModel1.SetActive(false);
            normalModel2.SetActive(false);
            revolverModel.SetActive(false);
            smokeModel.SetActive(true);
            inSmokeForm = true;
        }
    }

    private void SwitchToFleshForm()
    {
        if (inSmokeForm == true)
        {
            normalModel1.SetActive(true);
            normalModel2.SetActive(true);
            revolverModel.SetActive(true);
            smokeModel.SetActive(false);
            inSmokeForm = false;
        }
    }
    private void BehaviorSwitchCheck()
    {
        if (player == null) return;

        playerDistance = (player.transform.position - transform.position).magnitude;

        switch (currentBehavior)
        {
            case GunslingerBehavior.Idle:
                if ((CheckLOS() && playerDistance <= losAggroRange) || playerDistance <= aggroRange || bulletDetection.awake)
                {
                    currentBehavior = GunslingerBehavior.Closing;
                    CallOthers();
                    animator.Play("Walk");
                }
                break;

            case GunslingerBehavior.Closing:
                if (CheckLOS() && playerDistance > shootingMinRange && playerDistance <= shootingMaxRange){
                    currentBehavior = GunslingerBehavior.Shooting;
                    navigation.SetDestination(transform.position);
                    animator.Play("Shoot");
                }
                if (playerDistance <= shootingMinRange && canEscape)
                {
                    FindEscapeZone();
                    animator.Play("Walk");
                    currentBehavior = GunslingerBehavior.Escaping;
                }
                break;

            case GunslingerBehavior.Shooting:
                if (!CheckLOS())
                {
                    Debug.Log("[Gunslinger] Lost line of sight, reloading if possible");
                    enemyRangedAttack.Reload();
                    animator.Play("Reload");
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
                        animator.Play("Walk");
                        break;
                    }
                    else
                    if (playerDistance <= shootingMinRange && canEscape)
                    {
                        Debug.Log("[Gunslinger] Player too close, escaping");
                        FindEscapeZone();
                        animator.Play("Walk");
                        currentBehavior = GunslingerBehavior.Escaping;
                        break;
                    }
                    else
                    if (playerDistance > shootingMaxRange)
                    {
                        animator.Play("Walk");
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
                        animator.Play("Shoot");
                        Debug.Log("[Gunslinger] Reload complete, resuming shooting");
                        navigation.SetDestination(transform.position);
                        currentBehavior = GunslingerBehavior.Shooting;
                        break;
                    }
                    else
                    {
                        animator.Play("Walk");
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
                    animator.Play("Reload");
                    navigation.speed = baseSpeed;
                    //AGGIUNGI switch back
                    SwitchToFleshForm();
                }
                break;

            case GunslingerBehavior.Stunned: break;

            default:
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
                //AGGIUNGI switch
                SwitchToSmokeForm();
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
        bool result = Physics.BoxCast(weaponHolder.transform.position, new Vector3(0.3f, 0.3f, 0.3f), player.transform.position - weaponHolder.transform.position,
                                Quaternion.identity, (player.transform.position - weaponHolder.transform.position).magnitude, LayerMask.GetMask("Default", "Terrain"));
        return !result;
    }

    public void StayStill(float time)
    {
        stunTimer = time;
        hitReactCoroutine = StartCoroutine(HitReactRoutine());
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
            stunTimer += hitStunTime;
            if (hitReactCoroutine == null)
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
        navigation.SetDestination(transform.position);
        currentBehavior = GunslingerBehavior.Stunned;
        while (stunTimer > 0)
        {
            stunTimer -= Time.deltaTime;
            yield return null;
        }
        currentBehavior = GunslingerBehavior.Closing;
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
            Destroy(transform.parent.gameObject, Mathf.Max(0f, destroyDelay));
        }
    }

    public enum GunslingerBehavior
    {
        Idle,
        Closing,
        Shooting,
        Reloading,
        Escaping,
        Stunned
    }
}