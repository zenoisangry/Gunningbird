using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class FeralColonistNav : MonoBehaviour
{
    [Header("Links to other objects")]
    private PlayerInput player;
    private EscapeManager escManager;
    public GameObject body;
    public Transform meleeAimPoint;
    public CapsuleCollider jumpHitBox;
    public JumpAttackDetector jumpAttackDetector;
    public ProjectileAggro bulletDetection;

    private MeleeWeapon meleeAttack;
    private Rigidbody jumpRB;
    private MeshCollider bodyCollider;
    private FeralColonistMovement movementScript;
    private NavMeshAgent navigation;
    private float playerDistance;
    private float playerHorizontalDistance;
    private HealthSystem healthSystem;
    private Animator animator;
    private float stunTimer;

    [Header("Enemy Attack Logic")]
    [SerializeField] private EnemyWeaponAttack enemyWeaponAttack;
    [SerializeField] private Transform attackTarget;

    [Header("AI variables")]
    public float aggroRange;
    public float losAggroRange;
    public float aggroSpreadRange;
    public float escapeRange;
    public float escapeRangePriority;
    public float escapeCheckCooldown;
    public float escapeSafetyPriority;
    private bool canEscape = true;
    public float jumpRange;
    public float jumpOvershootSpeed;
    public float jumpChargeTime;
    public float jumpAbortTimer;
    public float meleeAttackRangeMultiplier;
    private float meleeAttackRange;
    NavMeshPath path;


    [Header("Damage / Death Reactions")]
    [SerializeField] private bool playHitReaction = true;
    [SerializeField] private float hitStunTime = 0.15f;
    [SerializeField] private string hitAnimationTrigger = "Hit";
    [SerializeField] private string deathAnimationTrigger = "Die";
    [SerializeField] private float destroyDelay = 2f;

    //Attack variables
    private float attackDelay;
    private float attackEndLag;
    private bool attacking = false;

    public FeralColonistBehavior currentBehavior = FeralColonistBehavior.Idle;
    private NavMeshHit hit;
    private bool checkForGround = false;
    private bool canJump = false;
    private bool isDead = false;

    private Coroutine hitReactCoroutine;
    private Coroutine attackCoroutine;
    private Coroutine jumpCoroutine;

    private bool hasLOS;
    private int playerRange;

    private Vector3 previousPlayerPosition;
    private Vector3 previousPlayerProjection;
    [SerializeField] private float rotationChangeAngle;
    [SerializeField] private float navChangeMaxDistance;
    [SerializeField] private float navChangeMinDistance;
    private float playerAngle;
    private bool updateRotation = true;
    private bool updateNavigation = true;
    private Vector3 previousPlayerRotation;
    private Vector3 previousNavTarget = Vector3.zero;

    private float attackDistance;
    private Vector3 attackPoint;
    private bool climbing;

    // Start is called once before the first execution of Update
    void Start()
    {
        path = new NavMeshPath();

        //Cerca player
        player = FindAnyObjectByType<PlayerInput>();

        //Cerca luoghi di cover segnati
        escManager = FindAnyObjectByType<EscapeManager>();

        // Inizializza MeleeWeapon se presente
        meleeAttack = GetComponent<MeleeWeapon>();
        if (meleeAttack != null && meleeAimPoint != null)
        {
            meleeAttackRange = meleeAttack.GetWeaponData().meleeRange * meleeAttackRangeMultiplier;
            attackDelay = meleeAttack.GetWeaponData().meleeHitDelay;
            attackEndLag = meleeAttack.GetWeaponData().meleeCooldown;
        }

        // Componenti generici
        navigation = GetComponent<NavMeshAgent>();
        jumpRB = body.GetComponent<Rigidbody>();
        bodyCollider = body.GetComponent<MeshCollider>();
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

    private void CheckPlayerPosition()
    {
        playerDistance = (player.transform.position - transform.position).magnitude;
        playerHorizontalDistance = new Vector2(player.transform.position.x - transform.position.x, player.transform.position.z - transform.position.z).magnitude;
        if(playerDistance <= meleeAttackRange)
        {
            playerRange = 0;
        }
        else if (playerDistance <= aggroRange)
        {
            playerRange = 1;
        } else if (playerDistance <= losAggroRange)
        {
            playerRange = 2;
        } else if (playerDistance > losAggroRange)
        {
            playerRange = 3;
        }

        playerAngle = Math.Abs(Vector3.SignedAngle(player.transform.position - transform.position, previousPlayerRotation, Vector3.zero));
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
            if ((player.projectedPosition - previousPlayerProjection).magnitude >= (playerDistance/aggroRange)*(navChangeMaxDistance-navChangeMinDistance)+navChangeMinDistance)
            {
                updateNavigation = true;
            }
        }

        if (navigation.steeringTarget != previousNavTarget)
        {
            updateRotation = true;
            previousNavTarget = navigation.steeringTarget;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isDead) return;

        CheckPlayerPosition();
        hasLOS = CheckLOS();

        if (!attacking)
        {
            BehaviorSwitchCheck();
        }

        if (navigation.updatePosition && currentBehavior == FeralColonistBehavior.Closing)
        {
            MovementKindCheck();
        }

        if (currentBehavior == FeralColonistBehavior.Attacking || currentBehavior == FeralColonistBehavior.Jumping)
        {
            AttackCheck();
        }

        if (currentBehavior == FeralColonistBehavior.Closing)
        {
            if (navigation.path.corners.Length == 2)
            {
                movementScript.RotateTowardsTarget(player.transform.position);
                updateRotation = false;
            }
            if (updateRotation)
            {
                previousPlayerRotation = player.transform.position - transform.position;
                movementScript.RotateTowardsTarget(navigation.steeringTarget);
                updateRotation = false;
            }
            if (player != null)
            {
                //Check current surface;
                navigation.SamplePathPosition(NavMesh.AllAreas, 0.1f, out hit);
                if ((1 << NavMesh.GetAreaFromName("Climb") & hit.mask) == 0)
                {
                    climbing = false;
                    if (animator.GetCurrentAnimatorStateInfo(0).IsName("Climb"))
                    {
                        animator.Play("Chase");
                    }
                }
                else
                {
                    climbing = true;
                    if (animator.GetCurrentAnimatorStateInfo(0).IsName("Chase"))
                    {
                        animator.Play("Climb");
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
        }

        if (currentBehavior == FeralColonistBehavior.Escaping)
        {
            if (navigation.remainingDistance <= navigation.stoppingDistance)
            {
                Debug.Log("Escape target reached");
                currentBehavior = FeralColonistBehavior.Idle;
                animator.Play("Idle");
                movementScript.DisableNavmeshFollow();
                bulletDetection.Activate();
            }
        }

        //Ruota il corpo
        if (currentBehavior == FeralColonistBehavior.Attacking)
        {
            if (updateRotation)
            {
                previousPlayerRotation = player.transform.position - transform.position;
                movementScript.RotateTowardsTarget(player.transform.position);
                updateRotation = false;
            }
        }
    }

    private void MovementKindCheck()
    {
        NavMesh.SamplePosition(transform.position, out hit, 0.1f, NavMesh.AllAreas);
        canJump = hit.mask == 1;
    }

    private void CallOthers()
    {
        Collider[] hit = Physics.OverlapSphere(transform.position, aggroSpreadRange, LayerMask.GetMask("Enemy"));
        foreach (Collider collider in hit)
        {
            collider.gameObject.transform.parent.GetComponentInChildren<ProjectileAggro>().awake = true;
        }
    }

    private void BehaviorSwitchCheck()
    {
        if (player == null) return;

        attackPoint = meleeAimPoint != null ? meleeAimPoint.position : transform.position;
        attackDistance = Vector3.Distance(player.transform.position, attackPoint);

        switch (currentBehavior)
        {
            case FeralColonistBehavior.Idle:
                if ((hasLOS && playerRange == 2) || playerRange < 2 || bulletDetection.awake)
                {
                    currentBehavior = FeralColonistBehavior.Closing;
                    //movementScript.RotateTowardsTarget(player.transform.position);
                    movementScript.EnableNavmeshFollow();
                    CallOthers();
                    animator.Play("Chase");
                    navigation.isStopped = false;
                }
                break;

            case FeralColonistBehavior.Closing:
                if (playerRange == 0)
                {
                    navigation.isStopped = true;
                    currentBehavior = FeralColonistBehavior.Attacking;
                    animator.Play("Attack");
                }
                else  if ((player.height > meleeAttackRange + 1) && playerDistance <= jumpRange && hasLOS)
                {
                    MovementKindCheck();
                    if (canJump)
                    {
                        if (player.height > ((player.transform.position - new Vector3(0, player.height, 0)) - transform.position).magnitude)
                        {
                            currentBehavior = FeralColonistBehavior.Jumping;
                            animator.Play("JumpCharge");
                            navigation.SetDestination(transform.position);
                            navigation.updatePosition = false;
                            if (jumpCoroutine != null)
                                StopCoroutine(jumpCoroutine);
                            jumpCoroutine = StartCoroutine(Jump());
                        }
                    }
                }
                else if (canEscape)
                {
                    if (player.height > jumpRange || (player.transform.position.y - transform.position.y) > jumpRange || !navigation.CalculatePath(player.projectedPosition, path))
                    {
                        FindEscapeZone();
                    }
                }
                break;

            case FeralColonistBehavior.Attacking:
                if (playerRange > 0)
                {
                    animator.Play("Chase");
                    currentBehavior = FeralColonistBehavior.Closing;
                    //movementScript.RotateTowardsTarget(player.transform.position);
                    movementScript.EnableNavmeshFollow();
                    navigation.isStopped = false;
                }
                break;

            case FeralColonistBehavior.Jumping:
                if (checkForGround)
                {
                    if (Physics.BoxCast(body.transform.position + Vector3.up*0.1f, new Vector3(0.5f, 0.1f, 0.5f),
                        Vector3.down, Quaternion.identity, 0.2f, LayerMask.GetMask("Default", "Terrain")) || jumpRB.linearVelocity.magnitude < 0.1f)
                    {
                        NavMeshHit navHit;
                        if (NavMesh.SamplePosition(jumpRB.gameObject.transform.position, out navHit, 0.2f, navigation.areaMask) || jumpRB.linearVelocity.magnitude < 0.1f)
                        {
                            navigation.Warp(jumpRB.gameObject.transform.position);
                            navigation.updatePosition = true;
                            movementScript.EnableNavmeshFollow();
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
                            animator.Play("Chase");
                            //movementScript.RotateTowardsTarget(player.transform.position);
                            movementScript.EnableNavmeshFollow();
                            navigation.isStopped = false;
                        }
                    }
                }
                break;

            case FeralColonistBehavior.Escaping:
                if ((player.height < jumpRange/2) && hasLOS && playerHorizontalDistance <= losAggroRange)
                {
                    currentBehavior = FeralColonistBehavior.Closing;
                    //movementScript.RotateTowardsTarget(player.transform.position);
                    navigation.isStopped = false;
                }
                break;

            case FeralColonistBehavior.Stunned: break;

            default:
                currentBehavior = FeralColonistBehavior.Closing;
                //movementScript.RotateTowardsTarget(player.transform.position);
                navigation.isStopped = false;
                break;
        }
    }

    private void FindEscapeZone()
    {
        Vector3 targetZone = Vector3.zero;
        float targetQuality = -900;
        foreach (KeyValuePair<Vector3, float> zone in escManager.escapeAreas)
        {
            int coveredLines = escManager.CheckZoneLOS(player.gameObject, zone.Key + new Vector3(0,1,0), zone.Value);
            if (coveredLines > 0)
            {
                float tempQuality = +escapeRange*escapeRangePriority -((zone.Key - transform.position).magnitude)*escapeRangePriority + coveredLines*(escapeRange/5)*escapeSafetyPriority;
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
            currentBehavior = FeralColonistBehavior.Escaping;
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

    private IEnumerator Jump()
    {
        canJump = false;
        checkForGround = false;
        float timer = 0;
        float abortTimer = 0;
        movementScript.DisableNavmeshFollow();

        while (timer < jumpChargeTime)
        {
            if (isDead) yield break;
            if (!hasLOS) abortTimer += Time.deltaTime;
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

        yield return new WaitForSeconds(0.1f);
        checkForGround = true;
        canJump = true;
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
        if (playerHorizontalDistance <= losAggroRange)
        {
            bool result = Physics.BoxCast(body.transform.position, new Vector3(0.4f,0.4f,0.4f), player.transform.position - body.transform.position,
                                Quaternion.identity, (player.transform.position - body.transform.position).magnitude, LayerMask.GetMask("Default", "Terrain"));
            return !result;
        }
        else return false;
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
        currentBehavior = FeralColonistBehavior.Stunned;
        while (stunTimer > 0)
        {
            stunTimer -= Time.deltaTime;
            yield return null;
        }
        currentBehavior = FeralColonistBehavior.Closing;
        hitReactCoroutine = null;
    }

    private void HandleDeath()
    {
        if (isDead) return;
        isDead = true;

        if (healthSystem != null)
        {
            healthSystem.DamageTaken -= HandleDamageTaken;
            healthSystem.Died -= HandleDeath;
        }

        if (transform.parent != null)
        {
            transform.parent.gameObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public enum FeralColonistBehavior
    {
        Idle,
        Closing,
        Attacking,
        Jumping,
        Escaping,
        Stunned
    }
}