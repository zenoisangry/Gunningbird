using UnityEngine;
using UnityEngine.InputSystem;

public class AimPoint : MonoBehaviour
{
    public GameObject followTarget;
    public InputActionAsset actions;
    private InputAction rotate;
    private Vector2 rotationDelta;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rotate = actions.FindAction("Player/Look");
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = followTarget.transform.position;
        rotationDelta = rotate.ReadValue<Vector2>()/3;
        transform.Rotate(new Vector3(-rotationDelta.y, 0, 0), Space.Self);
        transform.Rotate(new Vector3(0, rotationDelta.x, 0), Space.World);
    }
}
