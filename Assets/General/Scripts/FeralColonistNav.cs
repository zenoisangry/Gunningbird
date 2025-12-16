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
    private Rigidbody jumpRB;
    private BoxCollider bodyCollider;
    private FeralColonistMovement movementScript;
    private NavMeshAgent navigation;
    private float playerDistance;

    [Header("AI variables")]
    public float aggroRange;
    public float losAggroRange;
    public float jumpRange;
    public float jumpOvershootSpeed;
    public float jumpChargeTime;
    public float jumpAbortTimer;
    public float meleeAttackRange;

    public FeralColonistBehavior currentBehavior = FeralColonistBehavior.Idle;
    private NavMeshHit hit;
    private bool checkForGround = false;
    private bool canJump = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        navigation = GetComponent<NavMeshAgent>();
        jumpRB = body.GetComponent<Rigidbody>();
        bodyCollider = body.GetComponent<BoxCollider>();
        movementScript = body.GetComponent<FeralColonistMovement>();
    }

    // Update is called once per frame
    void Update()
    {
        if (navigation.updatePosition == true)
        {
            MovementKindCheck();
        }
        BehaviorSwitchCheck();
        if (currentBehavior == FeralColonistBehavior.Closing)
        {
            navigation.SetDestination(player.projectedPosition);
        }
    }
    private void MovementKindCheck()
    {
        {
            NavMesh.SamplePosition(transform.position, out hit, 0.1f, NavMesh.AllAreas);
            if (hit.mask == 1)
                canJump = true;
            else
                canJump = false;
        }
    }
    private void BehaviorSwitchCheck()
    {
        playerDistance = (player.transform.position - transform.position).magnitude;
        switch (currentBehavior)
        {
            case FeralColonistBehavior.Idle:
                //Aggro based on range or line of sight
                if ((CheckLOS() && playerDistance <= losAggroRange) || playerDistance <= aggroRange)
                {
                    currentBehavior = FeralColonistBehavior.Closing;
                }
                break;
            case FeralColonistBehavior.Closing:
                //Go to attacking
                if (playerDistance <= meleeAttackRange)
                {
                    currentBehavior = FeralColonistBehavior.Attacking;
                }
                else
                //Go to jumping
                if ((player.height > meleeAttackRange + 1) && playerDistance <= jumpRange && CheckLOS() && canJump)
                {
                    //Second check = la distanza del giocatore dal terreno è maggiore della distanza del feral colonist dal punto proiettato (aka l'angolo del salto è >45)
                    if (player.height > ((player.transform.position - new Vector3(0, player.height, 0)) - transform.position).magnitude)
                    {
                        currentBehavior = FeralColonistBehavior.Jumping;
                        navigation.SetDestination(transform.position);
                        navigation.updatePosition = false;
                        StartCoroutine(Jump());
                    }
                }
                else
                //Go to Escaping
                if ((player.height > jumpRange))
                {
                    currentBehavior = FeralColonistBehavior.Escaping;
                }
                break;
            case FeralColonistBehavior.Attacking:
                //Go back to closing
                if (playerDistance > meleeAttackRange)
                {
                    currentBehavior = FeralColonistBehavior.Closing;
                }
                break;
            case FeralColonistBehavior.Jumping:
                //Go back to closing on landing
                if (checkForGround)
                {
                    if (Physics.BoxCast(body.transform.position, new Vector3(0.5f,0.5f,0.5f), Vector3.down, Quaternion.identity, 0.51f, LayerMask.GetMask("Default")))
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
                //Escape logic still missing
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
            if (!CheckLOS())
            {
                abortTimer += Time.deltaTime;
            }
            else
            {
                abortTimer = 0;
            }
            if (abortTimer >= jumpAbortTimer)
            {
                checkForGround = true;
                yield break;
            }
            timer += Time.deltaTime;
            yield return null;
        }
        float jumpDuration = playerDistance / (jumpRange* (1+jumpOvershootSpeed/10));
        float lostHeight = (float)Math.Pow(jumpDuration,2) * Physics.gravity.y/2;
        float targetVerticalVelocity = (player.height - lostHeight) /jumpDuration;
        Vector2 horizontalForce = new Vector2((player.transform.position.x - transform.position.x)/jumpDuration, (player.transform.position.z - transform.position.z)/jumpDuration);
        
        jumpRB.linearVelocity = new Vector3(horizontalForce.x, targetVerticalVelocity, horizontalForce.y);

        yield return null;

        checkForGround = true;
    }

    private bool CheckLOS()
    {
        //Se vengono creati nuovi layer di terrain, cambiare la layermask del raycast
        if (!Physics.BoxCast(body.transform.position, bodyCollider.size, player.transform.position - body.transform.position, Quaternion.identity, (player.transform.position - body.transform.position).magnitude, LayerMask.GetMask("Default")))
        {
            return true;
        }
        else return false;
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
