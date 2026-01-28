using System.Collections;
using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Unity.Mathematics;

public class FeralColonistNav : MonoBehaviour
{
    [Header("Links to other objects")]
    private PlayerInput player;
    private EscapeManager escManager;
    public GameObject body;
    public Transform meleeAimPoint;
    public CapsuleCollider jumpHitBox;
    public JumpAttackDetector jumpAttackDetector;

    private MeleeWeapon meleeAttack;
    private Rigidbody jumpRB;
    private BoxCollider bodyCollider;
    private FeralColonistMovement movementScript;
    private NavMeshAgent navigation;
    private float playerDistance;
    private HealthSystem healthSystem;
    private Animator animator;

    [Header("Enemy Attack Logic")]
    [SerializeField] private EnemyWeaponAttack enemyWeaponAttack;
    [SerializeField] private Transform attackTarget;

    [Header("AI variables")]
    public float aggroRange;
    public float losAggroRange;
    public float escapeRange;
    public float escapeRangePriority;
    public float escapeCheckCooldown;
    public float escapeSafetyPriority;
    private bool canEscape;
    public float jumpRange;
    public float jumpOvershootSpeed;
    public float jumpChargeTime;
    public float jumpAbortTimer;
    public float meleeAttackRangeMultiplier;
    private float meleeAttackRange;

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
    private float attackDelay;
    private float attackEndLag;
    private float activeFrames;
    private bool attacking = false;

    public FeralColonistBehavior currentBehavior = FeralColonistBehavior.Idle;
    private NavMeshHit hit;
    private bool checkForGround = false;
    private bool canJump = false;
    private bool isDead = false;

    private Coroutine hitReactCoroutine;
    private Coroutine attackCoroutine;
    private Coroutine jumpCoroutine;

    // Start is called once before the first execution of Update
    void Start()
    {
        //Cerca player
        player = FindFirstObjectByType<PlayerInput>();

        //Cerca luoghi di cover segnati
        escManager = FindFirstObjectByType<EscapeManager>();

        // Inizializza MeleeWeapon se presente
        meleeAttack = GetComponent<MeleeWeapon>();
        if (meleeAttack != null && meleeAimPoint != null)
        {
            meleeAimPoint.localPosition = new Vector3(0f, 0f, meleeAttack.GetWeaponData().meleeRange);
            meleeAttackRange = meleeAttack.GetWeaponData().meleeRange * meleeAttackRangeMultiplier;
            attackDelay = meleeAttack.GetWeaponData().meleeHitDelay;
            attackEndLag = meleeAttack.GetWeaponData().meleeCooldown;
        }

        // Componenti generici
        navigation = GetComponent<NavMeshAgent>();
        jumpRB = body.GetComponent<Rigidbody>();
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

        // Protezione EnemyWeaponAttack
        if (enemyWeaponAttack != null && attackTarget == null)
        {
            attackTarget = player != null ? player.transform : null;
        }

        if (jumpAttackDetector == null && jumpHitBox != null)
        {
            jumpAttackDetector = jumpHitBox.GetComponent<JumpAttackDetector>();
        }

        if (jumpAttackDetector == null)
        {
            Debug.LogWarning("[FeralColonistNav] JumpAttackDetector not found!", this);
        }
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

        if (navigation.updatePosition)
        {
            MovementKindCheck();
        }

        if (!attacking)
        {
            BehaviorSwitchCheck();
        }

        if (currentBehavior == FeralColonistBehavior.Closing)
        {
            if (player != null)
                navigation.SetDestination(player.projectedPosition);
        }

        if (currentBehavior == FeralColonistBehavior.Attacking || currentBehavior == FeralColonistBehavior.Jumping)
        {
            AttackCheck();
        }

        if (currentBehavior == FeralColonistBehavior.Escaping)
        {
            if (navigation.remainingDistance <= navigation.stoppingDistance)
            {
                currentBehavior = FeralColonistBehavior.Idle;
            }
        }
    }

