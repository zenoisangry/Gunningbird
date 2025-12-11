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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        FollowNavAgent();
        transform.position += new Vector3(horizontalShift.x, verticalShift, horizontalShift.y);
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
}