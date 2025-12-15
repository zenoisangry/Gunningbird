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
    private FeralColonistMovement movementScript;
    private NavMeshAgent navigation;

    [Header("AI variables")]
    public float aggroRange;
    public float losAggroRange;
    public float jumpRange;
    public float jumpOvershootSpeed;
    public float jumpChargeTime;
    public float meleeAttackRange;

    private FeralColonistBehavior currentBehavior = FeralColonistBehavior.Idle;

    
    private float playerDistance;
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        navigation = GetComponent<NavMeshAgent>();
        jumpRB = body.GetComponent<Rigidbody>();
        movementScript = body.GetComponent<FeralColonistMovement>();
    }

    // Update is called once per frame
    void Update()
    {
        BehaviorSwitchCheck();
        if (currentBehavior == FeralColonistBehavior.Closing)
        {
            navigation.SetDestination(player.projectedPosition);
        }
    }

    private void BehaviorSwitchCheck()
    {
        playerDistance = (player.transform.position - body.transform.position).magnitude;
        //Fai passare il feral colonist da idle a closing
        if (currentBehavior == FeralColonistBehavior.Idle)
        {
            if ((CheckLOS() && playerDistance <= losAggroRange) || playerDistance <= aggroRange)
            {
                currentBehavior = FeralColonistBehavior.Closing;
            }
        }
        //Fai passare il feral colonist da closing a jumping
        //First check = il giocatore deve essere irraggiungibile con attacco melee base e dentro il jump range
        if (currentBehavior == FeralColonistBehavior.Closing){
            if ((player.height > meleeAttackRange+1) && playerDistance <= jumpRange)
            {
                //Second check = la distanza del giocatore dal terreno è maggiore della distanza del feral colonist dal punto proiettato (aka l'angolo del salto è >45)
                if (player.height > playerDistance){
                    currentBehavior = FeralColonistBehavior.Jumping;
                    navigation.SetDestination(transform.position);
                    StartCoroutine(Jump());
                }
            }
            //Fai passare il feral colonist da closing ad attacking

        }

        //Fai tornare il feral colonist post-salto in idle/closing
        if (currentBehavior == FeralColonistBehavior.Jumping)
        {
            //Check per il grounded state (che due palle)
        }
    }

    private IEnumerator Jump()
    {
        float timer = 0;
        movementScript.DisableNavmeshFollow();
        while (timer < jumpChargeTime)
        {
            Debug.Log("Charging jump");
            timer += Time.deltaTime;
            yield return null;
        }
        float jumpDuration = playerDistance / (jumpRange* (1+jumpOvershootSpeed/10));
        float lostHeight = (float)Math.Pow(jumpDuration,2) * Physics.gravity.y/2;
        float targetVerticalVelocity = (player.height - lostHeight) /jumpDuration;
        Vector2 horizontalForce = new Vector2((player.transform.position.x - transform.position.x)/jumpDuration, (player.transform.position.z - transform.position.z)/jumpDuration);
        
        jumpRB.linearVelocity = new Vector3(horizontalForce.x, targetVerticalVelocity, horizontalForce.y);
    }

    private bool CheckLOS()
    {
        //Se vengono creati nuovi layer di terrain, cambiare la layermask del raycast
        if (!Physics.Raycast(body.transform.position, player.transform.position - body.transform.position, (player.transform.position - body.transform.position).magnitude, LayerMask.GetMask("Default")))
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
