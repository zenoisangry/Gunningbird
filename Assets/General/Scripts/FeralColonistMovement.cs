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

    private bool navmeshFollow = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        navmeshResetPoint = new Vector3 (0,-verticalOffset,0.5f);
    }

    // Update is called once per frame
    void Update()
    {
        if (navmeshFollow)
        {
            FollowNavAgent();
            transform.position += new Vector3(horizontalShift.x, verticalShift, horizontalShift.y);
        }
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
        navAimPoint.transform.position = transform.position + navmeshResetPoint;
    }
}