using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

public class FeralColonistMovement : MonoBehaviour
{
    public GameObject navAimPoint;

    [Header("Navigation variables")]
    public float horizontalConstraint;
    public float verticalOffset;

    private Vector2 horizontalShift;
    private float verticalShift;
    private Vector3 navmeshResetPoint;
    private Rigidbody rb;
    private Vector3 prevTarget;

    private bool navmeshFollow = false;

    public float navForwardOffset = 0;

    public bool climbing;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        navmeshResetPoint = new Vector3 (navForwardOffset,-verticalOffset,0);
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (navmeshFollow)
        {
            FollowNavAgent();
            transform.position += new Vector3(horizontalShift.x, verticalShift, horizontalShift.y);
        }
    }

    public void RotateTowardsTarget(Vector3 target)
    {
        if (target != prevTarget)
        {
            rb.freezeRotation = false;
            rb.MoveRotation(Quaternion.LookRotation(new Vector3(target.x, 0, target.z) - new Vector3(transform.position.x, 0, transform.position.z)));
            prevTarget = target;
            rb.freezeRotation = true;
        }
    }
    private void FollowNavAgent()
    {
        verticalShift = navAimPoint.transform.position.y - (transform.position.y - verticalOffset);
        horizontalShift = new Vector2(navAimPoint.transform.position.x, navAimPoint.transform.position.z) - new Vector2(transform.position.x -navForwardOffset, transform.position.z);
        if (horizontalShift.magnitude > horizontalConstraint)
        {
            horizontalShift = horizontalShift - horizontalShift * (horizontalConstraint/horizontalShift.magnitude);
        }
        else
        {
            horizontalShift = Vector2.zero;
        }
    }

    IEnumerator AdjustClimb()
    {
        bool positionFound = false;
        Vector3 leftPos;
        Vector3 rightPos;
        while (!positionFound)
        {
            RaycastHit tempHit;
            Physics.Raycast(transform.position + (Vector3.left*0.1f), Vector3.forward, out tempHit, 1f);
            if (tempHit.collider == null)
            {
                leftPos = Vector3.zero;
            }
            else
            {
                leftPos = transform.position + (Vector3.left * 0.1f) + Vector3.forward*tempHit.distance;
            }
            Physics.Raycast(transform.position + (Vector3.right * 0.1f), Vector3.forward, out tempHit, 1f);
            if (tempHit.collider == null)
            {
                rightPos = Vector3.zero;
            }
            else
            {
                rightPos = transform.position + (Vector3.right * 0.1f) + Vector3.forward * tempHit.distance;
            }

            if (leftPos == Vector3.zero && rightPos == Vector3.zero)
            {
                Debug.Log("Flip called");
                RotateTowardsTarget(transform.position + Vector3.back);
            }
            else if (rightPos == Vector3.zero)
            {
                Debug.Log("Rotating towards the left. Old rotation = " + rb.rotation.eulerAngles);
                rb.MoveRotation(transform.rotation * Quaternion.Euler(new Vector3(0,10,0)));
                Debug.Log("New rotation = " + rb.rotation.eulerAngles);
            }
            else if (leftPos == Vector3.zero)
            {
                Debug.Log("Rotating towards the right. Old rotation = " + rb.rotation.eulerAngles);
                rb.MoveRotation(transform.rotation * Quaternion.Euler(new Vector3(0, -10, 0)));
                Debug.Log("New rotation = " + rb.rotation.eulerAngles);
            }
            else
            {
                Debug.Log("Zoning in on correct rotation. Old rotation = " + rb.rotation.eulerAngles);
                Vector3 workingAngle = leftPos - rightPos;
                workingAngle = Quaternion.AngleAxis(90, Vector3.up) * workingAngle;
                rb.MoveRotation(Quaternion.LookRotation(workingAngle));
                positionFound = true;
                Debug.Log("New rotation = " + rb.rotation.eulerAngles);
            }
            yield return null;
        }
        yield return null;
    }

    public void StartClimbing()
    {
        StartCoroutine(AdjustClimb());
    }

    public void EnableNavmeshFollow()
    {
        navmeshFollow = true;
        ResetNavAgent();
    }

    public void DisableNavmeshFollow()
    {
        navmeshFollow = false;
    }

    public void ResetNavAgent()
    {
        navAimPoint.transform.position = transform.position + navmeshResetPoint;
    }
}