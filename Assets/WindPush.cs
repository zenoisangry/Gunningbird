using UnityEngine;

public class WindPush : MonoBehaviour
{
    public float windStrength;
    public float windSpeed;
    private Rigidbody body;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        body = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if (body.linearVelocity.magnitude < windSpeed)
        {
            body.AddForce(new Vector3(windStrength, 0.0f, windStrength));
        }
    }
}
