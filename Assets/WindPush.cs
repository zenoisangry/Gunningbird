using UnityEngine;

public class WindPush : MonoBehaviour
{
    public float windStrength;
    public float windSpeed;
    private Rigidbody body;
    private Vector2 horizontalVelocity;
    private float velocityDampening;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        body = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        horizontalVelocity = new Vector2(body.linearVelocity.x, body.linearVelocity.z);
        if (horizontalVelocity.magnitude < windSpeed)
        {
            if (horizontalVelocity.magnitude > 1)
            {
                velocityDampening = Mathf.Sqrt((windSpeed - horizontalVelocity.magnitude)/windSpeed);
                body.AddForce(new Vector3(windStrength*velocityDampening, 0f, windStrength*velocityDampening));
            }
            else
            {
                body.AddForce(new Vector3(windStrength, 0.0f, windStrength));
            }

        }
    }
}
