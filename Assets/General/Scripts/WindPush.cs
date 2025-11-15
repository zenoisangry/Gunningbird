using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
public class WindPush : MonoBehaviour
{
    public float windStrength;
    public float windSpeed;
    public float jumpStrength;
    private Rigidbody body;
    private Vector2 horizontalVelocity;
    private Vector2 calculatedDirection;
    private float velocityDampening;
    public InputActionAsset actions;
    public Transform lookDirection;
    private bool grounded = true;
    public float jumpCD;
    private float jumpTimer;
    public float jumpScaling;
    private RaycastHit cuteTargets;
    public float cuteDetectionRange;
    public float downwardsCuteDetection;
    private Collider[] nearbyObjects;

    private Vector2 movementDirection;

    public GameObject currentDecoration;
    public List<GameObject> decorationList;
    void Start()
    {
        body = GetComponent<Rigidbody>();
        actions.FindAction("Player/Jump").started += Jump;
    }

    void Update()
    {
        //Detect ground
        if (jumpTimer > 0)
        {
            jumpTimer -= Time.deltaTime;
        }

        //Detect decorations in the environment
        nearbyObjects = Physics.OverlapSphere(transform.position, cuteDetectionRange);
        foreach (Collider collider in nearbyObjects)
        {
            if (collider.gameObject.layer == 7 && currentDecoration == null)
            {
                currentDecoration = collider.gameObject;
                currentDecoration.GetComponent<BoxCollider>().enabled = false;
                currentDecoration.layer = 0;
                currentDecoration.transform.SetParent(transform, false);
                currentDecoration.transform.localPosition = Vector3.zero;
            }
        }

        //Wind movement
        movementDirection = Quaternion.AngleAxis(lookDirection.eulerAngles.y, Vector3.back) * actions.FindAction("Player/Move").ReadValue<Vector2>();
        horizontalVelocity = new Vector2(body.linearVelocity.x, body.linearVelocity.z);
        if (horizontalVelocity.magnitude < windSpeed && movementDirection != Vector2.zero)
        {
            if (horizontalVelocity.magnitude > 1)
            {
                calculatedDirection = (movementDirection.normalized + horizontalVelocity.normalized)/2;
                //Get movementDirection, then add to horizontal velocity. get "forwards" part of the vector.
                //if forward magnitude is 1 (same direction), full dampening is applied
                velocityDampening = Mathf.Sqrt((windSpeed - horizontalVelocity.magnitude*calculatedDirection.magnitude)/windSpeed);
                //Fai in modo che il magnitude diventi moltiplicatore "percentuale" dell'effetto del velocity dampening
                movementDirection = movementDirection * windStrength * velocityDampening;
                body.AddForce(new Vector3(movementDirection.x, 0f, movementDirection.y));
            }
            else
            {
                body.AddForce(new Vector3(movementDirection.x, 0f, movementDirection.y));
            }
        }

        bool found = false;
        //Detect cutifiable objects below
        Physics.SphereCast(transform.position + 2*Vector3.up, cuteDetectionRange, downwardsCuteDetection*Vector3.down, out cuteTargets, 4f);
        if (cuteTargets.collider != null)
        {
            if (cuteTargets.collider.gameObject.layer == 6)
            {
                found = true;
            }
        }
        //Detect cutifiable objects in front
        if (!found)
        {
            Physics.SphereCast(transform.position + 2 * Vector3.back, cuteDetectionRange, Vector3.forward, out cuteTargets, 4f);
            if (cuteTargets.collider != null)
            {
                if (cuteTargets.collider.gameObject.layer == 6)
                {
                    found = true;
                }
            }
        }
        //Detect cutifiable objects on the left
        if (!found)
        {
            Physics.SphereCast(transform.position + 2 * Vector3.right, cuteDetectionRange, Vector3.left, out cuteTargets, 4f);
            if (cuteTargets.collider != null)
            {
                if (cuteTargets.collider.gameObject.layer == 6)
                {
                    found = true;
                }
            }
        }
        //Detect cutifiable objects on the right
        if (!found)
        {
            Physics.SphereCast(transform.position + 2 * Vector3.left, cuteDetectionRange, Vector3.right, out cuteTargets, 4f);
            if (cuteTargets.collider != null)
            {
                if (cuteTargets.collider.gameObject.layer == 6)
                {
                    found = true;
                }
            }
        }
        //Detect cutifiable objects on the back
        if (!found)
        {
            Physics.SphereCast(transform.position + 2 * Vector3.forward, cuteDetectionRange, Vector3.back, out cuteTargets, 4f);
            if (cuteTargets.collider != null)
            {
                if (cuteTargets.collider.gameObject.layer == 6)
                {
                    found = true;
                }
            }
        }

        if (found)
        {
            Vector2 orientation = new Vector2(transform.position.x, transform.position.z) - new Vector2(cuteTargets.point.x, cuteTargets.point.z);
            Debug.Log("Cute target found");
            if (currentDecoration != null)
            {
                cuteTargets.collider.gameObject.GetComponent<CuteObject>().AddDecoration(currentDecoration, cuteTargets.point, Quaternion.LookRotation(new Vector3(orientation.x, 0, orientation.y), Vector3.up));
                Destroy(currentDecoration);
                currentDecoration = null;
            }
            else
            {
                cuteTargets.collider.gameObject.GetComponent<CuteObject>().AddDecoration(decorationList[Random.Range(0, 4)], cuteTargets.point, Quaternion.LookRotation(new Vector3(orientation.x, 0, orientation.y), Vector3.up));
            }
            cuteTargets.collider.gameObject.layer = 0;
        }
    }

    void Jump(InputAction.CallbackContext ctx)
    {
        if (grounded && jumpTimer <= 0)
        {
            jumpTimer = jumpCD;
            grounded = false;
            body.AddForce(Vector3.up * (20 + jumpStrength * body.linearVelocity.magnitude * jumpScaling));
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (jumpTimer<=0)
        {
            grounded = true;
        }
    }
}
