using Unity.Cinemachine;
using UnityEngine;

public class CameraStretcher : MonoBehaviour
{
    public CinemachineCamera followCamera;
    public CinemachineThirdPersonFollow followArm;
    private Rigidbody body;

    private float baseSpeed;
    private float targetSpeed;
    private float currentSpeed;
    public float cameraAccelCap;
    private float cameraAcceleration;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        body = GetComponent<Rigidbody>();
        baseSpeed = followCamera.Lens.FieldOfView;
        currentSpeed = baseSpeed;
        cameraAcceleration = cameraAccelCap / 20;
    }

    // Update is called once per frame
    void Update()
    {
        targetSpeed = baseSpeed + new Vector2(body.linearVelocity.x, body.linearVelocity.z).magnitude;
        if (currentSpeed < targetSpeed)
        {
            if (currentSpeed + cameraAcceleration >= targetSpeed)
            {
                currentSpeed = targetSpeed;
            }
            else
            {
                currentSpeed += cameraAcceleration;
            }
        }
        else if (currentSpeed > targetSpeed)
        {
            if (currentSpeed - cameraAcceleration <= targetSpeed)
            {
                currentSpeed = targetSpeed;
            }
            else
            {
                currentSpeed -= cameraAcceleration;
            }
        }
        followCamera.Lens.FieldOfView = currentSpeed;
        followArm.CameraDistance = currentSpeed/10;
    }
}
