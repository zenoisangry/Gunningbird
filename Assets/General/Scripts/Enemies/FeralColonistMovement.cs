using UnityEngine;
using UnityEngine.Rendering.Universal;

public class FeralColonistMovement : MonoBehaviour
{
    public FeralColonistNav AI;
    public GameObject navAimPoint;

    [Header("Navigation variables")]
    public float horizontalConstraint;
    public float verticalOffset;

    private Vector2 horizontalShift;
    private float verticalShift;
    private Vector3 navmeshResetPoint;
    private Rigidbody rb;

    private bool navmeshFollow = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        navmeshResetPoint = new Vector3 (0,-verticalOffset,0.5f);
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
        rb.MoveRotation(Quaternion.LookRotation (new Vector3 (target.x,0,target.z) - new Vector3(transform.position.x,0,transform.position.z))*Quaternion.Euler(Vector3.up*90));
    }
    private void FollowNavAgent()
    {
        verticalShift = navAimPoint.transform.position.y - (transform.position.y - verticalOffset);
        horizontalShift = new Vector2(navAimPoint.transform.position.x, navAimPoint.transform.position.z) - new Vector2(transform.position.x, transform.position.z);
        if (horizontalShift.magnitude > horizontalConstraint)
        {
            horizontalShift = horizontalShift - horizontalShift * (horizontalConstraint/horizontalShift.magnitude);
        }
        else
        {
            horizontalShift = Vector2.zero;
        }
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
        Debug.Log("New position = " + transform.position + navmeshResetPoint);
        navAimPoint.transform.position = transform.position + navmeshResetPoint;
    }
}