    private void MovementKindCheck()
    {
        NavMesh.SamplePosition(transform.position, out hit, 0.1f, NavMesh.AllAreas);
        canJump = hit.mask == 1;
    }

    private void BehaviorSwitchCheck()
    {
        if (player == null) return;

        playerDistance = (player.transform.position - transform.position).magnitude;

        Vector3 attackPoint = meleeAimPoint != null ? meleeAimPoint.position : transform.position;
        float attackDistance = Vector3.Distance(player.transform.position, attackPoint);

        switch (currentBehavior)
        {
            case FeralColonistBehavior.Idle:
                if ((CheckLOS() && playerDistance <= losAggroRange) || playerDistance <= aggroRange)
                {
                    currentBehavior = FeralColonistBehavior.Closing;
                }
                break;

            case FeralColonistBehavior.Closing:
                if (playerDistance <= meleeAttackRange)
                {
                    navigation.SetDestination(transform.position);
                    currentBehavior = FeralColonistBehavior.Attacking;
                }
                else if ((player.height > meleeAttackRange + 1) && playerDistance <= jumpRange && CheckLOS() && canJump)
                {
                    if (player.height > ((player.transform.position - new Vector3(0, player.height, 0)) - transform.position).magnitude)
                    {
                        currentBehavior = FeralColonistBehavior.Jumping;
                        navigation.SetDestination(transform.position);
                        navigation.updatePosition = false;
                        if (jumpCoroutine != null)
                            StopCoroutine(jumpCoroutine);
                        jumpCoroutine = StartCoroutine(Jump());
                    }
                }
                else if ((player.height > jumpRange))
                {
                    FindEscapeZone();
                    currentBehavior = FeralColonistBehavior.Escaping;
                }
                break;

            case FeralColonistBehavior.Attacking:
                if (playerDistance > meleeAttackRange)
                {
                    currentBehavior = FeralColonistBehavior.Closing;
                }
                break;

            case FeralColonistBehavior.Jumping:
                if (checkForGround)
                {
                    if (Physics.BoxCast(body.transform.position, new Vector3(0.5f, 0.5f, 0.5f),
                        Vector3.down, Quaternion.identity, 0.51f, LayerMask.GetMask("Default")))
                    {
                        movementScript.EnableNavmeshFollow();
                        navigation.Warp(transform.position);
                        navigation.updatePosition = true;
                        checkForGround = false;

                        if (jumpAttackDetector != null)
                        {
                            jumpAttackDetector.DisableHitBox();
                            jumpAttackDetector.ResetAttack();
                        }
                        else if (jumpHitBox != null)
                        {
                            jumpHitBox.enabled = false;
                        }

                        currentBehavior = FeralColonistBehavior.Closing;
                    }
                }
                break;

            case FeralColonistBehavior.Escaping:
                if ((player.height < jumpRange/4*3) && ((CheckLOS() && playerDistance <= losAggroRange) || playerDistance <= aggroRange))
                {
                    currentBehavior = FeralColonistBehavior.Closing;
                }
                break;

            default:
                currentBehavior = FeralColonistBehavior.Closing;
                break;
        }
    }

