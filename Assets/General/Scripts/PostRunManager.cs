using System.Collections.Generic;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

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
    public CinemachineCamera postRunCamera;
    private int currentProxy = 0;
    private List<GameObject> appliedDecorations = new List<GameObject>();
    private bool active = false;

    void Start()
    {
        InitializePostRun();
    }

    public void InitializePostRun()
    {
        foreach (GameObject proxy in proxys)
        {
            proxyArchive.Add(proxy.GetComponent<ProxyHandler>().objectType, proxy);
        }
        postRunCamera.Follow = proxys[currentProxy].transform;
        active = true;
    }
    public void SpawnDecoration (EligibleObject target, Vector3 positionOffset, Quaternion rotation, GameObject decorationType)
    {
        proxyArchive[target].GetComponent<ProxyHandler>().UpdateDecorationNumber();
        appliedDecorations.Add(Object.Instantiate(decorationType, proxyArchive[target].transform.position + positionOffset, rotation, proxyArchive[target].transform));
    }

    public void SwitchAimPoint(bool forwards)
    {
        if (forwards)
        {
            if (currentProxy < proxys.Count-1)
            {
                currentProxy += 1;
            }
        }
        else
        {
            if (currentProxy > 0)
            {
                currentProxy -= 1;
            }
        }
        postRunCamera.Follow = proxys[currentProxy].transform;
    }

    void Update()
    {
        if (active)
        {
            proxys[currentProxy].transform.Rotate(new Vector3(0, 100, 0) * Time.deltaTime);
        }
    }

    public void GoToPostRun()
    {
        active = true;
        postRunCamera.enabled = true;
        postRunCamera.Priority = 2;
        postRunCamera.Follow = proxys[currentProxy].transform;
    }

    public void EndPostRun()
    {
        active = false;
        foreach (GameObject proxy in proxys)
        {
            proxy.GetComponent<ProxyHandler>().ResetDecorationNumber();
        }
        foreach (GameObject decoration in appliedDecorations)
        {
            Destroy(decoration);
        }
        appliedDecorations = new List<GameObject>();
        postRunCamera.Priority = 0;
        postRunCamera.enabled = false;
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
        wheel,
        chair,
    }
}