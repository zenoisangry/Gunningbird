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

    [Header("Pause Menu")]
    public GameObject pauseMenuCanvas;

    private bool isPaused = false;

    void Start()
    {
        body = GetComponent<Rigidbody>();
        actions.FindAction("Player/Jump").started += Jump;

        if (pauseMenuCanvas != null)
            pauseMenuCanvas.SetActive(false);

        HideCursor();
    }

    void Update()
    {
        HandlePauseInput();

        if (isPaused) return;

        // Detect ground
        if (jumpTimer > 0)
            jumpTimer -= Time.deltaTime;

        // Detect decorations in the environment
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

        // Wind movement
        movementDirection = Quaternion.AngleAxis(lookDirection.eulerAngles.y, Vector3.back) * actions.FindAction("Player/Move").ReadValue<Vector2>();
        horizontalVelocity = new Vector2(body.linearVelocity.x, body.linearVelocity.z);
        if (horizontalVelocity.magnitude < windSpeed && movementDirection != Vector2.zero)
        {
            if (horizontalVelocity.magnitude > 1)
            {
                calculatedDirection = (movementDirection.normalized + horizontalVelocity.normalized) / 2;
                velocityDampening = Mathf.Sqrt((windSpeed - horizontalVelocity.magnitude * calculatedDirection.magnitude) / windSpeed);
                movementDirection = movementDirection * windStrength * velocityDampening;
                body.AddForce(new Vector3(movementDirection.x, 0f, movementDirection.y));
            }
            else
            {
                body.AddForce(new Vector3(movementDirection.x, 0f, movementDirection.y));
            }
        }

        // Detect cutifiable objects
        bool found = false;
        CheckCuteTargets(ref found, transform.position + 2 * Vector3.up, Vector3.down);
        if (!found) CheckCuteTargets(ref found, transform.position + 2 * Vector3.back, Vector3.forward);
        if (!found) CheckCuteTargets(ref found, transform.position + 2 * Vector3.right, Vector3.left);
        if (!found) CheckCuteTargets(ref found, transform.position + 2 * Vector3.left, Vector3.right);
        if (!found) CheckCuteTargets(ref found, transform.position + 2 * Vector3.forward, Vector3.back);

        if (found)
        {
            Vector2 orientation = new Vector2(transform.position.x, transform.position.z) - new Vector2(cuteTargets.point.x, cuteTargets.point.z);
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

    void HandlePauseInput()
    {
        if (Keyboard.current != null && (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.pKey.wasPressedThisFrame))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        SetPause(!isPaused);
    }

    public void SetPause(bool pause)
    {
        isPaused = pause;
        Time.timeScale = pause ? 0f : 1f;

        if (pauseMenuCanvas != null)
            pauseMenuCanvas.SetActive(pause);

        if (pause)
            ShowCursor();
        else
            HideCursor();
    }

    private void HideCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void ShowCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void PauseInput(bool pause)
    {
        SetPause(pause);
    }

    void CheckCuteTargets(ref bool found, Vector3 origin, Vector3 direction)
    {
        Physics.SphereCast(origin, cuteDetectionRange, direction * downwardsCuteDetection, out cuteTargets, 4f);
        if (cuteTargets.collider != null && cuteTargets.collider.gameObject.layer == 6)
            found = true;
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
        if (jumpTimer <= 0)
            grounded = true;
    }
}