using UnityEngine;
using static PostRunManager;

public class CuteObject : MonoBehaviour
{
    public PostRunManager.EligibleObject objectType;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void AddDecoration(GameObject decoration, Vector3 worldCoords, Quaternion givenRotation)
    {
        Vector3 relativeCoords = worldCoords - transform.position;
        Object.Instantiate(decoration, worldCoords, givenRotation, transform);
        PostRunManager.Instance.SpawnDecoration(objectType, relativeCoords, givenRotation, decoration);
    }
}