    private void FindEscapeZone()
    {
        Vector3 targetZone = Vector3.zero;
        float targetQuality = 0;
        foreach (KeyValuePair<Vector3, float> zone in escManager.escapeAreas)
        {
            int coveredLines = CheckZoneLOS(zone.Key + new Vector3(0,1,0), zone.Value);
            if (coveredLines > 0)
            {
                float tempQuality = -(zone.Key - transform.position).magnitude*escapeRangePriority + coveredLines*(escapeRange/5)*escapeSafetyPriority;
                if (tempQuality > targetQuality)
                {
                    targetQuality = tempQuality;
                    targetZone = zone.Key;
                }
            }
        }
        if (targetZone == Vector3.zero)
        {
            currentBehavior = FeralColonistBehavior.Closing;
        }
        else
        {
            float maxOffset = escManager.escapeAreas[targetZone];
            Vector3 finaloffset = new Vector3(UnityEngine.Random.Range(-maxOffset, maxOffset), 0, UnityEngine.Random.Range(-maxOffset, maxOffset));
            navigation.SetDestination(targetZone + finaloffset);
        }
        StartCoroutine(EscapeCD());
        canEscape = false;
        //TODO logica per determinare in quale zona andare
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

    private int CheckZoneLOS(Vector3 zoneCoords, float zoneRadius)
    {
        int coveredSightlines = 0;
        Vector3 tempDistance;
        //Check for center
        tempDistance = player.transform.position - zoneCoords;
        if (Physics.Raycast(zoneCoords, tempDistance, tempDistance.magnitude, LayerMask.GetMask("Default", "Enemy"))) coveredSightlines += 1;
        //Check for Xpositive
        tempDistance = player.transform.position - (zoneCoords + new Vector3(zoneRadius,0,0));
        if (Physics.Raycast(zoneCoords + new Vector3(zoneRadius, 0, 0), tempDistance, tempDistance.magnitude, LayerMask.GetMask("Default", "Enemy"))) coveredSightlines += 1;
        //Check for Xnegative
        tempDistance = player.transform.position - (zoneCoords + new Vector3(-zoneRadius,0,0));
        if (Physics.Raycast(zoneCoords + new Vector3(-zoneRadius,0,0), tempDistance, tempDistance.magnitude, LayerMask.GetMask("Default", "Enemy"))) coveredSightlines += 1;
        //Check for Zpositive
        tempDistance = player.transform.position - (zoneCoords + new Vector3(0, 0, zoneRadius));
        if (Physics.Raycast(zoneCoords + new Vector3(0, 0, zoneRadius), tempDistance, tempDistance.magnitude, LayerMask.GetMask("Default", "Enemy"))) coveredSightlines += 1;
        //Check for Znegative
        tempDistance = player.transform.position - (zoneCoords + new Vector3(0, 0, -zoneRadius));
        if (Physics.Raycast(zoneCoords + new Vector3(0, 0, -zoneRadius), tempDistance, tempDistance.magnitude, LayerMask.GetMask("Default", "Enemy"))) coveredSightlines += 1;
        //TODO check se la zona è nascosta dal player o no
        Debug.Log(coveredSightlines);
        return coveredSightlines;
    }

    private IEnumerator Jump()
    {
        checkForGround = false;
        float timer = 0;
        float abortTimer = 0;
        movementScript.DisableNavmeshFollow();

        while (timer < jumpChargeTime)
        {
            if (isDead) yield break;

            if (!CheckLOS()) abortTimer += Time.deltaTime;
            else abortTimer = 0;

            if (abortTimer >= jumpAbortTimer)
            {
                checkForGround = true;
                yield break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        if (isDead) yield break;

        float jumpDuration = playerDistance / (jumpRange * (1 + jumpOvershootSpeed / 10));
        float lostHeight = (float)System.Math.Pow(jumpDuration, 2) * Physics.gravity.y / 2;
        float targetVerticalVelocity = (player.height - lostHeight) / jumpDuration;
        Vector2 horizontalForce = new Vector2(
            (player.transform.position.x - transform.position.x) / jumpDuration,
            (player.transform.position.z - transform.position.z) / jumpDuration
        );

        jumpRB.linearVelocity = new Vector3(horizontalForce.x, targetVerticalVelocity, horizontalForce.y);

        if (jumpAttackDetector != null)
        {
            jumpAttackDetector.EnableHitBox();
        }
        else if (jumpHitBox != null)
        {
            jumpHitBox.enabled = true;
        }

        yield return null;
        checkForGround = true;
        jumpCoroutine = null;
    }

    private void AttackCheck()
    {
        if (isDead) return;

        if (!attacking && enemyWeaponAttack != null && player != null)
        {
            Vector3 attackPoint = meleeAimPoint != null ? meleeAimPoint.position : transform.position;
            float attackDistance = Vector3.Distance(player.transform.position, attackPoint);

            if (attackDistance <= meleeAttackRange)
            {
                enemyWeaponAttack.SetTarget(player.transform);

                Vector3 directionToPlayer = (player.transform.position - transform.position).normalized;
                if (directionToPlayer.magnitude > 0.1f)
                {
                    if (currentBehavior == FeralColonistBehavior.Jumping)
                    {
                        transform.rotation = Quaternion.LookRotation(directionToPlayer);
                    }
                    else
                    {
                        directionToPlayer.y = 0;
                        if (directionToPlayer != Vector3.zero)
                            transform.rotation = Quaternion.LookRotation(directionToPlayer);
                    }
                }

                if (enemyWeaponAttack.CanAttack())
                {
                    enemyWeaponAttack.Attack(player.transform);
                }

                if (attackCoroutine != null)
                    StopCoroutine(attackCoroutine);
                attackCoroutine = StartCoroutine(Attack());
                attacking = true;
            }
        }
    }

    private IEnumerator Attack()
    {
        float t = 0;
        while (t < attackDelay + attackEndLag)
        {
            if (isDead) yield break;
            t += Time.deltaTime;
            yield return null;
        }
        attacking = false;
        attackCoroutine = null;
    }

    private bool CheckLOS()
    {
        if (player == null) return false;
        Debug.Log("Checking LOS");
        bool result = Physics.BoxCast(body.transform.position, bodyCollider.size/3, player.transform.position - body.transform.position,
                                Quaternion.identity, (player.transform.position - body.transform.position).magnitude, LayerMask.GetMask("Default"));
        Debug.Log("Something is blocking LOS = " + result);
        return !Physics.BoxCast(body.transform.position, bodyCollider.size/3f, player.transform.position - body.transform.position,
                                Quaternion.identity, (player.transform.position - body.transform.position).magnitude, LayerMask.GetMask("Default"));
    }

    private void HandleDamageTaken(float finalDamage)
    {
        if (isDead) return;

        // Force aggro when taking damage
        if (currentBehavior == FeralColonistBehavior.Idle)
        {
            currentBehavior = FeralColonistBehavior.Closing;
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
        // Briefly pause attack decision-making
        bool prevAttacking = attacking;
        attacking = true;

        yield return new WaitForSeconds(hitStunTime);

        // Restore attacking state only if still alive
        if (!isDead)
            attacking = prevAttacking;

        hitReactCoroutine = null;
    }

    private void HandleDeath()
    {
        if (isDead) return;
        isDead = true;

        currentBehavior = FeralColonistBehavior.Idle;
        attacking = false;

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

        if (jumpCoroutine != null)
        {
            StopCoroutine(jumpCoroutine);
            jumpCoroutine = null;
        }

        if (disableNavOnDeath && navigation != null)
        {
            navigation.SetDestination(transform.position);
            navigation.updatePosition = false;
            navigation.enabled = false;
        }

        if (movementScript != null)
        {
            movementScript.DisableNavmeshFollow();
        }

        if (disableWeaponAttackOnDeath && enemyWeaponAttack != null)
        {
            enemyWeaponAttack.enabled = false;
        }

        if (disableBodyCollidersOnDeath)
        {
            if (bodyCollider != null)
                bodyCollider.enabled = false;

            if (body != null)
            {
                Collider[] colliders = body.GetComponentsInChildren<Collider>();
                foreach (Collider col in colliders)
                {
                    col.enabled = false;
                }
            }
        }

        if (jumpAttackDetector != null)
        {
            jumpAttackDetector.DisableHitBox();
        }
        else if (jumpHitBox != null)
        {
            jumpHitBox.enabled = false;
        }

        if (jumpRB != null)
        {
            jumpRB.linearVelocity = Vector3.zero;
            jumpRB.isKinematic = true;
        }

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

    public enum FeralColonistBehavior
    {
        Idle,
        Closing,
        Attacking,
        Jumping,
        Escaping
    }
}