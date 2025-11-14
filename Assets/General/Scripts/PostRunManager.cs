using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class PostRunManager : MonoBehaviour
{
    private static PostRunManager _instance;

    public static PostRunManager Instance { get { return _instance; } }


    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }
    public List<GameObject> proxys;
    private Dictionary<EligibleObject, GameObject> proxyArchive = new Dictionary<EligibleObject, GameObject>();

    void Start()
    {
        foreach (GameObject proxy in proxys)
        {
            proxyArchive.Add(EligibleObject.cactus1, proxy);
        }
    }

    public void SpawnDecoration (EligibleObject target, Vector3 positionOffset, Quaternion rotation, GameObject decorationType)
    {
        Object.Instantiate(decorationType, proxyArchive[target].transform.position + positionOffset, rotation, proxyArchive[target].transform);
    }

    public enum EligibleObject
    {
        barrel,
        cactus1,
        cactus2,
        cactus3,
        cowboyhat,
        shovel,
        tntbox,
        pickaxe,
        wheel
    }
}