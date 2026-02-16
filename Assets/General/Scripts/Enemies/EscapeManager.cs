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
            foreach (Transform child2 in child.transform)
            {
                if (child2.GetComponent<EscapePoint>() != null)
                {
                    escapeAreas.Add(child2.position, child2.gameObject.GetComponent<EscapePoint>().areaRadius);
                }
            }
        }
    }

    public int CheckZoneLOS(GameObject player, Vector3 zoneCoords, float zoneRadius)
    {
        int coveredSightlines = 0;
        Vector3 tempDistance;
        //Check for center
        tempDistance = player.transform.position - zoneCoords;
        if (Physics.Raycast(zoneCoords, tempDistance, tempDistance.magnitude, LayerMask.GetMask("Default", "Enemy", "Weapon", "Projectile"))) coveredSightlines += 1;
        //Check for Xpositive
        tempDistance = player.transform.position - (zoneCoords + new Vector3(zoneRadius, 0, 0));
        if (Physics.Raycast(zoneCoords + new Vector3(zoneRadius, 0, 0), tempDistance, tempDistance.magnitude, LayerMask.GetMask("Default", "Enemy", "Weapon", "Projectile"))) coveredSightlines += 1;
        //Check for Xnegative
        tempDistance = player.transform.position - (zoneCoords + new Vector3(-zoneRadius, 0, 0));
        if (Physics.Raycast(zoneCoords + new Vector3(-zoneRadius, 0, 0), tempDistance, tempDistance.magnitude, LayerMask.GetMask("Default", "Enemy", "Weapon", "Projectile"))) coveredSightlines += 1;
        //Check for Zpositive
        tempDistance = player.transform.position - (zoneCoords + new Vector3(0, 0, zoneRadius));
        if (Physics.Raycast(zoneCoords + new Vector3(0, 0, zoneRadius), tempDistance, tempDistance.magnitude, LayerMask.GetMask("Default", "Enemy", "Weapon", "Projectile"))) coveredSightlines += 1;
        //Check for Znegative
        tempDistance = player.transform.position - (zoneCoords + new Vector3(0, 0, -zoneRadius));
        if (Physics.Raycast(zoneCoords + new Vector3(0, 0, -zoneRadius), tempDistance, tempDistance.magnitude, LayerMask.GetMask("Default", "Enemy", "Weapon", "Projectile"))) coveredSightlines += 1;
        return coveredSightlines;
    }
}
