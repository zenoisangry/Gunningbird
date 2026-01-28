using System.Collections;
using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class CorruptedGunslingerNav : MonoBehaviour
{
    [Header("Links to other objects")]
    public PlayerInput player;
    public GameObject body;
    public Transform meleeAimPoint;
    public CapsuleCollider jumpHitBox;

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
    public float shootingMinRange;
    public float shootingMaxRange;
    public float formChangeRange;
    public float formChangeCooldown;

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

        if (currentBehavior == GunslingerBehavior.Closing)
        {
            if (player != null)
                navigation.SetDestination(player.projectedPosition);
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
                }
                break;

            case GunslingerBehavior.Shooting:
                //Read current weapon state. If empty, go to cover
                break;

            case GunslingerBehavior.Reloading:
                //When weapon is fully reloaded, go back to closing
                break;

            case GunslingerBehavior.Escaping:
                break;

            default:
                currentBehavior = GunslingerBehavior.Closing;
                break;
        }
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

    public enum GunslingerBehavior
    {
        Idle,
        Closing,
        Shooting,
        Reloading,
        Escaping
    }
}