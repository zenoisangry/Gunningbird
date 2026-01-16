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

    //Attack variables
    private float attackDelay;
    private float attackEndLag;
    private float activeFrames;
    private bool attacking = false;

    public FeralColonistBehavior currentBehavior = FeralColonistBehavior.Idle;
    private NavMeshHit hit;
    private bool checkForGround = false;
    private bool canJump = false;

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

        // Protezione EnemyWeaponAttack
        if (enemyWeaponAttack != null && attackTarget == null)
        {
            attackTarget = player != null ? player.transform : null;
        }
    }

    // Update is called once per frame
    void Update()
    {
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
                        StartCoroutine(Jump());
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

        float jumpDuration = playerDistance / (jumpRange * (1 + jumpOvershootSpeed / 10));
        float lostHeight = (float)Math.Pow(jumpDuration, 2) * Physics.gravity.y / 2;
        float targetVerticalVelocity = (player.height - lostHeight) / jumpDuration;
        Vector2 horizontalForce = new Vector2((player.transform.position.x - transform.position.x) / jumpDuration,
                                              (player.transform.position.z - transform.position.z) / jumpDuration);

        jumpRB.linearVelocity = new Vector3(horizontalForce.x, targetVerticalVelocity, horizontalForce.y);
        yield return null;
        checkForGround = true;
    }

    private void AttackCheck()
    {
        if (!attacking)
        {
            if (enemyWeaponAttack != null && attackTarget != null && enemyWeaponAttack.CanAttack())
            {
                enemyWeaponAttack.SetTarget(attackTarget);
                enemyWeaponAttack.Attack(attackTarget);
            }
            StartCoroutine(Attack());
            attacking = true;
        }
    }

    private IEnumerator Attack()
    {
        float t = 0;
        while (t < attackDelay + attackEndLag)
        {
            t += Time.deltaTime;
            yield return null;
        }
        attacking = false;
    }

    private bool CheckLOS()
    {
        if (player == null) return false;

        return !Physics.BoxCast(body.transform.position, bodyCollider.size, player.transform.position - body.transform.position,
                                Quaternion.identity, (player.transform.position - body.transform.position).magnitude, LayerMask.GetMask("Default"));
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