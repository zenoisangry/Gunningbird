using UnityEngine;
using UnityEngine.AI;

public class FeralColonistNav : MonoBehaviour
{
    public PlayerInput player;
    private NavMeshAgent navigation;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        navigation = GetComponent<NavMeshAgent>();
    }

    // Update is called once per frame
    void Update()
    {
        navigation.SetDestination(player.projectedPosition);
    }

    public enum FeralColonistBehavior
    {
        Idle,
        Closing,
        Attacking,
        Escaping
    }
}
