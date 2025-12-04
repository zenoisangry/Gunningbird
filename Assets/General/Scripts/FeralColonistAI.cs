using UnityEngine;
using UnityEngine.AI;

public class FeralColonistAI : MonoBehaviour
{
    public Transform playerPosition;
    private NavMeshAgent navigation;
    public GameObject body;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        navigation = GetComponent<NavMeshAgent>();
    }

    // Update is called once per frame
    void Update()
    {
        navigation.SetDestination(playerPosition.position);
    }

    private void LateUpdate()
    {
        //body.transform.localRotation = Quaternion.Inverse(transform.localRotation);
    }
}
