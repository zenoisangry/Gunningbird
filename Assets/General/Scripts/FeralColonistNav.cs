using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class FeralColonistNav : MonoBehaviour
{
    public PlayerInput player;
    public GameObject body;
    private NavMeshAgent navigation;
    private float playerDistance;
    private Rigidbody jumpRB;
    public FeralColonistBehavior currentBehavior = FeralColonistBehavior.Idle;

    [Header("AI variables")]
    public float aggroRange;
    public float losAggroRange;
    public float jumpRange;
    public float jumpStrength;
    public float jumpChargeTime;
    public float meleeAttackRange;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        navigation = GetComponent<NavMeshAgent>();
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
                    StartCoroutine(Jump());
                }
            }

                //Fai passare il feral colonist da closing ad attacking
            //if ((player.transform.position - body.transform.position).magnitude <= meleeAttackRange)
            //{
            //
            //}
        }
    }

    private IEnumerator Jump()
    {
        float timer = 0;
        while (timer < jumpChargeTime)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        //Effettua il jump fisico calcolando la traiettoria.

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
