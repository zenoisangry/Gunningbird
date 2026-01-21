using System.Collections;
using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class FeralColonistNav : MonoBehaviour
{
    [Header("Links to other objects")]
    public PlayerInput player;
    public GameObject body;
    public Transform meleeAimPoint;

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

        if (currentBehavior == FeralColonistBehavior.Attacking)
        {
            AttackCheck();
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
                    if (Physics.BoxCast(body.transform.position, new Vector3(0.5f, 0.5f, 0.5f), Vector3.down, Quaternion.identity, 0.51f, LayerMask.GetMask("Default")))
                    {
                        movementScript.EnableNavmeshFollow();
                        navigation.Warp(transform.position);
                        navigation.updatePosition = true;
                        checkForGround = false;
                        currentBehavior = FeralColonistBehavior.Closing;
                    }
                }
                break;

            case FeralColonistBehavior.Escaping:
                break;

            default:
                currentBehavior = FeralColonistBehavior.Closing;
                break;
        }
    }

    private IEnumerator Jump()
    {
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
        float lostHeight = (float)Math.Pow(jumpDuration, 2) * Physics.gravity.y / 2;
        float targetVerticalVelocity = (player.height - lostHeight) / jumpDuration;
        Vector2 horizontalForce = new Vector2((player.transform.position.x - transform.position.x) / jumpDuration,
                                              (player.transform.position.z - transform.position.z) / jumpDuration);

        jumpRB.linearVelocity = new Vector3(horizontalForce.x, targetVerticalVelocity, horizontalForce.y);
        yield return null;
        checkForGround = true;
        jumpCoroutine = null;
    }

    private void AttackCheck()
    {
        if (isDead) return;

        if (!attacking && enemyWeaponAttack != null && player != null)
        {
            enemyWeaponAttack.SetTarget(player.transform);

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

        return !Physics.BoxCast(body.transform.position, bodyCollider.size, player.transform.position - body.transform.position,
                                Quaternion.identity, (player.transform.position - body.transform.position).magnitude, LayerMask.GetMask("Default"));
    }

    private void HandleDamageTaken(float finalDamage)
    {
        if (isDead) return;

        // Force aggro when taking damage
        if (currentBehavior == FeralColonistBehavior.Idle || currentBehavior == FeralColonistBehavior.Escaping)
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

        // Stop behavior
        currentBehavior = FeralColonistBehavior.Idle;
        attacking = false;

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

        if (jumpCoroutine != null)
        {
            StopCoroutine(jumpCoroutine);
            jumpCoroutine = null;
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
        if (disableWeaponAttackOnDeath && enemyWeaponAttack != null)
        {
            enemyWeaponAttack.enabled = false;
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

        // Freeze jump rigidbody
        if (jumpRB != null)
        {
            jumpRB.linearVelocity = Vector3.zero;
            jumpRB.isKinematic = true;
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

    public enum FeralColonistBehavior
    {
        Idle,
        Closing,
        Attacking,
        Jumping,
        Escaping
    }
}