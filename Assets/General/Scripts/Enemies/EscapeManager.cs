using System;
using System.Collections;
using UnityEngine;
using Unity.VisualScripting;
using System.Collections.Generic;

public class EscapeManager : MonoBehaviour
{
    public Dictionary<Vector3, float> escapeAreas = new Dictionary<Vector3, float>();

    private void Start()
    {
        foreach(Transform child in gameObject.transform)
        {
            escapeAreas.Add(child.position, child.gameObject.GetComponent<EscapePoint>().areaRadius);
        }
    }

}
