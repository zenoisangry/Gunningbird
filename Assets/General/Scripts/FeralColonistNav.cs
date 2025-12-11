using UnityEngine;
using UnityEngine.AI;

public class FeralColonistNav : MonoBehaviour
{
    public PlayerInput player;
    public GameObject body;
    private NavMeshAgent navigation;
    private FeralColonistBehavior currentBehavior = FeralColonistBehavior.Idle;

    [Header("AI variables")]
    public float aggroRange;
    public float losAggroRange;

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
        if (currentBehavior == FeralColonistBehavior.Idle)
        {
            if ((CheckLOS() && (player.transform.position - body.transform.position).magnitude <= losAggroRange) || (player.transform.position - body.transform.position).magnitude <= aggroRange)
            {
                currentBehavior = FeralColonistBehavior.Closing;
            }
        }
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
        Escaping
    }
}
