using UnityEngine;
using UnityEngine.InputSystem;

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

    private Vector2 movementDirection;
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

        //Wind movement
        movementDirection = Quaternion.AngleAxis(lookDirection.eulerAngles.y, Vector3.back) * actions.FindAction("Player/Move").ReadValue<Vector2>();
        horizontalVelocity = new Vector2(body.linearVelocity.x, body.linearVelocity.z);
        if (horizontalVelocity.magnitude < windSpeed && movementDirection != Vector2.zero)
        {
            Debug.Log(movementDirection);
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
    }

    void Jump(InputAction.CallbackContext ctx)
    {
        if (grounded && jumpTimer <= 0)
        {
            jumpTimer = jumpCD;
            grounded = false;
            body.AddForce(Vector3.up * (20 + jumpStrength * body.linearVelocity.magnitude * jumpScaling));
            Debug.Log(jumpStrength * body.linearVelocity.magnitude);
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
